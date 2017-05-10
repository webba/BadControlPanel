using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml.Serialization;

namespace GameControlPanel
{
    class Program
    {
        public static ProcessManager pmanager = new ProcessManager();
        static void Main(string[] args)
        {
            var server = new LocalHttpListener();
            var task = Task.Factory.StartNew(() => server.Start());
            
            var ptask = Task.Factory.StartNew(() => pmanager.Startup());
            bool wait = true;
            while (wait)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Q:
                            wait = false;
                            break;
                        default:
                            break;
                    }
                }
            } 
            server.Stop();
        }
    }
}
