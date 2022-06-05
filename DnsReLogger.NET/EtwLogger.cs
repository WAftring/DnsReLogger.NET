using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace DnsReLogger.NET
{
    static internal class EtwLogger
    {
        const string LoggerBaseName = "DnsReLogger.NET";
        const string DNS_SERVER_GUID = "EB79061A-A566-4698-9119-3ED2807060E7";
        
        static public bool IsRunning { get; private set; }
        public static bool Start(bool Live)
        {
            using (var session = new TraceEventSession(LoggerBaseName))
            {
                session.EnableProvider(Guid.Parse(DNS_SERVER_GUID), )
            }
            return true;
        }
        public static bool Stop()
        {

        }
    }
}
