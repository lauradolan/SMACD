using System;
using System.Linq;
using System.Net.Sockets;
using SMACD.Artifacts;
using SMACD.Data;
using SMACD.Data.Resources;
using Synthesys.SDK;
using Synthesys.Tasks;

namespace Synthesys
{
    public static class TargetRegistrationExtensions
    {
        public static void RegisterTarget(this Session session, TargetModel resourceModel)
        {
            if (resourceModel is HttpTargetModel)
            {
                var http = resourceModel as HttpTargetModel;
                var uri = new Uri(http.Url);
                var pieces = uri.AbsolutePath.Split('/').ToList();

                var serviceBase = session.Artifacts[uri.Host][uri.Port];
                if (!(serviceBase is HttpServicePortArtifact))
                {
                    var httpArtifact = new HttpServicePortArtifact() { Parent = session.Artifacts[uri.Host] };
                    httpArtifact.Identifiers.Add($"{ProtocolType.Tcp.ToString()}/{uri.Port}");
                    session.Artifacts[uri.Host][uri.Port] = httpArtifact;
                    
                    var httpBase = session.Artifacts[uri.Host][uri.Port];
                    httpBase.Metadata.Set(serviceBase.Metadata, "_known_", DataProviderSpecificity.Explicit);
                }

                var pathTip = UrlHelper.GeneratePathArtifacts(
                    (HttpServicePortArtifact) session.Artifacts[uri.Host][uri.Port], uri.AbsolutePath, http.Method);

                pathTip.Requests.Add(new UrlRequestArtifact
                {
                    Parent = pathTip,
                    Fields = new ObservableDictionary<string, string>(http.Fields),
                    Headers = new ObservableDictionary<string, string>(http.Headers)
                });
            }

            if (resourceModel is SocketPortTargetModel)
            {
                var socket = resourceModel as SocketPortTargetModel;
                session.Artifacts[socket.Hostname][$"{socket.Protocol}/{socket.Port}"] = new ServicePortArtifact();
            }
        }
    }
}