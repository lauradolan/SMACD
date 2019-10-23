using SMACD.Data;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     IUnderstandServiceMaps indicates that the Extension needs access to the Service Map in use
    /// </summary>
    public interface IUnderstandServiceMaps
    {
        /// <summary>
        ///     Framework-populated reference to the Service Map
        /// </summary>
        ServiceMapFile ServiceMap { get; set; }
    }
}