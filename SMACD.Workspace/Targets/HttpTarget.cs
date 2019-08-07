using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SMACD.Workspace.Targets
{
    /// <summary>
    /// Describes a web (HTTP) target
    /// </summary>
    public class HttpTarget : TargetDescriptor, IHasRemoteHost, IHasPort, IHasResourceAccessMode, IHasResourceLocatorAddress, IHasParameterDictionary
    {
        private Uri Uri => new Uri(_resourceLocatorAddress);

        /// <summary>
        /// Remote Hostname
        /// </summary>
        public string RemoteHost { get; set; }

        /// <summary>
        /// Port Protocol
        /// </summary>
        public ProtocolType Protocol { get; set; }

        /// <summary>
        /// Remote Port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Resource Access Mode (HTTP Method)
        /// </summary>
        public string ResourceAccessMode { get; set; }
        public string Method => ResourceAccessMode;

        private string _resourceLocatorAddress;
        /// <summary>
        /// Resource Locator Address (URL)
        /// </summary>
        public string ResourceLocatorAddress
        {
            get => _resourceLocatorAddress;
            set
            {
                _resourceLocatorAddress = value;
                RemoteHost = Uri.Host;
                Protocol = ProtocolType.Tcp;
                Port = Uri.Port;
            }
        }
        public string URL => ResourceLocatorAddress;

        /// <summary>
        /// Headers to send
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Fields to send
        /// </summary>
        public Dictionary<string, string> Fields { get => Parameters; set => Parameters = value; }
        public Dictionary<string, string> Parameters { get; set; }

        public override string ToString()
        {
            return $"HTTP {Method} {URL} ({Fields.Count} fields, {Headers.Count} headers)";
        }
    }
}
