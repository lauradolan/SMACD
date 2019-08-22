using SMACD.Artifacts;

namespace SMACD.SDK.Capabilities
{
    public interface IUnderstandProjectInformation
    {
        /// <summary>
        /// Project information pertaining to this run
        /// </summary>
        ProjectPointer ProjectPointer { get; set; }
    }
}
