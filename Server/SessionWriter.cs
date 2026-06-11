using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Globalization;
using System.IO;


namespace Server
{
    public class SessionWriter : IDisposable
    {
        private FileStream sessionStream;
        private StreamWriter sessionWriter;

        private FileStream rejectsStream;
        private StreamWriter rejectsWriter;

        private bool disposed = false;

        public string SessionPath { get; private set; }
        public string RejectsPath { get; private set; }

        private const string SampleHeader =
            "Timestamp,AF3,T7,Pz,T8,AF4,Attention,Engagement,Excitement,Interest," +
            "Relaxation,Stress,Battery,ContactQuality,SlideIndex,SetIndex,RowIndex";

        public SessionWriter(string dataRoot, string participantId, DateTime sessionDate)
        {
            string folder = Path.Combine(
                dataRoot,
                participantId,
                sessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(folder);   // pravi i sve poddirektorijume

            SessionPath = Path.Combine(folder, "session.csv");
            RejectsPath = Path.Combine(folder, "rejects.csv");

            // Rotacija po sesiji: FileMode.Create -> svaka sesija počinje od nule.
            sessionStream = new FileStream(SessionPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            sessionWriter = new StreamWriter(sessionStream, Encoding.UTF8);
            sessionWriter.WriteLine(SampleHeader);
            sessionWriter.Flush();

            rejectsStream = new FileStream(RejectsPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            rejectsWriter = new StreamWriter(rejectsStream, Encoding.UTF8);
            rejectsWriter.WriteLine("Time,Reason,RawRow");
            rejectsWriter.Flush();
        }

        public void WriteSample(Sample sample)
        {
            if (disposed)
                throw new ObjectDisposedException("SessionWriter");

            sessionWriter.WriteLine(BuildRawRow(sample));
            sessionWriter.Flush();
        }

        public void WriteReject(string reason, string rawRow)
        {
            if (disposed)
                throw new ObjectDisposedException("SessionWriter");

            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            rejectsWriter.WriteLine(time + "," + Escape(reason) + "," + Escape(rawRow));
            rejectsWriter.Flush();
        }

        public static string BuildRawRow(Sample s)
        {
            var ci = CultureInfo.InvariantCulture;
            return string.Join(",", new string[]
            {
                s.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", ci),
                s.AF3.ToString(ci), s.T7.ToString(ci), s.Pz.ToString(ci),
                s.T8.ToString(ci), s.AF4.ToString(ci),
                s.Attention.ToString(ci), s.Engagement.ToString(ci), s.Excitement.ToString(ci),
                s.Interest.ToString(ci), s.Relaxation.ToString(ci), s.Stress.ToString(ci),
                s.Battery.ToString(ci), s.ContactQuality.ToString(ci),
                s.SlideIndex.ToString(ci), s.SetIndex.ToString(ci), s.RowIndex.ToString(ci)
            });
        }

        private static string Escape(string value)
        {
            if (value == null) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // zatvaramo u obrnutom redosledu otvaranja
                    if (sessionWriter != null) { sessionWriter.Dispose(); sessionWriter = null; }
                    if (sessionStream != null) { sessionStream.Dispose(); sessionStream = null; }
                    if (rejectsWriter != null) { rejectsWriter.Dispose(); rejectsWriter = null; }
                    if (rejectsStream != null) { rejectsStream.Dispose(); rejectsStream = null; }
                }
                disposed = true;
            }
        }

        ~SessionWriter()
        {
            Dispose(false);
        }
    }
}
