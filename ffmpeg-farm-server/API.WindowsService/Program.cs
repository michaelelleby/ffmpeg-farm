using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace API.WindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;

            ServicesToRun = new ServiceBase[]
            {
                new APIService()
            };

            if (Environment.UserInteractive)
            {
                APIService service = (APIService) ServicesToRun.First();
                service.Start();
                Console.WriteLine("Press any key to stop program");
                Console.Read();
                service.Stop();
            }
            else
            {
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
