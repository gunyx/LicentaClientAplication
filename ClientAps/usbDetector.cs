using System;
using System.Collections.Generic;
using System.Management;

namespace ClientAps
{
    class usbDetector
    {
        string port;
        string descript;

        public usbDetector(string port, string description)
        {
            this.port = port;
            this.descript = description;
        }
    }
    class mangerUSB
    {
        public List<usbDetector> GetUsbInfo()
        {
            List<usbDetector> usbConection = new List<usbDetector>();
            ManagementObjectCollection collection;
            var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub");
            collection = searcher.Get();
            foreach (var device in collection)
            {
                usbConection.Add(new usbDetector(
                            (string)device.GetPropertyValue("DeviceID"),
                            (string)device.GetPropertyValue("Description")
                ));
            }
            collection.Dispose();
            return usbConection;
        }

    }
}
