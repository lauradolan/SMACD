namespace Synthesys.Plugins.Nikto
{
    public class NiktoReport
    {
        public string NiktoVersion { get; set; }
        public string ScanStart { get; set; }
        public string ScanEnd { get; set; }
        public string ServerBanner { get; set; }
        public string SiteName { get; set; }
    }
}