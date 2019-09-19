using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.Artifacts
{
    public class RootArtifact : Artifact
    {
        public delegate void ArtifactEventDelegate(Artifact newOrModifiedArtifact, List<Artifact> path);

        /// <summary>
        ///     Artifact Identifier
        /// </summary>
        public override string Identifier => "_root_";

        public override string ArtifactTextSummary => "Root Artifact. This element does not carry data or vulnerabilities.";
        public override string ArtifactSummaryViewTypeName => "RootArtifactView.razor_";

        /// <summary>
        ///     Hostname or IP of resource
        /// </summary>
        /// <param name="hostNameOrIp">Hostname/IP</param>
        /// <returns></returns>
        public HostArtifact this[string hostNameOrIp]
        {
            get
            {
                // Try to short-circuit doing a lot more work by checking the n+1 case
                var existingResult = (HostArtifact) Children.FirstOrDefault(h =>
                    ((HostArtifact) h).Aliases.Contains(hostNameOrIp));
                if (existingResult != null) return existingResult;

                // Hard mode! Resolve first and check against aliases
                var aliases = new List<string>();
                var ip = string.Empty;
                var hostName = string.Empty;

                // Dns.GetHostEntry uses OS DNS timeout, which can be *really* long (i.e. seconds)
                IPHostEntry entry = null;
                try
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var task = Task.Run(() => entry = Dns.GetHostEntry(hostNameOrIp), cancellationTokenSource.Token);
                    Task.Delay(1000).ContinueWith(a =>
                            cancellationTokenSource.Cancel())
                        .Wait();
                }
                catch (Exception)
                {
                }

                if (entry != null)
                {
                    aliases.AddRange(entry.Aliases.Where(a => !string.IsNullOrEmpty(a)));
                    ip = entry.AddressList.FirstOrDefault()?.ToString();
                    hostName = entry.HostName;
                    if (!aliases.Contains(ip) && !string.IsNullOrEmpty(ip)) aliases.Add(ip);

                    if (!aliases.Contains(hostName) && !string.IsNullOrEmpty(hostName)) aliases.Add(hostName);

                    if (!aliases.Contains(hostNameOrIp) && !string.IsNullOrEmpty(hostNameOrIp))
                        aliases.Add(hostNameOrIp);
                }

                if (aliases.Count == 0) aliases.Add(hostNameOrIp);

                var result = (HostArtifact) Children.FirstOrDefault(h =>
                    ((HostArtifact) h).Aliases.Any(a => aliases.Contains(a)));
                if (result == null)
                {
                    result = new HostArtifact
                    {
                        Parent = this,
                        IpAddress = ip,
                        Hostname = hostName
                    };
                    foreach (var item in aliases)
                        if (!result.Aliases.Contains(item) && !string.IsNullOrEmpty(item))
                            result.Aliases.Add(item);

                    if (string.IsNullOrEmpty(result.Hostname))
                        result.Hostname = aliases.FirstOrDefault(a =>
                            Regex.IsMatch(a,
                                "^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\\-]*[A-Za-z0-9])$"));
                    if (string.IsNullOrEmpty(result.IpAddress))
                        result.IpAddress =
                            aliases.FirstOrDefault(a => Regex.IsMatch(a, "\b(?:\\d{1,3}\\.){3}\\d{1,3}\b"));

                    result.BeginFiringEvents();
                    Children.Add(result);
                }

                return result;
            }
        }

        /// <summary>
        ///     Fired when an Artifact belonging to this tree
        /// </summary>
        public event ArtifactEventDelegate ArtifactCreated;

        /// <summary>
        ///     Fired when the data of an Artifact changes
        /// </summary>
        public event ArtifactEventDelegate ArtifactChanged;

        /// <summary>
        ///     Fired when an Artifact is added to a given Artifact
        /// </summary>
        public event ArtifactEventDelegate ArtifactChildAdded;

        internal void InvokeArtifactCreated(Artifact newArtifact, List<Artifact> path)
        {
            ArtifactCreated?.Invoke(newArtifact, path);
        }

        internal void InvokeArtifactChanged(Artifact changedArtifact, List<Artifact> path)
        {
            ArtifactChanged?.Invoke(changedArtifact, path);
        }

        internal void InvokeArtifactChildAdded(Artifact newChild, List<Artifact> path)
        {
            ArtifactChildAdded?.Invoke(newChild, path);
        }
    }
}