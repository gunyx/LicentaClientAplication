using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClientAps
{
    internal class processActivity : _package
    {
        //MEMBRII
        [JsonProperty]
        private bool already_added = false;
        [JsonProperty]
        private string processName;
        [JsonProperty]
        private string aplicationType;
        [JsonProperty]
        private string accesedTime;
        [JsonProperty]
        private string type;
        [JsonProperty]
        private string description;


        //METODE
        public Boolean verify_status_package()
        {
            return already_added;
        }
        public void set_status_package(bool status)
        {
            already_added = status;
        }
        public string get_accesed_time()
        {
            return accesedTime;
        }
        public string get_package_inf()
        {
            string info = "ProcessName: " + processName + "\n" +
                "AplicationType: " + aplicationType + "\n"+
                "AccesedTime" + accesedTime+"\n"+
                "Description: "+description;
            return info;
        }
        public string get_class()
        {
            return this.type;
        }
        public void set_package(Dictionary<string, string> informationList)
        {
            this.processName = informationList["processName"].ToString();
            this.aplicationType = informationList["aplicationType"].ToString();
            this.accesedTime = informationList["accesedTime"].ToString();
            if (informationList["descrpt"]!= null)
            {
                this.description = informationList["descrpt"].ToString();
            }
            else
            {
                this.description = "Nothing";
            }
            this.type = "ProcessActivity";
        }
    }
}
