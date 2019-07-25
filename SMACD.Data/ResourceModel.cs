namespace SMACD.Data
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public abstract class ResourceModel : IModel
    {
        /// <summary>
        ///     Resource identifier
        /// </summary>
        public string ResourceId { get; set; }
    }
}