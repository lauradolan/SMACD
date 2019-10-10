using SMACD.AppTree;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     IUnderstandProjectInformation indicates that the Extension needs to know what business object is related to its
    ///     instance.
    /// </summary>
    public interface IUnderstandProjectInformation
    {
        /// <summary>
        ///     Framework-populated reference to the project information
        /// </summary>
        ProjectPointer ProjectPointer { get; set; }
    }
}