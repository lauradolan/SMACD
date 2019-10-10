using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents the root-level node for all data
    /// </summary>
    public class RootNode : AppTreeNode
    {
        /// <summary>
        ///     Delegate used to describe an affected Artifact when an Event occurs
        /// </summary>
        /// <param name="affectedNode">Affected node</param>
        public delegate void ArtifactEventDelegate(AppTreeNode affectedNode);

        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public override string NodeViewName => "SMACD.Artifacts.Views.RootNodeView";

        /// <summary>
        ///     Whether or not to suppress Artifact tree related events (useful during data loads or when responsiveness is not desired)
        /// </summary>
        public bool SuppressEventFiring { get; set; } = false;

        /// <summary>
        ///     Hostname or IP of resource
        /// </summary>
        /// <param name="hostNameOrIp">Hostname/IP</param>
        /// <returns></returns>
        public HostNode this[string hostNameOrIp]
        {
            get
            {
                HostNode existingResult = ChildrenAre<HostNode>(n => n.Identifiers.Contains(hostNameOrIp)).FirstOrDefault();
                if (existingResult != null)
                {
                    return existingResult;
                }

                // Hard mode! Resolve first and check against aliases
                List<string> resolvedAliases = new List<string>();
                string ip = string.Empty;
                string hostName = string.Empty;

                // Dns.GetHostEntry uses OS DNS timeout, which can be *really* long (i.e. seconds)
                IPHostEntry entry = null;
                try
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    Task<IPHostEntry> task = Task.Run(() => entry = Dns.GetHostEntry(hostNameOrIp), cancellationTokenSource.Token);
                    Task.Delay(1000).ContinueWith(a => cancellationTokenSource.Cancel()).Wait();
                }
                catch (Exception)
                {
                }

                if (entry != null)
                {
                    resolvedAliases.AddRange(entry.Aliases.Where(a => !string.IsNullOrEmpty(a)));

                    ip = entry.AddressList.FirstOrDefault()?.ToString();
                    hostName = entry.HostName;
                    if (!resolvedAliases.Contains(ip) && !string.IsNullOrEmpty(ip))
                    {
                        resolvedAliases.Add(ip);
                    }

                    if (!resolvedAliases.Contains(hostName) && !string.IsNullOrEmpty(hostName))
                    {
                        resolvedAliases.Add(hostName);
                    }
                }

                if (!resolvedAliases.Contains(hostNameOrIp) && !string.IsNullOrEmpty(hostNameOrIp))
                {
                    resolvedAliases.Add(hostNameOrIp);
                }

                HostNode result = ChildrenAre<HostNode>(n => n.Identifiers.Any(i => resolvedAliases.Contains(i))).FirstOrDefault();
                if (result == null)
                {
                    result = new HostNode
                    {
                        Parent = this,
                        IpAddress = ip,
                        Hostname = hostName
                    };
                    foreach (string item in resolvedAliases.Distinct())
                    {
                        if (!result.Identifiers.Contains(item) && !string.IsNullOrEmpty(item))
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                result.Identifiers.Add(item);
                            }
                        }
                    }
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

        /// <summary>
        ///     Represents the root of an Artifact correlation tree
        /// </summary>
        public RootNode()
        {
            Identifiers.Add("_root_");
        }

        /// <summary>
        ///     Invoke the ArtifactCreated event
        /// </summary>
        /// <param name="newTreeNode">Created Artifact</param>
        internal void InvokeTreeNodeCreated(AppTreeNode newTreeNode)
        {
            if (!SuppressEventFiring)
            {
                ArtifactCreated?.Invoke(newTreeNode);
            }
        }

        /// <summary>
        ///     Invoke the ArtifactChanged event
        /// </summary>
        /// <param name="changedTreeNode">Artifact changed</param>
        internal void InvokeTreeNodeChanged(AppTreeNode changedTreeNode)
        {
            if (!SuppressEventFiring)
            {
                ArtifactChanged?.Invoke(changedTreeNode);
            }
        }

        /// <summary>
        ///     Invoke the ArtifactChildAdded event
        /// </summary>
        /// <param name="nodeAddingChild">Newly added child Artifact</param>
        internal void InvokeTreeChildAdded(AppTreeNode nodeAddingChild)
        {
            if (!SuppressEventFiring)
            {
                ArtifactChildAdded?.Invoke(nodeAddingChild);
            }
        }
    }
}