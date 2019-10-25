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
    [TriggeredBy("**//{UrlRequestNode}*", AppTreeNodeEvents.IsCreated)]
    public class HtmlExplorerReaction : ReactionExtension, ICanQueueTasks
    {
        /// <summary>
        ///     Default execution timeout of the HTTP request
        /// </summary>
        public const int TIMEOUT_MS = 10 * 1000; // 10s

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
                var node = artifactTrigger.Node as UrlRequestNode;
                if (node != null && node.Method != null)
                {
                    lock (node)
                    {
                        if (!node.Detail.UnderlyingCollection.Any())
                        {
                            using (var http = new HttpClient())
                            {
                                HttpResponseMessage result = null;
                                try
                                {
                                    var cts = new System.Threading.CancellationTokenSource(TIMEOUT_MS);
                                    var url = node.GetEntireUrl();
                                    if (node.Method == HttpMethod.Get)
                                    {
                                        result = http.GetAsync(url, cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Post)
                                    {
                                        var content = new FormUrlEncodedContent(node.Fields);
                                        result = http.PostAsync(url, content, cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Put)
                                    {
                                        var content = new FormUrlEncodedContent(node.Fields);
                                        result = http.PutAsync(url, content, cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Delete)
                                    {
                                        result = http.DeleteAsync(url, cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Head)
                                    {
                                        result = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Options)
                                    {
                                        result = http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url), cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Patch)
                                    {
                                        var content = new FormUrlEncodedContent(node.Fields);
                                        result = http.PatchAsync(url, content, cts.Token).Result;
                                    }
                                    else if (node.Method == HttpMethod.Trace)
                                    {
                                        result = http.SendAsync(new HttpRequestMessage(HttpMethod.Trace, url), cts.Token).Result;
                                    }

                                    if (result != null)
                                    {
                                        var details = new UrlRequestDetails()
                                        {
                                            ResultCode = (int)result.StatusCode,
                                            Headers = result.Headers.ToDictionary(k => k.Key, v => v.Value.FirstOrDefault()),
                                            ResultHtml = result.Content.ReadAsStringAsync().Result
                                        };

                                        node.Detail.Set(details, "htmlexplorer", DataProviderSpecificity.ExploitSpecificScanner);
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
