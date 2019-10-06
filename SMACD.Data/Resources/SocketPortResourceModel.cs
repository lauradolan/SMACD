namespace SMACD.Data.Resources
{
    /// <summary>
    ///     Represents a Target resolved to its handler
    /// </summary>
    public class SocketPortTargetModel : TargetModel
    {
        /// <summary>
        ///     Hostname or IP address of system to connect to
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        ///     Protocol used to connect (default is TCP)
        /// </summary>
        public string Protocol { get; set; } = "TCP";

        /// <summary>
        ///     Port number to connect to
        /// </summary>
        public int Port { get; set; }
    }
}