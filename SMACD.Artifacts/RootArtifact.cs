﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.Artifacts
{
    public class RootArtifact : Artifact
    {
        /// <summary>
        /// Artifact Identifier
        /// </summary>
        public override string Identifier => "_root_";

        /// <summary>
        /// Fired when an Artifact belonging to this tree
        /// </summary>
        public event ArtifactEventDelegate ArtifactCreated;

        /// <summary>
        /// Fired when the data of an Artifact changes
        /// </summary>
        public event ArtifactEventDelegate ArtifactChanged;

        /// <summary>
        /// Fired when an Artifact is added to a given Artifact
        /// </summary>
        public event ArtifactEventDelegate ArtifactChildAdded;

        public delegate void ArtifactEventDelegate(Artifact newOrModifiedArtifact, List<Artifact> path);

        /// <summary>
        /// Hostname or IP of resource
        /// </summary>
        /// <param name="hostNameOrIp">Hostname/IP</param>
        /// <returns></returns>
        public HostArtifact this[string hostNameOrIp]
        {
            get
            {
                // Try to short-circuit doing a lot more work by checking the n+1 case
                HostArtifact existingResult = (HostArtifact)Children.FirstOrDefault(h =>
                    ((HostArtifact)h).Aliases.Contains(hostNameOrIp));
                if (existingResult != null)
                {
                    return existingResult;
                }

                List<string> aliases = new List<string>();
                string ip = string.Empty;
                string hostName = string.Empty;

                // Dns.GetHostEntry uses OS DNS timeout, which can be *really* long (i.e. seconds)
                IPHostEntry entry = null;
                try
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    Task<IPHostEntry> task = Task.Run(() => entry = Dns.GetHostEntry(hostNameOrIp), cancellationTokenSource.Token);
                    Task.Delay(1000).ContinueWith((a) =>
                        cancellationTokenSource.Cancel())
                        .Wait();
                }
                catch (Exception) { }

                if (entry != null)
                {
                    aliases.AddRange(entry.Aliases);
                    ip = entry.AddressList.FirstOrDefault().ToString();
                    hostName = entry.HostName;
                    if (!aliases.Contains(ip))
                    {
                        aliases.Add(ip);
                    }

                    if (!aliases.Contains(hostName))
                    {
                        aliases.Add(hostName);
                    }

                    if (!aliases.Contains(hostNameOrIp))
                    {
                        aliases.Add(hostNameOrIp);
                    }
                }

                aliases.RemoveAll(string.IsNullOrEmpty);

                if (aliases.Count == 0)
                {
                    aliases.Add(hostNameOrIp);
                }

                HostArtifact result = (HostArtifact)Children.FirstOrDefault(h =>
                    ((HostArtifact)h).Aliases.Any(a => aliases.Contains(a)));
                if (result == null)
                {
                    result = new HostArtifact()
                    {
                        Parent = this,
                        IpAddress = ip,
                        Hostname = hostName
                    };
                    foreach (string item in aliases)
                    {
                        if (!result.Aliases.Contains(item))
                        {
                            result.Aliases.Add(item);
                        }
                    }
                    result.BeginFiringEvents();
                    Children.Add(result);
                }
                return result;
            }
        }

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
