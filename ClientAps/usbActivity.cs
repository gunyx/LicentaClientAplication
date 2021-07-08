using System;
using System.Collections.Generic;
using System.Text;

namespace ClientAps
{
    class usbActivity : _package
    {
        //MEMBRII
        private string informationModem;
        private string accesedTime;
        private string type;
        private bool already_added = false;

        //METODE
        public Boolean verify_status_package()
        {
            return already_added;
        }
        public void set_status_package(bool status)
        {
            already_added = status;
        }
        public string get_package_inf()
        {
            return informationModem+"&"+accesedTime;
        }
        public string get_accesed_time()
        {
            return accesedTime;
        }
        public void set_package(Dictionary<string, string> informationList)
        {
            this.informationModem = informationList["infoModem"].ToString();
            this.accesedTime = DateTime.Now.ToString("h:mm:ss tt");
            this.type = "USB";
        }
        public string get_class()
        {
            return this.type;
        }
    }
}
