namespace SMACD.Data
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public class SocketPortResourceModel : ResourceModel
    {
        public string Hostname { get; set; }
        public string Protocol { get; set; } = "TCP";
        public int Port { get; set; }
    }
}