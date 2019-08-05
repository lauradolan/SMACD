using System.Net.Sockets;

namespace SMACD.Workspace.Targets
{
    public interface IHasPort
    {
        ProtocolType Protocol { get; set; }
        int Port { get; set; }
    }
}
