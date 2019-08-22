namespace SMACD.Data.Resources
{
    /// <summary>
    ///     Represents a Target resolved to its handler
    /// </summary>
    public class SocketPortTargetModel : TargetModel
    {
        public string Hostname { get; set; }
        public string Protocol { get; set; } = "TCP";
        public int Port { get; set; }
    }
}