namespace SMACD.Workspace.Targets
{
    /// <summary>
    /// Describes a target host and port
    /// </summary>
    public class RawPortTarget : TargetDescriptor
    {
        /// <summary>
        /// Target hostname
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Target port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Protocol (TCP or UDP, default TCP)
        /// </summary>
        public string Protocol { get; set; } = "TCP";
    }
}
