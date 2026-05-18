using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.ServiceModel;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IEegService> factory = null;
            IEegService proxy = null;

            try
            {
                factory = new ChannelFactory<IEegService>("EegService");
                proxy = factory.CreateChannel();

                Meta meta = new Meta("1", "subject_1_results.csv", 10, "1.0");

                ServiceResponse response = proxy.StartSession(meta);

                Console.WriteLine("Odgovor Servera:");
                Console.WriteLine("ACK: " + response.Ack);
                Console.WriteLine("Status: " + response.Status);
                Console.WriteLine("Poruka: " + response.Message);
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("ValidationFault: " + ex.Detail.Message);
            }
            catch (FaultException<FormatFault> ex)
            {
                Console.WriteLine("FormatFault: " + ex.Detail.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška: " + ex.Message);
            }
            finally
            {
                if (factory != null)
                {
                    factory.Close();
                }
            }

            Console.WriteLine("Pritisni ENTER za kraj.");
            Console.ReadLine();
        }
    }
}
