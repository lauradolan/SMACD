using System.Collections.Generic;

namespace SMACD.Workspace.Targets
{
    public interface IHasParameterDictionary
    {
        Dictionary<string, string> Parameters { get; set; }
    }
}
