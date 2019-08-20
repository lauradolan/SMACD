using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about a plugin and how to run it against an abuse case
    /// </summary>
    public class ActionPointerModel : PointerModel, IModel
    {
        /// <summary>
        ///     Provides the unique identifier for the Action associated with this Pointer
        /// </summary>
        public string Action
        {
            get => TargetIdentifier;
            set => TargetIdentifier = value;
        }

        /// <summary>
        ///     Parameters to pass to Action to adjust its execution
        /// </summary>
        public Dictionary<string, string> Options
        {
            get => Parameters;
            set => Parameters = value;
        }

        /// <summary>
        ///     Target to be acted upon by the Action
        /// </summary>
        public TargetPointerModel Target { get; set; }
    }
}