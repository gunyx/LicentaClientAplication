using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ClientAps
{
    //clasa ce se va ocupa cu task-urile provenite din partea serverului
    class ManagerRequestSv
    {
        //MEMBRII
        private static object _locker_semph=new object();//folosit pentru semafor
        private static object _locker_send=new object();//folosit pentru task
        private static object _locker_fct = new object();//blocheaza pentru add la for_resolve

        private static List<_package> tasks;//task-urile indeplinite
        private static List<string> for_resolve;//cererile de la server

        private Thread listenerThread = null;
        private Thread senderThread = null;
        private Thread analizerThread = null;

        private ConexServer conexiuneSv;//socket-ul cu server-ul

        bool signal;//stop threads
        int semafor = 0;//var pentru a bloca conexiune doar pentru receptia continutului de fisiere

        //METODE
        public ManagerRequestSv()
        {
            tasks = new List<_package>();//task-urile rezolvate
            for_resolve = new List<string>();//cererile pentru rezolvare
            conexiuneSv = new ConexServer("127.0.0.1", 6666);//PORTUL SERVER-ULUI si IP-ul
            while (!conexiuneSv.get_socket_client().Connected)
            {
                conexiuneSv = new ConexServer("127.0.0.1", 7777);
            }
            try
            {
                ThreadStart listener = new ThreadStart(ListenThread);
                listenerThread = new Thread(listener);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                ThreadStart analizer = new ThreadStart(AnalizerThread);
                analizerThread = new Thread(analizer);
                analizerThread.IsBackground = true;
                analizerThread.Start();

                ThreadStart sender = new ThreadStart(SenderFunction);
                senderThread = new Thread(sender);
                senderThread.IsBackground = true;
                senderThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
        private void SenderFunction()
        {
            try
            { 
                Thread.CurrentThread.IsBackground = true;
                while (!signal)
                {
                    _package[] for_send = null;
                    if (tasks.Count!= 0)
                    {
                        lock (_locker_send)
                        {
                            for_send = tasks.ToArray();
                        }
                        foreach (_package selectie in for_send)
                        {
                            if (selectie.get_class().Equals("Screenshots"))
                            {
                                lock(_locker_semph)
                                {
                                    semafor = 1;
                                }
                                    
                                conexiuneSv.SendFile(selectie.get_package_inf(), selectie.get_class());
                                conexiuneSv.SendNormalMsj("Ready");
                                    
                                lock (_locker_semph)
                                {
                                    semafor = 0;
                                }
                            }
                            else if (selectie.get_class().Equals("Records"))
                            {
                                Console.WriteLine("Trimit informatiile");
                                lock (_locker_semph)
                                {
                                    semafor = 1;
                                }

                                conexiuneSv.SendFile(selectie.get_package_inf(), selectie.get_class());
                                conexiuneSv.SendNormalMsj("Ready");
                                    
                                lock (_locker_semph)
                                {
                                        semafor = 0;
                                }

                            }
                            lock (_locker_send)
                            {
                                tasks.Remove(selectie);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
        private void ListenThread()
        {
            try
            {
               Thread.CurrentThread.IsBackground = true;
                while (!signal)
                {
                    if (semafor == 0 && for_resolve.Count==0)//se transmite pachet cu pachet de la sv
                    {
                        string infs = null;//acesta il primesc de la server
                        infs = conexiuneSv.ReceiveRequest();
                        if (infs != null)
                        {
                            lock (_locker_fct)
                            {
                               // Console.WriteLine("Am adaugat");
                                for_resolve.Add(infs);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
        private void AnalizerThread()
        {
            while(!signal)
            {
                string[] for_resolve_c;//copie pentru a nu tine variabila blocata

                lock (_locker_fct)
                {
                    for_resolve_c = for_resolve.ToArray();
                }

                foreach (var informatie in for_resolve_c)
                {
                    string[] subs = informatie.Split('~');
                    string parametrii = String.Empty;
                    string tip = String.Empty;
                    int selector = 0;

                    foreach (var segment in subs)
                    {
                        if (selector == 0)
                        {
                            tip = segment;
                        }
                        else if (selector == 1)
                        {
                            parametrii = segment;
                        }
                        selector++;
                    }
                    
                    if (tip.Equals("Recording"))
                    {
                        Console.WriteLine("Incep sa execut task-ul");
                        Thread thread_recording = new Thread(() => recording_fct(parametrii));
                        thread_recording.Start();
                        thread_recording.Join();

                    }
                    else if (tip.Equals("Screenshot"))
                    {
                        Thread thread_ss = new Thread(() => printscreen_fct());
                        thread_ss.Start();
                        thread_ss.Join();
                    }
                    else
                    {
                        Console.WriteLine("Am primit o cerere de tipul " + tip);
                    }
                    lock(_locker_fct)
                    {
                        for_resolve.Remove(informatie);
                        Console.WriteLine("Am sters din for_resolve "+informatie+" si am adaugat la task"+ tasks.Count);
                    }
                }
            }
        }
        private void printscreen_fct()
        {
            _package ss = (detailedActivity)new detailedActivity();
            ((detailedActivity)ss).take_printscreen();
            lock (_locker_send)
            {
                tasks.Add(ss);
            }
        }
        private void recording_fct(string parametrii)
        {
            string fps= String.Empty; ;
            string durata= String.Empty; ;
            string[] componente = parametrii.Split('$');
            int counter = 0;
            foreach (var parametru in componente)//componente doar cu 2 elemente
            {
                if(counter==0)
                {
                    durata = parametru;
                }
                else
                {
                    fps = parametru;
                }

                counter++;
            }
            _package video = (detailedActivity)new detailedActivity();
            ((detailedActivity)video).start_video(Int32.Parse(durata), Int32.Parse(fps));
            lock (_locker_send)
            {
                tasks.Add(video);
            }
        }       
    }
}
