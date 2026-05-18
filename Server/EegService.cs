using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.ServiceModel;


namespace Server
{
    public class EegService : IEegService
    {
        private bool sessionStarted = false;
        private int lastRowIndex = -1;
        private int receivedSamples = 0;
        private Meta currentMeta;

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

            Console.WriteLine("Start sesije:");
            Console.WriteLine("ParticipantId: " + meta.ParticipantId);
            Console.WriteLine("FileName: " + meta.FileName);
            Console.WriteLine("TotalRows: " + meta.TotalRows);
            Console.WriteLine("SchemaVersion: " + meta.SchemaVersion);

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

            lastRowIndex = sample.RowIndex;
            receivedSamples++;

            Console.WriteLine("Primljen red: " + sample.RowIndex);

            return new ServiceResponse(true, TransferStatus.IN_PROGRESS, "Sample received.");
        }

        public ServiceResponse EndSession()
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sesija nije pokrenuta."));
            }

            sessionStarted = false;

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

            if (sample.Battery < 0 || sample.Battery > 100)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Battery mora biti u opsegu 0-100."));
            }

            if (sample.ContactQuality < 0 || sample.ContactQuality > 100)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("ContactQuality mora biti u opsegu 0-100."));
            }

            ValidateNonNegative(sample.Attention, "Attention");
            ValidateNonNegative(sample.Engagement, "Engagement");
            ValidateNonNegative(sample.Excitement, "Excitement");
            ValidateNonNegative(sample.Interest, "Interest");
            ValidateNonNegative(sample.Relaxation, "Relaxation");
            ValidateNonNegative(sample.Stress, "Stress");
        }

        private void ValidateNonNegative(double value, string fieldName)
        {
            if (value < 0)
            {
                throw new FaultException<ValidationFault>(new ValidationFault(fieldName + " ne sme biti negativan."));
            }
        }

    }
}
