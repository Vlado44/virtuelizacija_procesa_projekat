using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class Meta
    {
        [DataMember]
        public string ParticipantId { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public int TotalRows { get; set; }

        [DataMember]
        public string SchemaVersion { get; set; }

        public Meta() {}

        public Meta(string participantId, string fileName, int totalRows, string schemaVersion)
        {
            ParticipantId = participantId;
            FileName = fileName;
            TotalRows = totalRows;
            SchemaVersion = schemaVersion;

        }

    }
}
