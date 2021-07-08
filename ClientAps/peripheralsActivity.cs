using System;
using System.Collections.Generic;
using System.Text;

namespace ClientAps
{
    class peripheralsActivity:_package
    {
        //MEMBRII
        private string filename;
        private bool already_added = false;
        private string accesedTime;
        private string type;

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
            return filename;
        }
        public string get_accesed_time()
        {
            return accesedTime;
        }
        public void set_package(Dictionary<string, string> informationList)
        {
            this.filename = informationList["filenameKeys"].ToString();
            this.accesedTime= DateTime.Now.ToString("h:mm:ss tt");
            this.type = "KeyboardActivity";
        }
        public string get_class()
        {
            return this.type;
        }

    }
}
