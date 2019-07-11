using SMACD.Shared.Extensions;

namespace SMACD.Shared.Data
{
    /// <summary>
    ///     Stores information about an external service integration
    /// </summary>
    public class IntegrationPointerModel : IModel
    {
        /// <summary>
        ///     Name of the service
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        ///     Fingerprint of this data model
        /// </summary>
        public string GetFingerprint()
        {
            return this.Fingerprint();
        }
    }
}