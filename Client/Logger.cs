using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Client
{
    public class Logger
    {
        private readonly string logPath;
        
        public Logger(string logPath)
        {
            this.logPath = logPath;
        }

        public void LogBadRow(string fileName, int rowIndex, string rawLine, string reason)
        {
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine("Time: "+DateTime.Now);
                writer.WriteLine("File: "+fileName);
                writer.WriteLine("Row: "+rowIndex);
                writer.WriteLine("Reason: "+reason);
                writer.WriteLine("RawLine: " + rawLine);
                writer.WriteLine("--------------------------------");
            }
        }
    }
}
