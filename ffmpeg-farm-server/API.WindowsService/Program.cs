using System;
using System.Linq;
using System.ServiceProcess;

namespace API.WindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new APIService()
            };

            if (Environment.UserInteractive)
            {
                APIService service = (APIService) servicesToRun.First();
                service.Start();
                Console.WriteLine(@"
.---..---.                        .---.                       .    .--. --.--
|    |                            |                          / \   |   )  |  
|--- |--- .--.--. .,-.  .-. .-..  |--- .-.  .--..--.--.     /___\  |--'   |  
|    |    |  |  | |   )(.-'(   |  |   (   ) |   |  |  |    /     \ |      |  
'    '    '  '  `-|`-'  `--'`-`|  '    `-'`-'   '  '  `-  '       `'    --'--
                  |         ._.'                                             
                  '                                                          ");
                Console.WriteLine();
                Console.WriteLine("Press any key to stop program");
                Console.Read();
                service.Stop();
            }
            else
            {
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
