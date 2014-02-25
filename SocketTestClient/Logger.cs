using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SocketTestClient
{
    class Logger
    {
        StreamWriter sw;
        string path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName + "\\Log\\";

        public bool Enabled { get; set; }

        public Logger()
        {
            DateTime now = DateTime.Now;
            string datePatt = @"MM dd yyyy hh_mm_ss";
            //the format of the file is <current time>. It's a csv file.
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                this.sw = new StreamWriter(path + now.ToString(datePatt) + ".csv");
            }

            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("funny!");
            }
            Enabled = true;
        }

        public void LogLine(string[] strs)
        {
            if (Enabled)
            {
                try
                {
                    for (int i = 0; i < strs.Length; i++)
                    {
                        if (i != strs.Length - 1)
                            sw.Write(strs[i] + ",");
                        else sw.Write(strs[i] + "\r\n");
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void Close()
        {
            if (sw != null) sw.Dispose();
        }
    }

    
}
