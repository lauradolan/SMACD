using System;
using System.Collections.Generic;
using System.Text;

namespace SMACD.Plugins.Nmap
{
    public class NmapRun
    {
        public DateTime RunTime { get; set; }
        public IList<NmapPort> Ports { get; set; } = new List<NmapPort>();
    }

    public class NmapPort
    {
        public string Protocol { get; set; }
        public int Port { get; set; }
        public string Service { get; set; }
        public int ServiceFingerprintConfidence { get; set; }
    }
}
