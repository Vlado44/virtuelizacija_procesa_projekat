using System;
using System.Globalization;

namespace Server
{
    public class ConsoleSubscriber
    {
        public ConsoleSubscriber(EegPublisher publisher)
        {
            publisher.OnTransferStarted += HandleTransferStarted;
            publisher.OnSampleReceived += HandleSampleReceived;
            publisher.OnTransferCompleted += HandleTransferCompleted;
            publisher.OnWarningRaised += HandleWarning;
        }

        private void HandleTransferStarted(object sender, TransferStartedEventArgs e)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("[START] Ucesnik " + e.ParticipantId +
                              " (" + e.FileName + "), ocekivano redova: " + e.TotalRows);
            Console.WriteLine("prenos u toku...");
        }

        private void HandleSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            if (e.ReceivedCount % 1000 == 0)
                Console.WriteLine("prenos u toku... primljeno uzoraka: " + e.ReceivedCount);
        }

        private void HandleTransferCompleted(object sender, TransferCompletedEventArgs e)
        {
            Console.WriteLine("zavrsen prenos.");
            Console.WriteLine("[END] Ucesnik " + e.ParticipantId +
                              ", ukupno primljenih uzoraka: " + e.TotalReceived);
            Console.WriteLine("----------------------------------------");
        }

        private void HandleWarning(object sender, WarningEventArgs e)
        {
            string line = "[UPOZORENJE][" + e.Type + "] ucesnik " + e.ParticipantId +
                          ", red " + e.RowIndex + ": " + e.Message;

            if (e.Before.HasValue && e.After.HasValue)
                line += " (pre=" + e.Before.Value.ToString("F3", CultureInfo.InvariantCulture) +
                        ", posle=" + e.After.Value.ToString("F3", CultureInfo.InvariantCulture) + ")";

            Console.WriteLine(line);
        }
    }
}
