using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class EegService : IEegService
    {
        private bool sessionStarted = false;
        private int lastRowIndex = -1;
        private int receivedSamples = 0;
        private Meta currentMeta;
        private SessionWriter writer;         
        private readonly string dataRoot;
        private readonly int batteryLowThreshold;
        private readonly int contactQualityMin;
        private readonly double attentionSpikeThreshold;
        private readonly double channelOutOfBandPct;
        private readonly int timestampSkewMaxMs;

        public EegService()
        {
            batteryLowThreshold = int.Parse(ConfigurationManager.AppSettings["BatteryLowThreshold"]);
            contactQualityMin = int.Parse(ConfigurationManager.AppSettings["ContactQualityMin"]);
            attentionSpikeThreshold = double.Parse(ConfigurationManager.AppSettings["AttentionSpikeThreshold"], CultureInfo.InvariantCulture);
            channelOutOfBandPct = double.Parse(ConfigurationManager.AppSettings["ChannelOutOfBandPct"], CultureInfo.InvariantCulture);
            timestampSkewMaxMs = int.Parse(ConfigurationManager.AppSettings["TimestampSkewMaxMs"]);

            dataRoot = ConfigurationManager.AppSettings["DataRoot"];
            if (string.IsNullOrEmpty(dataRoot))
                dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        }

        public ServiceResponse StartSession(Meta meta)
        {
            if(meta == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Meta podaci ne smeju biti null!"));
            }

            if(string.IsNullOrWhiteSpace(meta.ParticipantId))
            {
                throw new FaultException<ValidationFault>(new ValidationFault("PatricipantId je obavezan!"));
            }

            if (string.IsNullOrWhiteSpace(meta.FileName))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("FileName je obavezan."));
            }

            if (meta.TotalRows < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TotalRows ne sme biti negativan."));
            }

            sessionStarted = true;
            lastRowIndex = -1;
            receivedSamples = 0;
            currentMeta = meta;

            CloseWriter();   
            writer = new SessionWriter(dataRoot, meta.ParticipantId, DateTime.Now);

            Console.WriteLine("Start sesije:");
            Console.WriteLine("ParticipantId: " + meta.ParticipantId);
            Console.WriteLine("FileName: " + meta.FileName);
            Console.WriteLine("TotalRows: " + meta.TotalRows);
            Console.WriteLine("SchemaVersion: " + meta.SchemaVersion);
            Console.WriteLine("");
           
            return new ServiceResponse(true, TransferStatus.IN_PROGRESS, "Session started.");

        }

        public ServiceResponse PushSample(Sample sample)
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije pokrenuta. Prvo pozovi StartSession."));
            }

            ValidateSample(sample);

            writer.WriteSample(sample);

            lastRowIndex = sample.RowIndex;
            receivedSamples++;

            if (sample.RowIndex % 1000 == 0)
            {
                Console.WriteLine("Primljen red: " + sample.RowIndex);
            }

            return new ServiceResponse(true, TransferStatus.IN_PROGRESS, "Sample received.");
        }

        public ServiceResponse EndSession()
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sesija nije pokrenuta."));
            }

            sessionStarted = false;

            CloseWriter();

            Console.WriteLine("Završena sesija.");
            Console.WriteLine("Ukupno primljenih uzoraka: " + receivedSamples);

            return new ServiceResponse(true, TransferStatus.COMPLETED, "Session completed.");
        }

        private void ValidateSample(Sample sample)
        {
            if (sample == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sample ne sme biti null."));
            }

            if (sample.RowIndex <= lastRowIndex)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("RowIndex mora monotono da raste."));
            }

            if (sample.Timestamp == DateTime.MinValue)
            {
                throw new FaultException<FormatFault>(new FormatFault("Timestamp nije validan."));
            }

            ValidateChannel(sample.AF3, "AF3");
            ValidateChannel(sample.T7, "T7");
            ValidateChannel(sample.Pz, "Pz");
            ValidateChannel(sample.T8, "T8");
            ValidateChannel(sample.AF4, "AF4");

            ValidateMetric(sample.Attention, "Attention");
            ValidateMetric(sample.Engagement, "Engagement");
            ValidateMetric(sample.Excitement, "Excitement");
            ValidateMetric(sample.Interest, "Interest");
            ValidateMetric(sample.Relaxation, "Relaxation");
            ValidateMetric(sample.Stress, "Stress");

            if (sample.Battery < 0 || sample.Battery > 100)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Battery mora biti u opsegu 0-100."));
            }

            if (sample.ContactQuality < 0 || sample.ContactQuality > 100)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("ContactQuality mora biti u opsegu 0-100."));
            }

            
            /*
            if (sample.SlideIndex < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("SlideIndex ne sme biti negativan."));
            }

            if (sample.SetIndex < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("SetIndex ne sme biti negativan."));
            }
            */

            if (sample.Battery < batteryLowThreshold)
            {
                Console.WriteLine("Upozorenje: Battery je ispod praga.(20) Vrednost: " + sample.Battery);
            }

            if (sample.ContactQuality < contactQualityMin)
            {
                Console.WriteLine("Upozorenje: ContactQuality je ispod praga.(70) Vrednost: " + sample.ContactQuality);
            }
        }

        private void ValidateChannel(double value, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(fieldName + " mora biti realan broj."));
            }

            if (value < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(fieldName + " ne sme biti negativan."));
            }
        }

        private void ValidateMetric(double value, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(fieldName + " mora biti realan broj."));
            }

            if (value < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(fieldName + " ne sme biti negativan."));
            }
        }

        private void CloseWriter()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }   
    }
}
