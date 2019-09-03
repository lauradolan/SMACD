using SMACD.Artifacts;
using SMACD.SDK.Attributes;

namespace SMACD.GUI.Models
{
    public class AddActionModel
    {
        public ExtensionAttribute SelectedAction { get; set; }
        public Artifact Target { get; set; }
    }
}
