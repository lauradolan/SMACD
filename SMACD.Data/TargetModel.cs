namespace SMACD.Data
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public abstract class TargetModel : IModel
    {
        /// <summary>
        ///     Resource identifier
        /// </summary>
        public string TargetId { get; set; }
    }
}