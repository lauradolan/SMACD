using System;

namespace SMACD.PluginHost.Resources
{
    /// <summary>
    ///     Raw TCP/UDP Resource (Socket Port)
    /// </summary>
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

        public override bool IsApproximateTo(Resource resource)
        {
            var socketPort = resource as SocketPortResource;
            if (socketPort == null) return false;

            if (Hostname != socketPort.Hostname) return false;
            if (Protocol != socketPort.Protocol) return false;
            if (Port != socketPort.Port) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SocketPortResource))
                return false;
            var that = (SocketPortResource) obj;

            if (Hostname != that.Hostname)
                return false;
            if (Protocol != that.Protocol)
                return false;
            if (Port != Port)
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