namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about an external service integration
    /// </summary>
    public class IntegrationPointerModel : PointerModel, IModel
    {
        /// <summary>
        ///     Name of the service
        /// </summary>
        public string Service
        {
            get => TargetIdentifier;
            set => TargetIdentifier = value;
        }
    }
}