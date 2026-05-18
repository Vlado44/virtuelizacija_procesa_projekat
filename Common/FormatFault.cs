using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class FormatFault
    {
        [DataMember]
        public string Message { get; set; }

        public FormatFault() { }

        public FormatFault(string message)
        {
            Message = message;
        }

    }
}
