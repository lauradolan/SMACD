using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public class HttpResourceModel : ResourceModel
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Fields = new Dictionary<string, string>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
    }
}