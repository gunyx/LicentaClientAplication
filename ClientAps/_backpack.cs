using System;
using System.Collections.Generic;
using System.Text;

namespace ClientAps
{
    /*
     * Clasa ce contine lista pachetelor ce trebuiesc trimise la server
     */
    class _backpack
    {
        private List<_package> pachete=new List<_package>();
        public List<_package> get_list() => this.pachete;
        public void add_package(_package new_package)
        {
            pachete.Add(new_package);
        }
        public int get_nr_elements()
        {
            if(pachete==null)
            {
                return 0;
            }
            else
            {
                return pachete.Count;
            }
        }
        public _package get_packege(int position)
        {
            return pachete[position];
        }
        public bool remove_package(_package sters)
        {
            pachete.Remove(sters);

            return false;
        }
    }
}
