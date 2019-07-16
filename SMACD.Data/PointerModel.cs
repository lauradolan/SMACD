using System.Collections.Generic;

namespace SMACD.Data
{
    public abstract class PointerModel
    {
        public string TargetIdentifier { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return $"({GetType().Name}) Target: '{TargetIdentifier}' | Params: " +
                   string.Join(", ", string.Join("=", Parameters));
        }
    }
}