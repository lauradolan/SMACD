using System;
using System.Linq;
using SMACD.Artifacts;
using SMACD.Data;
using SMACD.Data.Resources;
using Synthesys.SDK;

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
                if (session.Artifacts[uri.Host].Children.Any(c => c.Identifier == uri.Port.ToString()))
                {
                    // TODO: Do this better.
                    var original = session.Artifacts[uri.Host][uri.Port];
                    session.Artifacts[uri.Host][uri.Port] = new HttpServicePortArtifact
                    {
                        ServiceName = original.ServiceName,
                        ServiceBanner = original.ServiceBanner
                    };
                }
                else
                {
                    session.Artifacts[uri.Host][uri.Port] = new HttpServicePortArtifact();
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
                session.Artifacts[socket.Hostname][$"{socket.Protocol}/{socket.Port}"].ServiceName = "";
            }
        }
    }
}