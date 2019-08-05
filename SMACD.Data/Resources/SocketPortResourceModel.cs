namespace SMACD.Data.Resources
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public class SocketPortResourceModel : TargetModel
    {
        public string Hostname { get; set; }
        public string Protocol { get; set; } = "TCP";
        public int Port { get; set; }
    }
}