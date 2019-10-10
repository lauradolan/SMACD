using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about a plugin and how to run it against an abuse case
    /// </summary>
    public class ActionPointerModel : IModel
    {
        /// <summary>
        ///     Provides the unique identifier for the Action associated with this Pointer
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        ///     Parameters to pass to Action to adjust its execution
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Target to be acted upon by the Action
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        ///     String representation of Action with configuration
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Action: '{Action}' | Target: '{Target}' | Params: " +
                   string.Join(", ", string.Join("=", Options));
        }
    }
}