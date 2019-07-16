using System;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;

namespace SMACD.ScannerEngine.Resources
{
    /// <summary>
    ///     Raw TCP/UDP Resource (Socket Port)
    /// </summary>
    [PluginMetadata("socketport")]
    public class SocketPortResource : Resource
    {
        public string Hostname { get; set; }
        public string Protocol { get; set; }
        public int Port { get; set; }
        public string ServiceGuess { get; set; }

        public override string GetDescription()
        {
            return $"Raw - {Hostname}:{Protocol} [{Port}] (Service guess is {ServiceGuess})";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SocketPortResource))
                return false;
            var that = (SocketPortResource) obj;

            if (this.Hostname != that.Hostname)
                return false;
            if (this.Protocol != that.Protocol)
                return false;
            if (this.Port != this.Port)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Hostname,
                Protocol,
                Port
            );
        }

        public override string ToString()
        {
            return GetDescription();
        }
    }
}