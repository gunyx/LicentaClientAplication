using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace ServerConsole
{
    class Conexiune_client
    {
        TcpClient activity_port;
        TcpClient request_port;

        public void set_activity(TcpClient param)
        {
            Console.WriteLine("Am setat portul pentru receptie automata");
            activity_port = new TcpClient();
            activity_port = param;
        }
        public void set_requests(TcpClient param)
        {
            Console.WriteLine("Am setat portul pentru request");
            request_port = new TcpClient();
            request_port = param;
        }
        public TcpClient get_activity()
        {
            return activity_port;
        }
        public TcpClient get_requests()
        {
            return request_port;
        }
    }

    class Server
    {
        private static readonly object _locker = new object();//hashtable
        private static readonly object _locker_status = new object();
        private static bool signal_stop = true;
        private int activate_status = 0;

        private static string IpServer_s = String.Empty;
        private static int PortServer_s1 = 0;
        private static int PortServer_s2 = 0;

        private static TcpListener serverTCP1 = null;
        private static TcpListener serverTCP2 = null;

        private static Hashtable clients = new Hashtable();

        private static Thread connThread = null;
        private static Thread recvThread = null;
        private static Thread managerConn = null;


        public Server(string ip, int port, int port2)
        {
            IpServer_s = ip;
            PortServer_s1 = port;
            PortServer_s2 = port2;
            signal_stop = true;
            Initializare();
        }

        public void Initializare()
        {
            if (connThread != null)
            {
                connThread = null;
            }
            if (recvThread != null)
            {
                recvThread = null;
            }
            if (managerConn != null)
            {
                managerConn = null;
            }
            ThreadStart startListen = new ThreadStart(ServerConnection);
            connThread = new Thread(startListen);
            signal_stop = false;
            connThread.Start();

            ThreadStart startInteraction = new ThreadStart(ServerComunication);
            recvThread = new Thread(startInteraction);
            signal_stop = false;
            recvThread.Start();

            ThreadStart startManager = new ThreadStart(UpdateTable);
            managerConn = new Thread(startManager);
            signal_stop = false;
            managerConn.Start();
        }

        public void ServerComunication()//functie executata de thread-ul ce solicita task-uri
        {
            IPAddress IP_Server_Int = IPAddress.Parse(IpServer_s);
            serverTCP2 = new TcpListener(IP_Server_Int, PortServer_s1);
            serverTCP2.Start();

            Console.WriteLine("Interaction server port ON");
            try
            {
                while (!signal_stop)
                {
                    if (serverTCP2.Pending())
                    {
                        Console.WriteLine("Cerere noua pe portul de comunicatie");
                        TcpClient request = serverTCP2.AcceptTcpClient();
                        byte[] buffer = new byte[request.ReceiveBufferSize];
                        int dim_pachet_conectare = request.Client.Receive(buffer);
                        
                        if (dim_pachet_conectare > 0)
                        {
                            string[] fst_receive = null;
                            string buffer_s = Encoding.UTF8.GetString(buffer);
                            fst_receive = buffer_s.Split((char)126);//caracterul '~'
                            string mac_client = fst_receive[1].Substring(0,12);
                            switch (fst_receive[0])
                            {
                                case "CONNECT":
                                    if (clients.ContainsKey(mac_client))//Exista o intrare in tabela pentru MAC-ul clientului
                                    {
                                        Conexiune_client test;
                                        lock (_locker)
                                        {
                                            //  test = (Conexiune_client)clients[Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString()];
                                            test = (Conexiune_client)clients[mac_client];
                                        }
                                        TcpClient val_testata = test.get_requests();//daca are deja deschis un Socket pentru requests
                                        if (val_testata == null)//nu exista conexiune cu respectivul client
                                        {
                                            Conexiune_client new_conex = new Conexiune_client();
                                            new_conex.set_activity(test.get_activity());
                                            new_conex.set_requests(request);
                                            lock (_locker)
                                            {
                                                activate_status = 2;
                                                clients[mac_client] = null;
                                                clients[mac_client] = new_conex;
                                                // clients[Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString()] = null;
                                               // clients[Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString()] = new_conex;
                                            }
                                        }
                                        else//deja exista un socket deschis cu acel client
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        Conexiune_client con_new = new Conexiune_client();
                                        con_new.set_requests(request);
                                        lock (_locker)
                                        {
                                            clients.Add(mac_client, con_new);
                                            //clients.Add(Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString(), con_new);
                                            //Conexiune_client new_client2 = (Conexiune_client)clients[fst_receive[1]];
                                        }
                                    }
                                    //Trimit confirmarea deschiderii socket-ului client-server(+testarea lui)
                                    byte[] send = Encoding.UTF8.GetBytes("ok");
                                    byte[] SendMsg = new byte[send.Length];
                                    Buffer.BlockCopy(send, 0, SendMsg, 0, send.Length);
                                    request.Client.Send(SendMsg);        
                                    
                                    //mac_client = Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString();     
                                    Conexiune new_client = new Conexiune(request, mac_client, 1);
                                   
                                    break;
                                case "EXIT":

                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare Server " + ex.Message);
                Environment.Exit(-5);
            }
        }

        public void ServerConnection()//functie executata de thread-ul ce asculta informatiile de la clienti
        {
            IPAddress IP_Server = IPAddress.Parse(IpServer_s);
            serverTCP1 = new TcpListener(IP_Server, PortServer_s2);
            serverTCP1.Start();
            Console.WriteLine("Receiver server port ON");
            try
            {
                while (!signal_stop)
                {
                    if (serverTCP1.Pending())
                    {
                        Console.WriteLine("Cerere noua pe portul de receptie pachete");
                        TcpClient request = serverTCP1.AcceptTcpClient();
                        byte[] buffer = new byte[request.ReceiveBufferSize];
                        int dim_pachet_conectare = request.Client.Receive(buffer);
                       
                        if (dim_pachet_conectare > 0)
                        {
                            string[] fst_receive = null;
                            string buffer_s = Encoding.UTF8.GetString(buffer);
                            fst_receive = buffer_s.Split((char)126);//caracterul '~'
                            string mac_addr_client = fst_receive[1].Substring(0, 12);
                            switch (fst_receive[0])
                            {
                                case "CONNECT":
                                    if (clients.ContainsKey(mac_addr_client))
                                    {
                                        Conexiune_client test;
                                        lock (_locker)
                                        {
                                            // test = (Conexiune_client)clients[Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString()];
                                            test = (Conexiune_client)clients[mac_addr_client];
                                        }
                                        TcpClient val_testata = test.get_activity();
                                        if (val_testata == null)
                                        {
                                            Conexiune_client new_conex = new Conexiune_client();
                                            new_conex.set_activity(request);
                                            new_conex.set_requests(test.get_requests());
                                            lock (_locker)
                                            {
                                                clients[mac_addr_client] = null;
                                                clients[mac_addr_client] = new_conex;
                                                //clients[Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString()] = null;
                                                //clients[Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString()] = new_conex;
                                                activate_status = 2;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        Conexiune_client con_new = new Conexiune_client();
                                        con_new.set_activity(request);
                                        lock (_locker)
                                        {
                                            clients.Add(mac_addr_client, con_new);
                                            //clients.Add(Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString(), con_new);
                                        }
                                    }

                                    byte[] send = Encoding.UTF8.GetBytes("ok");
                                    byte[] SendMsg = new byte[send.Length];
                                    Buffer.BlockCopy(send, 0, SendMsg, 0, send.Length);
                                    request.Client.Send(SendMsg);

                                    //mac_addr_client = Regex.Match(fst_receive[1], @"(\w+)(\d+)").ToString();
                                    Conexiune new_client = new Conexiune(request, mac_addr_client, 0);
                                  
                                    break;
                                default:
                                    break;
                            }
                        }     
                    }
                    else
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare Server " + ex.Message);
                Environment.Exit(-5);
            }
        }

        public void UpdateTable()
        {
            while (!signal_stop)
            {
                Hashtable clientsCopy = new Hashtable();
                if (activate_status == 2)
                {
                    lock (_locker)
                    {
                        clientsCopy = (Hashtable)clients.Clone();
                    }
                    foreach (var key in clientsCopy.Keys)
                    {
                        Conexiune_client test = (Conexiune_client)clientsCopy[key];
                        if ((test.get_activity().Connected == false) || (test.get_requests().Connected == false))//aici
                        {

                            if (test.get_activity() != null)
                            {
                                test.get_activity().Close();
                            }
                            if (test.get_requests() != null)
                            {
                                test.get_requests().Close();
                            }
                            lock (_locker)
                            {
                                Console.WriteLine("Am sters un client din hashtable");
                                //Console.WriteLine(clients[key])
                                Console.WriteLine(clients.Count);
                                clients.Remove(key);
                                if(clients.Count==0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
            }
        }
         
    }
}
