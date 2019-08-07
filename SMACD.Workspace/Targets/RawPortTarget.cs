using System.Net.Sockets;

namespace SMACD.Workspace.Targets
{
    /// <summary>
    /// Describes a target host and port
    /// </summary>
    public class RawPortTarget : TargetDescriptor, IHasRemoteHost, IHasPort
    {
        /// <summary>
        /// Target hostname
        /// </summary>
        public string RemoteHost { get; set; }

        /// <summary>
        /// Protocol (default TCP)
        /// </summary>
        public ProtocolType Protocol { get; set; }

        /// <summary>
        /// Target port
        /// </summary>
        public int Port { get; set; }

        public override string ToString()
        {
            return $"RAW {RemoteHost}:{Protocol.ToString().ToUpper()}/{Port}";
        }
    }
}
