using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using SMACD.AppTree.Details;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using Synthesys.Tasks;
using Synthesys.Tasks.Attributes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Synthesys.Plugins.HTMLExplorer
{
    [Extension("htmlexplorer",
        Name = "HTML Explorer",
        Description = "Populates HTML from successful requests",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    [TriggeredBy("**//{UrlNode}*", AppTreeNodeEvents.AddsChild)]
    public class HtmlExplorerReaction : ReactionExtension, ICanQueueTasks
    {
        public ITaskToolbox Tasks { get; set; }

        public override void Initialize()
        {
            // Register a broader set of encoding providers to handle more character sets
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            Logger.LogDebug("HTML Explorer triggered by {0}", trigger);
            var artifactTrigger = trigger as ArtifactTriggerDescriptor;
            if (artifactTrigger != null)
            {
                var node = artifactTrigger.Node as UrlNode;
                if (node != null)
                {
                    lock (node)
                    {
                        foreach (var request in node.Requests.Where(r => !r.Detail.UnderlyingCollection.Any()))
                        {
                            using (var http = new HttpClient())
                            {
                                HttpResponseMessage result = null;
                                try
                                {
                                    var url = request.GetEntireUrl();
                                    if (request.Method == HttpMethod.Get)
                                    {
                                        result = http.GetAsync(url).Result;
                                    }
                                    else if (request.Method == HttpMethod.Post)
                                    {
                                        var content = new FormUrlEncodedContent(request.Fields);
                                        result = http.PostAsync(url, content).Result;
                                    }

                                    if (result != null)
                                    {
                                        var details = new UrlRequestDetails()
                                        {
                                            ResultCode = (int)result.StatusCode,
                                            Headers = result.Headers.ToDictionary(k => k.Key, v => v.Value.FirstOrDefault()),
                                            ResultHtml = result.Content.ReadAsStringAsync().Result
                                        };

                                        request.Detail.Set(details, "htmlexplorer", DataProviderSpecificity.ExploitSpecificScanner);
                                    }
                                }
                                catch (WebException ex)
                                {
                                    Logger.LogWarning(ex, "HTML Explorer encountered an issue grabbing content");
                                }
                            }
                        }
                    }
                }
            }
            return ExtensionReport.Blank();
        }
    }
}
