using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ServerConsole
{
    class  Conexiune
    {
        private static object _locker = new object();//locker pentru instantierea in constructor (avem membrii statici)
        private static object _locker_requests = new object();//mutex pentru lista for_resole
        private static object _locker_sem = new object();//locker pentru variabila ce blocheaza in asteptare toate thread-urile
                                                         //care incearca sa trimita pachete in timp ce se receptioneaza fisiere
        public TcpClient client_conn;
        private List<string> for_resolve;
        private List<string> exclusion;

        private Thread receptioner = null;
        private Thread sender = null;
        private Thread listen_supervisor;

        private string path_client;
        private int type_conn;
        private static bool signal_stop = true;
        private int semafor = 0;


        public Conexiune(TcpClient conexiune,string path,int tip)
        {
            lock (_locker)
            {
                for_resolve = new List<string>();//lista de cereri
                exclusion = new List<string>();

                exclusion.Add("Notepad");

                path_client = path;
                client_conn = conexiune;
                type_conn = tip;
 
                signal_stop = true;
                semafor = 0;
            }

            if (type_conn == 1)//conexiune pentru request-uri de la server
            {
                listen_supervisor = new Thread(ListenSupervisor);
                listen_supervisor.IsBackground = true;
                signal_stop = false;
                listen_supervisor.Start(client_conn.Client);

                sender = new Thread(SenderRequests);
                sender.IsBackground = true;
                signal_stop = false;
                sender.Start(client_conn.Client);
            }
            else//conexiune de ascultare si receptionare a pachetelor automate
            {
                receptioner = new Thread(ReceiveMsg);
                receptioner.IsBackground = true;
                signal_stop = false;
                receptioner.Start(client_conn.Client);
            }
        }

        public void ReceiveMsg(object socket_for_client)
        {
            Socket socket_conexiune = (Socket)socket_for_client;
            string FileName = String.Empty;
            string FileType = String.Empty;
            long FileLen = 0;

            while (!signal_stop)
            {
                try
                {
                    int bytesRcv = 0;
                    byte[] bufferInformatii = new byte[1024 * 1024 * 5];
                    if (socket_conexiune != null)
                    {
                        //Console.WriteLine("Astept sa primesc raspuns");
                        bytesRcv = socket_conexiune.Receive(bufferInformatii);//asteapta pana cand receptioneaza un pachet
                    }
                    if (bytesRcv > 0)
                    {
                       // Console.WriteLine("Am primit pachet");
                        if (bufferInformatii[0] == 'F')//pachet ce contine un fisier
                        {
                            if (bufferInformatii[1] == 'D')//Detalii despre fisierul trimis
                            {
                                string informatii = Encoding.UTF8.GetString(bufferInformatii, 2, bytesRcv - 2);
                                string[] subs = informatii.Split('~');
                                int selector = 0;

                                foreach (var segment in subs)
                                {
                                    if (selector == 0)
                                    {
                                        FileName = segment;
                                        //Console.WriteLine("Numele fisierului:" + FileName);
                                    }
                                    else if (selector == 1)
                                    {
                                        FileLen = Convert.ToInt64(segment);
                                        //Console.WriteLine("Lungime fisier:" + FileLen);
                                    }
                                    else if (selector == 2)
                                    {
                                        FileType = segment;
                                        //Console.WriteLine("Continutul fisierului: " + FileType);
                                    }
                                    selector++;
                                }

                                //trimit confirmarea receptionarii detaliilor de fisier
                                byte[] send = Encoding.UTF8.GetBytes("content");
                                byte[] SendMsg = new byte[send.Length];
                                Buffer.BlockCopy(send, 0, SendMsg, 0, send.Length);
                                client_conn.Client.Send(SendMsg);      
                            }

                            if (bufferInformatii[1] == 'C')//Continutul fisierului
                            {
                                Console.WriteLine("SERVER: Incep sa primesc continutul");
                                string locatie_client = @"EroareMac";//Director hardcodat
                                if (path_client != null)
                                {
                                    locatie_client = path_client;
                                }
                                string typePath = String.Empty;
                                if (FileType != null)
                                {
                                    typePath = Path.Combine(locatie_client, FileType);
                                }
                                else
                                {
                                    typePath = locatie_client;
                                }

                                if (!Directory.Exists(typePath))//verific daca directoru' cu MAC-ul std exista
                                {
                                    Directory.CreateDirectory(typePath);
                                }

                                string FullPath = Path.Combine(typePath, FileName);
                                int dim_pachet = 0;
                                long recvBytes = 0;
                                int step = 0;
                                int index = 1;

                                while (File.Exists(FullPath))
                                {
                                    string[] strs = FileName.Split('.');
                                    strs[0] = strs[0].Split('(')[0] + "(" + index.ToString() + ")";
                                    FileName = strs[0] + "." + strs[1];
                                    FullPath = Path.Combine(typePath, FileName);
                                    index++;
                                }
                                Stopwatch stopwatch=null;
                                if (semafor == 1)
                                {
                                   stopwatch = new Stopwatch();
                                   stopwatch.Start();
                                }
                                using (FileStream file = new FileStream(FullPath, FileMode.Create, FileAccess.Write))
                                {
                                    while (recvBytes < FileLen)
                                    {
                                        if (step == 0)
                                        {
                                            file.Write(bufferInformatii, 2, bytesRcv - 2);
                                            file.Flush();
                                            recvBytes += bytesRcv - 2;
                                            step++;
                                        }
                                        else
                                        {
                                            dim_pachet = socket_conexiune.Receive(bufferInformatii);
                                            file.Write(bufferInformatii, 0, dim_pachet);
                                            file.Flush();
                                            recvBytes += dim_pachet;
                                        }
                                    }
                                    file.Close();
                                }
                                Console.WriteLine("Am primit tot fisierul");
                                byte[] send = Encoding.UTF8.GetBytes("next");
                                byte[] SendMsg = new byte[send.Length];
                                Buffer.BlockCopy(send, 0, SendMsg, 0, send.Length);
                                socket_conexiune.Send(SendMsg);

                                if (semafor == 1)
                                {
                                    stopwatch.Stop();
                                    Console.WriteLine("Elapsed Time is {0} ms", stopwatch.ElapsedMilliseconds);
                                }
                                //Console.WriteLine("Server: dupa continut am trimis " + SendMsg.Length);
                                //Console.WriteLine("Am trimis next si acum astept dupa alt pachet \n");
                                if (semafor==1)
                                {
                                   // Console.WriteLine("NU AICI");
                                    byte[] buffReady = new byte[client_conn.ReceiveBufferSize];
                                    socket_conexiune.Receive(buffReady);
                                    string strReady = Encoding.UTF8.GetString(buffReady);
                                    var onlyLetters = new String(strReady.Where(Char.IsLetter).ToArray());
                                    if (!onlyLetters.Equals("Ready"))
                                    {
                                        Console.WriteLine("Semnal incorect de la clinet. Am primit : " + onlyLetters);
                                        return;
                                    }
                                    break;//opresc thread-ul de recv
                                }
                            }


                        }
                        else if (bufferInformatii[0] == 'P')//pachet ce contine json-ul cu informatii despre procesele deschise
                        {
                            string inf_process = Encoding.UTF8.GetString(bufferInformatii, 1, bytesRcv - 1);
                            Console.WriteLine("Proces inregistrat " + inf_process);
                            string locatie_client = @"EroareMac";//Director hardcodat
                            if (path_client != null)
                            {
                                locatie_client = path_client;
                            }
                            string typePath= Path.Combine(locatie_client,"ProcessActivity");
                            if (!Directory.Exists(typePath))//verific daca directoru' cu MAC-ul std exista
                            {
                                Directory.CreateDirectory(typePath);
                            }
                            string path = Path.Combine(typePath, "processInfo.txt");
                            using (StreamWriter swo = File.AppendText(path))
                            {
                                swo.WriteLine(inf_process);
                                swo.WriteLine("\n");
                            }
                            byte[] send = Encoding.UTF8.GetBytes("next");
                            byte[] SendMsg = new byte[send.Length];
                            Buffer.BlockCopy(send, 0, SendMsg, 0, send.Length);
                            client_conn.Client.Send(SendMsg);
                        }
                        else if(bufferInformatii[0]=='U')
                        {
                            string inf_port = Encoding.UTF8.GetString(bufferInformatii, 1, bytesRcv - 1);
                            Console.WriteLine("Dispozitiv extern detectat " + inf_port);
                            string locatie_client = @"EroareMac";//Director hardcodat
                            if (path_client != null)
                            {
                                locatie_client = path_client;
                            }
                            string typePath = Path.Combine(locatie_client, "PortsActivity");
                            if (!Directory.Exists(typePath))//verific daca directoru' cu MAC-ul std exista
                            {
                                Directory.CreateDirectory(typePath);
                            }
                            string path = Path.Combine(typePath, "portsInfo.txt");
                            using (StreamWriter swo = File.AppendText(path))
                            {
                                swo.WriteLine(inf_port);
                                swo.WriteLine("\n");
                            }

                            //confirmarea receptionarii informatiilor despre proces
                            byte[] send = Encoding.UTF8.GetBytes("next");
                            byte[] SendMsg = new byte[send.Length];
                            Buffer.BlockCopy(send, 0, SendMsg, 0, send.Length);
                            client_conn.Client.Send(SendMsg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exceptie " + ex.Message+" de pe socket-ul "+type_conn);
                    break;
                }
            }
        }
    
        public string testApps(string recv_info)
        {
            string[] detaliere = recv_info.Split(":");
            int step_info = 0;
            string answer=null;
            for(int i=0;i<detaliere.Length;i++)
            {
                if (detaliere[i].Contains("aplicationType"))
                {
                    step_info = 1;
                }else if (step_info == 1)
                {
                    string[] intermediar = detaliere[i].Split(",");
                    answer = intermediar[0];
                    step_info = 2;
                    break;
                }
            }
            return answer;
        }

        public void ListenSupervisor(object socker_for_client)
        {
            //Functia apelata din interfata grafica
            while (!signal_stop)
            {
                //Thread.Sleep(1000);
                lock (_locker_requests)
                {
                    for_resolve.Add("Screenshot");
                }
                Thread.Sleep(1000);
                lock (_locker_requests)
                {
                    
                    for_resolve.Add("Recording~10$5");
                }
                Thread.Sleep(1000);
            }
        }
    
        public void SenderRequests(object socker_for_client)
        {
            //aici se da throw
            while (!signal_stop)
            {
                try
                {
                    string[] for_send_c;

                    lock (_locker_requests)
                    {
                        for_send_c = for_resolve.ToArray();//copia listei de task-uri
                    }
                    if (for_send_c.Length == 0)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    else
                    {
                        foreach (var request in for_send_c)
                        {
                            /*Astepta sa se primeasca raspunsul cererii serverului de la client
                             * Putand solicita doar Recording/Screenshot, este irelevant de a trimite 2 cereri de recording in acelasi timp
                             * Ocupare spatiu cu aceasi informatie+buffer de conexiune
                            */
                            if (semafor == 0)
                            {

                                byte[] pachet = Encoding.UTF8.GetBytes(request);
                                byte[] pachetMsg = new byte[pachet.Length];
                                Buffer.BlockCopy(pachet, 0, pachetMsg, 0, pachet.Length);
                                client_conn.Client.Send(pachetMsg);
                                //Console.WriteLine("Wait for reply");
                                byte[] buffRecv = new byte[client_conn.ReceiveBufferSize];
                                client_conn.Client.Receive(buffRecv);
                                string buffer_primit = Encoding.UTF8.GetString(buffRecv);
                                var onlyLetters = new String(buffer_primit.Where(Char.IsLetter).ToArray());
                                Console.WriteLine("Am trimis pachetul");
                                if (!onlyLetters.Equals("primit"))
                                {
                                    Console.WriteLine("Semnal incorect de la clinet. Am primit : " + onlyLetters);
                                    return;
                                }

                                lock (_locker_requests)
                                {
                                    for_resolve.Remove(request);
                                    // Console.WriteLine("Sterg");
                                }

                                lock (_locker_sem)//blochez canal de conexiune
                                {
                                    semafor = 1;
                                }
                                ReceiveMsg(socker_for_client);
                                //Console.WriteLine("Dar aici nu mai ajung");
                                lock (_locker_sem)//deblochez canal de conexiune
                                {
                                    semafor = 0;
                                }
                            }
                        }
                    }
                }catch(Exception ex)
                {
                    Console.WriteLine("Exceptie " + ex.Message + " de pe socket-ul " + type_conn);
                    break;
                }
            }
        }
    }
}
