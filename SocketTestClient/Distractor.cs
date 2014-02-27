using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SocketTestClient
{
    class Distractor
    {
        string path = "C:\\Users\\Interactions Lab\\Desktop\\WordList.txt";
        int index = -1;
        int count;
        List<string> words;

        public Distractor()
        {
            count = 0;
            using (StreamReader sr = new StreamReader(path))
            {
                words = new List<string>();
                string line = sr.ReadLine();
                //Console.WriteLine("oh!");
                while (line != null)
                {
                    words.Add(line);
                    count++;
                    line = sr.ReadLine();
                }
                Console.WriteLine(count.ToString());
            }
        }

        public string NextWord()
        {
            if (index < count)
            {
                index++;
            }
            else index = 0;
            return words[index];
        }



    }
}
