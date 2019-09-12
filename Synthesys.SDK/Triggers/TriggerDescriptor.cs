using System.Collections.Generic;
using System.Linq;
using SMACD.Artifacts;

namespace Synthesys.SDK.Triggers
{
    public class TriggerDescriptor
    {
        protected TriggerDescriptor()
        {
        }

        /// <summary>
        ///     Create an artifact-based trigger
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Trigger operation</param>
        /// <returns></returns>
        public static ArtifactTriggerDescriptor ArtifactTrigger(string artifactPath, ArtifactTrigger trigger)
        {
            return new ArtifactTriggerDescriptor(artifactPath, trigger);
        }

        /// <summary>
        ///     Create an extension-based trigger
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="trigger">Extension execution condition</param>
        /// <returns></returns>
        public static ExtensionTriggerDescriptor ExtensionTrigger(string extensionIdentifier,
            ExtensionConditionTrigger trigger)
        {
            return new ExtensionTriggerDescriptor(extensionIdentifier, trigger);
        }

        /// <summary>
        ///     Create a trigger activated by a SystemEvent
        /// </summary>
        /// <param name="trigger">System event</param>
        /// <returns></returns>
        public static SystemEventTriggerDescriptor SystemEventTrigger(SystemEvents trigger)
        {
            return new SystemEventTriggerDescriptor(trigger);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != GetType() ||
                !(obj is TriggerDescriptor))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Check if the Artifact's path matches the given path
        /// </summary>
        /// <param name="triggeringArtifact">Artifact</param>
        /// <param name="path">Path to check against</param>
        /// <returns></returns>
        public static bool PathMatches(Artifact triggeringArtifact, string path)
        {
            return RecurseMatch(triggeringArtifact, path.Split("|;|").ToList());
        }

        private static bool RecurseMatch(Artifact artifact, List<string> pathElements)
        {
            var nextEl = pathElements.First();
            var nextElements = new List<string>();
            if (pathElements.Count > 1) nextElements = new List<string>(pathElements.Skip(1));

            if (nextEl == "*" ||
                nextEl == artifact.Identifier)
                if (nextElements.Count == 0 ||
                    artifact.Children.Any(child => RecurseMatch(child, nextElements)))
                    return true;

            return false;
        }

        /// <summary>
        ///     Generate the path for a given artifact
        /// </summary>
        /// <param name="artifact">Artifact</param>
        /// <returns></returns>
        public string GeneratePath(Artifact artifact)
        {
            var fullPath = artifact.GetPathToRoot();
            var partialPath = fullPath.Where(p => !(p is RootArtifact || p is HostArtifact));
            return string.Join("|;|", partialPath.Select(i => i.Identifier));
        }
    }
}