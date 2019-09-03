using SMACD.Artifacts;
using SMACD.Data.Resources;
using SMACD.ScanEngine;
using SMACD.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.Dynamic;

namespace SMACD.Data.Interop
{
    public static class TargetRegistrationExtensions
    {
        public static void RegisterTarget(this Session session, TargetModel resourceModel)
        {
            if (resourceModel is HttpTargetModel)
            {
                HttpTargetModel http = resourceModel as HttpTargetModel;
                Uri uri = new Uri(http.Url);
                List<string> pieces = uri.AbsolutePath.Split('/').ToList();
                if (session.Artifacts[uri.Host].Children.Any(c => c.Identifier == uri.Port.ToString()))
                {
                    // TODO: Do this better.
                    ServicePortArtifact original = session.Artifacts[uri.Host][uri.Port];
                    session.Artifacts[uri.Host][uri.Port] = new HttpServicePortArtifact()
                    {
                        ServiceName = original.ServiceName,
                        ServiceBanner = original.ServiceBanner
                    };
                }
                else
                {
                    session.Artifacts[uri.Host][uri.Port] = new HttpServicePortArtifact();
                }

                UrlArtifact pathTip = UrlHelper.GeneratePathArtifacts(((HttpServicePortArtifact)session.Artifacts[uri.Host][uri.Port]), uri.AbsolutePath, http.Method);

                pathTip.Requests.Add(new UrlRequestArtifact()
                {
                    Parent = pathTip,
                    Fields = new ObservableDictionary<string, string>(http.Fields),
                    Headers = new ObservableDictionary<string, string>(http.Headers)
                });
            }
            if (resourceModel is SocketPortTargetModel)
            {
                SocketPortTargetModel socket = resourceModel as SocketPortTargetModel;
                session.Artifacts[socket.Hostname][$"{socket.Protocol}/{socket.Port}"].ServiceName = "";
            }
        }
    }
}
