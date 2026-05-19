using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.ServiceModel;
using System.IO;
using System.Globalization;


namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //TestDisposePattern();

            ChannelFactory<IEegService> factory = null;
            IEegService proxy = null;

            try
            {
                factory = new ChannelFactory<IEegService>("EegService");
                proxy = factory.CreateChannel();

                Console.WriteLine("Unesi putanju do EEG foldera:");
                string eegFolderPath = Console.ReadLine();

                if (!Directory.Exists(eegFolderPath))
                {
                    Console.WriteLine("Folder ne postoji.");
                    return;
                }

                Logger logger = new Logger("client_bad_rows.log");

                string[] files = Directory.GetFiles(
                    eegFolderPath,
                    "subject_*_results.csv",
                    SearchOption.AllDirectories
                );

                Console.WriteLine("Pronađeno fajlova: " + files.Length);

                foreach(string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string participantId = ExtractParticipantId(filePath);
                    int totalRows = CountDataRows(filePath);

                    Meta meta = new Meta(participantId, fileName, totalRows, "1.0");

                    ServiceResponse startResponse = proxy.StartSession(meta);

                    Console.WriteLine("Start sesije za fajl: "+fileName);
                    Console.WriteLine("ACK: " +startResponse.Ack);
                    Console.WriteLine("Status: " +startResponse.Status);
                    Console.WriteLine("Poruka: " +startResponse.Message);

                    using (StreamReader reader  = new StreamReader(filePath))
                    {
                        string header = reader.ReadLine();
                        int rowIndex = 0;

                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            rowIndex++;

                            try
                            {
                                Sample sample = ParseSample(line, rowIndex);

                                ServiceResponse sampleResponse = proxy.PushSample(sample);

                                if (!sampleResponse.Ack)
                                {
                                    logger.LogBadRow(fileName, rowIndex, line, sampleResponse.Message);
                                }
                            }
                            catch(FaultException<ValidationFault> ex)
                            {
                                logger.LogBadRow(fileName, rowIndex, line, ex.Detail.Message);
                            }
                            catch (FaultException<FormatFault> ex)
                            {
                                logger.LogBadRow(fileName, rowIndex, line, ex.Detail.Message);
                            }
                            catch (CommunicationException ex)
                            {
                                Console.WriteLine("Greška u WCF komunikaciji: " + ex.Message);
                                Console.WriteLine("Prekidam obradu trenutnog fajla jer je kanal u faulted stanju.");
                                break;
                            }
                            catch (Exception ex)
                            {
                                logger.LogBadRow(fileName, rowIndex, line, ex.Message);
                            }
                        }
                    }

                    ServiceResponse endResponse = proxy.EndSession();

                    Console.WriteLine("Kraj sesije za fajl: " + fileName);
                    Console.WriteLine("ACK: " + endResponse.Ack);
                    Console.WriteLine("Status: " + endResponse.Status);
                    Console.WriteLine("Poruka: " + endResponse.Message);
                    Console.WriteLine("----------------------------------------");

                }

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

        private static string ExtractParticipantId(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            return fileName
                .Replace("subject_", "")
                .Replace("_results", "");
        }

        private static int CountDataRows(string filePath)
        {
            return File.ReadLines(filePath).Skip(1).Count();
        }

        private static Sample ParseSample(string line, int rowIndex)
        {
            string[] parts = line.Split(',');

            if (parts.Length != 16)
            {
                throw new FormatException("CSV red nema očekivanih 16 kolona.");
            }

            return new Sample
            {
                Timestamp = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),

                AF3 = double.Parse(parts[1], CultureInfo.InvariantCulture),
                T7 = double.Parse(parts[2], CultureInfo.InvariantCulture),
                Pz = double.Parse(parts[3], CultureInfo.InvariantCulture),
                T8 = double.Parse(parts[4], CultureInfo.InvariantCulture),
                AF4 = double.Parse(parts[5], CultureInfo.InvariantCulture),

                Attention = double.Parse(parts[6], CultureInfo.InvariantCulture),
                Engagement = double.Parse(parts[7], CultureInfo.InvariantCulture),
                Excitement = double.Parse(parts[8], CultureInfo.InvariantCulture),
                Interest = double.Parse(parts[9], CultureInfo.InvariantCulture),
                Relaxation = double.Parse(parts[10], CultureInfo.InvariantCulture),
                Stress = double.Parse(parts[11], CultureInfo.InvariantCulture),

                Battery = int.Parse(parts[12], CultureInfo.InvariantCulture),
                ContactQuality = int.Parse(parts[13], CultureInfo.InvariantCulture),
                SlideIndex = int.Parse(parts[14], CultureInfo.InvariantCulture),
                SetIndex = int.Parse(parts[15], CultureInfo.InvariantCulture),

                RowIndex = rowIndex
            };
        }

        /*
        private static void TestDisposePattern()
        {
            try
            {
                using (DisposableFileWriter writer = new DisposableFileWriter("dispose_test.log"))
                {
                    writer.WriteLine("Test Dispose pattern-a je pokrenut.");

                    throw new Exception("Simulacija izuzetka tokom rada sa fajlom.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dispose test - uhvaćen izuzetak: " + ex.Message);
                Console.WriteLine("Resurs je zatvoren jer je korišćen using blok.");
            }
        }
        */
    }
}
