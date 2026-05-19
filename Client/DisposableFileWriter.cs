using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Client
{
    public class DisposableFileWriter : IDisposable
    {
        private StreamWriter writer;
        private bool disposed = false;

        public string Path {  get; private set; }

        public DisposableFileWriter(string path)
        {
            Path = path;
            writer = new StreamWriter(path, true);
        }

        public void WriteLine(string text)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("DisposableFileWriter");
            }

            writer.WriteLine(text);
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
                    if (writer != null)
                    {
                        writer.Dispose();
                        writer = null;
                    }
                }

                disposed = true;
            }
        }

    }
}
