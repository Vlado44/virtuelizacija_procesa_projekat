using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {

            new ConsoleSubscriber(EegPublisher.Instance);

            using (ServiceHost host = new ServiceHost(typeof(EegService)))
            {
                host.Open();

                Console.WriteLine("EEG WCF Servis je pokrenut!");
                Console.WriteLine("Pritisni ENTER za gasenje servisa.");
                Console.ReadLine();

                host.Close();
            }
        }
    }
}
