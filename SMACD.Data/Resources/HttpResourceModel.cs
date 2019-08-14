using System.Collections.Generic;

namespace SMACD.Data.Resources
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public class HttpResourceModel : TargetModel
    {
        public Dictionary<string, string> Fields = new Dictionary<string, string>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public string Method { get; set; }
        public string Url { get; set; }
    }
}