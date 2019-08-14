namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about a Resource and how to use it with a plugin
    /// </summary>
    public class TargetPointerModel : PointerModel, IModel
    {
        /// <summary>
        ///     Resource identifier to associate with Resource Map
        /// </summary>
        public string TargetId
        {
            get => TargetIdentifier;
            set => TargetIdentifier = value;
        }
    }
}