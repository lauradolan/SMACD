namespace SMACD.AppTree.Details
{
    /// <summary>
    ///     Details around a Service
    /// </summary>
    public class ServiceDetails
    {
        /// <summary>
        ///     Service name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        ///     Service banner
        /// </summary>
        public string ServiceBanner { get; set; }

        /// <summary>
        ///     Name of product providing the Service
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        ///     Version of product providing the Service
        /// </summary>
        public string ProductVersion { get; set; }
    }
}
