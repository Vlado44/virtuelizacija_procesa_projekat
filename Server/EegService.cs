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
        private bool hasPrev = false;
        private double prevAttention = 0;
        private double prevEngagement = 0;
        private Meta currentMeta;
        private SessionWriter writer;         
        private readonly string dataRoot;
        private readonly int batteryLowThreshold;
        private readonly int contactQualityMin;
        private readonly double attentionSpikeThreshold;
        private readonly double channelOutOfBandPct;
        private readonly int timestampSkewMaxMs;
        private readonly double engagementDropThreshold;

        public EegService()
        {
            batteryLowThreshold = int.Parse(ConfigurationManager.AppSettings["BatteryLowThreshold"]);
            contactQualityMin = int.Parse(ConfigurationManager.AppSettings["ContactQualityMin"]);
            attentionSpikeThreshold = double.Parse(ConfigurationManager.AppSettings["AttentionSpikeThreshold"], CultureInfo.InvariantCulture);
            channelOutOfBandPct = double.Parse(ConfigurationManager.AppSettings["ChannelOutOfBandPct"], CultureInfo.InvariantCulture);
            timestampSkewMaxMs = int.Parse(ConfigurationManager.AppSettings["TimestampSkewMaxMs"]);
            engagementDropThreshold = double.Parse(
            ConfigurationManager.AppSettings["EngagementDropThreshold"], CultureInfo.InvariantCulture);

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

            EegPublisher.Instance.RaiseTransferStarted(
            new TransferStartedEventArgs(meta.ParticipantId, meta.FileName, meta.TotalRows, DateTime.Now));

            return new ServiceResponse(true, TransferStatus.IN_PROGRESS, "Session started.");

        }

        public ServiceResponse PushSample(Sample sample)
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije pokrenuta. Prvo pozovi StartSession."));
            }

            if (sample == null)
                throw new FaultException<ValidationFault>(new ValidationFault("Sample ne sme biti null."));

            if (sample.RowIndex <= lastRowIndex)
            {
                RaiseWarning(WarningType.DuplicateRow, sample,
                    "RowIndex ne raste monotono (RowIndex=" + sample.RowIndex +
                    ", prethodni=" + lastRowIndex + ")");

                return new ServiceResponse(true, TransferStatus.IN_PROGRESS, "Duplicate row skipped.");
            }

            ValidateSample(sample);

            writer.WriteSample(sample);

            lastRowIndex = sample.RowIndex;
            receivedSamples++;

            EegPublisher.Instance.RaiseSampleReceived(
            new SampleReceivedEventArgs(currentMeta.ParticipantId, sample.RowIndex, receivedSamples));

            if (sample.Battery < batteryLowThreshold)
                RaiseWarning(WarningType.LowBattery, sample,
                    "Nizak nivo baterije (Battery=" + sample.Battery + ")");

            if (sample.ContactQuality < contactQualityMin)
                RaiseWarning(WarningType.PoorContact, sample,
                    "Los kvalitet kontakta (ContactQuality=" + sample.ContactQuality + ")");

            RunAnalytics(sample);          

            prevAttention = sample.Attention;   
            prevEngagement = sample.Engagement; 
            hasPrev = true;              

            return new ServiceResponse(true, TransferStatus.IN_PROGRESS, "Sample received.");
        }

        public ServiceResponse EndSession()
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sesija nije pokrenuta."));
            }

            sessionStarted = false;

            EegPublisher.Instance.RaiseTransferCompleted(
            new TransferCompletedEventArgs(currentMeta.ParticipantId, receivedSamples, DateTime.Now));

            CloseWriter();

           

            return new ServiceResponse(true, TransferStatus.COMPLETED, "Session completed.");
        }

        private void ValidateSample(Sample sample)
        {
            if (sample == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sample ne sme biti null."));
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

        private void RunAnalytics(Sample sample)
        {
            if (!hasPrev)
                return;

            double dAttention = sample.Attention - prevAttention;
            double dEngagement = sample.Engagement - prevEngagement;

            // |ΔAttention| > prag -> AttentionSpike 
            if (Math.Abs(dAttention) > attentionSpikeThreshold)
            {
                string smer = dAttention > 0 ? "porast" : "pad";
                string poruka = "Nagla promena paznje (" + smer + " " +
                                Math.Abs(dAttention).ToString("F3", CultureInfo.InvariantCulture) + ")";

                RaiseWarning(WarningType.AttentionSpike, sample, poruka, prevAttention, sample.Attention);
            }

            // isto pravilo: pad angažovanosti
            if (dEngagement < -engagementDropThreshold)
            {
                string poruka = "Nagli pad angazovanosti (pad " +
                                Math.Abs(dEngagement).ToString("F3", CultureInfo.InvariantCulture) + ")";

                RaiseWarning(WarningType.EngagementDrop, sample, poruka, prevEngagement, sample.Engagement);
            }
        }

        private void RaiseWarning(WarningType type, Sample sample, string message,
                          double? before = null, double? after = null)
        {
            EegPublisher.Instance.RaiseWarning(new WarningEventArgs(
                type, currentMeta.ParticipantId, sample.Timestamp,
                sample.RowIndex, message, before, after));

            if (writer != null)
                writer.WriteReject(type + ": " + message, SessionWriter.BuildRawRow(sample));
        }
    }
}
