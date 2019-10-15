using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using Synthesys.Tasks;
using Synthesys.Tasks.Attributes;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Synthesys.Plugins.HTMLExplorer
{
    [Extension("htmlexplorer",
        Name = "HTML Explorer",
        Description = "Populates HTML from successful requests",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    [TriggeredBy("**//{UrlRequestNode}*", AppTreeNodeEvents.IsCreated)]
    public class HtmlExplorerReaction : ReactionExtension, ICanQueueTasks
    {
        public ITaskToolbox Tasks { get; set; }

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            Logger.LogDebug("HTML Explorer triggered by {0}", trigger);
            var artifactTrigger = trigger as ArtifactTriggerDescriptor;
            if (artifactTrigger != null)
            {
                var node = artifactTrigger.Node as UrlRequestNode;
                if (node != null)
                {
                    var url = node.GetEntireUrl();
                    Logger.LogInformation("Retrieving HTML from {0}", url);

                    try
                    {
                        string html = string.Empty;
                        using (var wc = new WebClient())
                        {
                            if (node.Method == HttpMethod.Get)
                                html = wc.DownloadString(url);
                            else if (node.Method == HttpMethod.Post)
                            {
                                var fields = string.Join("&", node.Fields.Select(f => $"{f.Key}={f.Value}"));
                                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                                html = wc.UploadString(url, fields);
                            }
                            else
                            {
                                Logger.LogCritical("Triggered HTML population on non-GET/POST verb!");
                            }
                        }

                        if (!string.IsNullOrEmpty(html))
                        {
                            node.Detail.Set(new SMACD.AppTree.Details.UrlRequestDetails()
                            {
                                ResultHtml = html
                            }, "htmlexplorer", DataProviderSpecificity.ExploitSpecificScanner);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical("Could not download HTML from {0}", url);
                    }
                }
            }
            return ExtensionReport.Blank();
        }
    }
}
