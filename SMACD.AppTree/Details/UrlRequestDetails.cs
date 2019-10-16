using System.Collections.Generic;

namespace SMACD.AppTree.Details
{
    /// <summary>
    ///     Details around a URL segment
    /// </summary>
    public class UrlRequestDetails
    {
        /// <summary>
        ///     HTML generated from executing the URL with the given parameters
        /// </summary>
        public string ResultHtml { get; set; }

        /// <summary>
        ///     Result code when requesting URL
        /// </summary>
        public int ResultCode { get; set; }

        /// <summary>
        ///     Headers received during request
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
