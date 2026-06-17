using System;

namespace Server
{
    public class TransferStartedEventArgs : EventArgs
    {
        public string ParticipantId { get; }
        public string FileName { get; }
        public int TotalRows { get; }
        public DateTime Time { get; }
    
        public TransferStartedEventArgs(string participantId, string fileName, int totalRows, DateTime time)
        {
            ParticipantId = participantId;
            FileName = fileName;
            TotalRows = totalRows;
            Time = time;
        }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public string ParticipantId { get; }
        public int RowIndex { get; }
        public int ReceivedCount { get; }

        public SampleReceivedEventArgs(string participantId, int rowIndex, int receivedCount)
        {
            ParticipantId = participantId;
            RowIndex = rowIndex;
            ReceivedCount = receivedCount;
        }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public string ParticipantId { get; }
        public int TotalReceived { get; }
        public DateTime Time { get; }

        public TransferCompletedEventArgs(string participantId, int totalReceived, DateTime time)
        {
            ParticipantId = participantId;
            TotalReceived = totalReceived;
            Time = time;
        }
    }

    public class WarningEventArgs : EventArgs
    {
        public WarningType Type { get; }
        public string ParticipantId { get; }
        public DateTime Timestamp { get; }
        public int RowIndex { get; }
        public string Message { get; }
        public double? Before { get; }
        public double? After { get; }

        public WarningEventArgs(WarningType type, string participantId, DateTime timestamp,
                                int rowIndex, string message, double? before = null, double? after = null)
        {
            Type = type;
            ParticipantId = participantId;
            Timestamp = timestamp;
            RowIndex = rowIndex;
            Message = message;
            Before = before;
            After = after;
        }
    }


}
