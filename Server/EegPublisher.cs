
namespace Server
{
    public class EegPublisher
    {
        public static readonly EegPublisher Instance = new EegPublisher();

        // delegati 
        public delegate void TransferStartedEventHandler(object sender, TransferStartedEventArgs e);
        public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
        public delegate void TransferCompletedEventHandler(object sender, TransferCompletedEventArgs e);
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);

        // dogadjaji
        public event TransferStartedEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferCompletedEventHandler OnTransferCompleted;
        public event WarningEventHandler OnWarningRaised;

        // okidanje dogadjaja
        public void RaiseTransferStarted(TransferStartedEventArgs e)
        {
            if (OnTransferStarted != null)
                OnTransferStarted(this, e);
        }

        public void RaiseSampleReceived(SampleReceivedEventArgs e)
        {
            if (OnSampleReceived != null)
                OnSampleReceived(this, e);
        }

        public void RaiseTransferCompleted(TransferCompletedEventArgs e)
        {
            if (OnTransferCompleted != null)
                OnTransferCompleted(this, e);
        }

        public void RaiseWarning(WarningEventArgs e)
        {
            if (OnWarningRaised != null)
                OnWarningRaised(this, e);
        }
    }
}
