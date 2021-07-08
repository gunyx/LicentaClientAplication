using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientAps
{
    /*
     * Clasa ce contine metodele ce indeplinesc functionalitatile de inregistrare video si captura de ecran
     */

    public class detailedActivity: _package
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetSystemMetrics(int nIndex);
        //MEMBRII
        private bool already_added = false;
        private string type;
        private string location;

        //METODE
        public Boolean verify_status_package()
        {
            return already_added;
        }   
        public void set_status_package(bool status)
        {
           already_added=status;
        }
        public string get_accesed_time() => throw new NotImplementedException();   
        public string get_package_inf()
        {
            return location;
        }
        public void set_package(Dictionary<string, string> informationList)
        {
            this.type = informationList["Type monitoring"].ToString();
            this.location = informationList["pathProof"].ToString();
        }
        private static Video startVideo(string video_name,int fps)
        {
            var recorder = new Video(new VideoParameters(video_name, fps, SharpAvi.KnownFourCCs.Codecs.MicrosoftMpeg4V3));
            return recorder;
        }
        public void start_video(int secunde, int fps)
        {
            string nameForRecording = String.Empty;
            if (check_screenshot_directory(@"RecordsDir"))
            {
                nameForRecording = Directory.GetCurrentDirectory().ToString() + @"\RecordsDir\" + get_time() + ".avi";
            }
            else
            {
                Console.WriteLine("Eroare cu folderul pentru inregistrari");
                Environment.Exit(0);
            }


            var intrerupator = startVideo(nameForRecording, fps);

            Task a=Task.Delay(new TimeSpan(0, 0, secunde)).ContinueWith(o => { stop_method(intrerupator); });
            a.Wait();
            set_information_video(nameForRecording);  
        }
        public void stop_method(Video intrerupator)
        {
            intrerupator.Dispose();
        }
        public void take_printscreen()
        {
            int step = 0, width = 0, height = 0;
            /*ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2",
            "SELECT * FROM Win32_VideoController");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                if (step == 0)
                {
                    width = Int32.Parse(queryObj["CurrentHorizontalResolution"].ToString());
                    height = Int32.Parse(queryObj["CurrentVerticalResolution"].ToString());
                }
                else
                {
                    if (width > Int32.Parse(queryObj["CurrentHorizontalResolution"].ToString()))
                    {
                        width = Int32.Parse(queryObj["CurrentHorizontalResolution"].ToString());
                    }
                    if (height > Int32.Parse(queryObj["CurrentVerticalResolution"].ToString()))
                    {
                        height = Int32.Parse(queryObj["CurrentVerticalResolution"].ToString());
                    }
                }

                if(searcher.Get().Count<=2)
                {
                    break;
                }

                step++;
            }*/

            width = GetSystemMetrics(0);
            height = GetSystemMetrics(1);

            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0,
                bitmap.Size, CopyPixelOperation.SourceCopy);
            }
            if (check_screenshot_directory(@"Screenshot"))
            {
                string finalName = Directory.GetCurrentDirectory().ToString() + @"\Screenshot\" + get_time() + ".jpg";
                bitmap.Save(finalName, ImageFormat.Jpeg);
                Console.WriteLine(finalName);
                set_information_ss(finalName);
            }
            else
            {
                Console.WriteLine("Eroare cu folderul pentru screenshot-uri");
            }
        }
        private void set_information_video(string source)
        {
            this.type = "Records";
            this.location = source;
        }
        private void set_information_ss(string source)
        {
            this.type = "Screenshots";
            this.location = source;
        }
        private string get_time()
        {
            DateTime localDate = DateTime.Now;
            DateTime utcDate = DateTime.UtcNow;
            String[] cultureNames = { "ro-Ro" };
            string namePrintScreen = null;
            foreach (var cultureName in cultureNames)
            {
                var culture = new CultureInfo(cultureName);
                //Console.WriteLine(localDate.ToString(culture));
                namePrintScreen = localDate.ToString(culture).Replace(":", "-");
                //Console.WriteLine(namePrintScreen);
            }

            return namePrintScreen;
        }  
        public string get_class()
        {
            return this.type;
        }
        private bool check_screenshot_directory(string pathPrimit)
        {
            string path = pathPrimit;

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
    
    }
}
