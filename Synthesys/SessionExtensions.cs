using SMACD.Artifacts;
using SMACD.Data;
using SMACD.Data.Resources;
using Synthesys.SDK;
using Synthesys.Tasks;
using System;
using System.Linq;
using System.Net.Sockets;

namespace Synthesys
{
    public static class TargetRegistrationExtensions
    {
        public static void RegisterTarget(this Session session, TargetModel resourceModel)
        {
            if (resourceModel is HttpTargetModel)
            {
                HttpTargetModel http = resourceModel as HttpTargetModel;
                Uri uri = new Uri(http.Url);
                System.Collections.Generic.List<string> pieces = uri.AbsolutePath.Split('/').ToList();

                ServicePortArtifact serviceBase = session.Artifacts[uri.Host][uri.Port];
                if (!(serviceBase is HttpServicePortArtifact))
                {
                    HttpServicePortArtifact httpArtifact = new HttpServicePortArtifact() { Parent = session.Artifacts[uri.Host] };
                    httpArtifact.Identifiers.Add($"{ProtocolType.Tcp.ToString()}/{uri.Port}");
                    session.Artifacts[uri.Host][uri.Port] = httpArtifact;

                    ServicePortArtifact httpBase = session.Artifacts[uri.Host][uri.Port];
                    httpBase.Metadata.Set(serviceBase.Metadata, "_known_", DataProviderSpecificity.Explicit);
                }

                UrlArtifact pathTip = UrlHelper.GeneratePathArtifacts(
                    (HttpServicePortArtifact)session.Artifacts[uri.Host][uri.Port], uri.AbsolutePath, http.Method);

                pathTip.AddRequest("GET", http.Fields, http.Headers);
            }

            if (resourceModel is SocketPortTargetModel)
            {
                SocketPortTargetModel socket = resourceModel as SocketPortTargetModel;
                session.Artifacts[socket.Hostname][$"{socket.Protocol}/{socket.Port}"] = new ServicePortArtifact();
            }
        }
    }
}