using SMACD.Artifacts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.SDK
{
    public class TriggerDescriptor
    {
        /// <summary>
        /// Create an artifact-based trigger
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Trigger operation</param>
        /// <returns></returns>
        public static ArtifactTriggerDescriptor ArtifactTrigger(string artifactPath, ArtifactTrigger trigger)
        {
            return new ArtifactTriggerDescriptor(artifactPath, trigger);
        }

        /// <summary>
        /// Create an extension-based trigger
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="trigger">Extension execution condition</param>
        /// <returns></returns>
        public static ExtensionTriggerDescriptor ExtensionTrigger(string extensionIdentifier, ExtensionConditionTrigger trigger)
        {
            return new ExtensionTriggerDescriptor(extensionIdentifier, trigger);
        }

        /// <summary>
        /// Create a trigger activated by a SystemEvent
        /// </summary>
        /// <param name="trigger">System event</param>
        /// <returns></returns>
        public static SystemEventTriggerDescriptor SystemEventTrigger(SystemEvents trigger)
        {
            return new SystemEventTriggerDescriptor(trigger);
        }

        protected TriggerDescriptor(){}

        public override bool Equals(object obj)
        {
            if (obj.GetType() != GetType() ||
                !(obj is TriggerDescriptor))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Check if the Artifact's path matches the given path
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
            string nextEl = pathElements.First();
            List<string> nextElements = new List<string>();
            if (pathElements.Count > 1)
            {
                nextElements = new List<string>(pathElements.Skip(1));
            }

            if (nextEl == "*" ||
                nextEl == artifact.Identifier)
            {
                if (nextElements.Count == 0 ||
                    artifact.ChildNames.Any(a => RecurseMatch(artifact.GetChildById(a), nextElements)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generate the path for a given artifact
        /// </summary>
        /// <param name="artifact">Artifact</param>
        /// <returns></returns>
        public string GeneratePath(Artifact artifact)
        {
            List<Artifact> fullPath = artifact.GetPathToRoot();
            IEnumerable<Artifact> partialPath = fullPath.Where(p => !(p is RootArtifact || p is HostArtifact));
            return string.Join("|;|", partialPath.Select(i => i.Identifier));
        }
    }

    public enum ArtifactTrigger
    {
        IsCreated,
        IsUpdated,
        AddsChild
    }
    public class ArtifactTriggerDescriptor : TriggerDescriptor
    {
        /// <summary>
        /// Path to Artifact
        /// </summary>
        public string ArtifactPath { get; set; }

        /// <summary>
        /// Artifact operation causing trigger
        /// </summary>
        public ArtifactTrigger Trigger { get; set; }

        /// <summary>
        /// Create a descriptor for a trigger activated by an operation on an Artifact
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Triggering operation</param>
        public ArtifactTriggerDescriptor(string artifactPath, ArtifactTrigger trigger)
        {
            ArtifactPath = artifactPath;
            Trigger = trigger;
        }

        public override string ToString()
        {
            return $"Artifact Trigger ({ArtifactPath} {Trigger.ToString()})";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ArtifactTriggerDescriptor castDescriptor = obj as ArtifactTriggerDescriptor;
            if (castDescriptor.ArtifactPath == ArtifactPath && castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ArtifactPath, Trigger);
        }
    }

    public enum ExtensionConditionTrigger
    {
        Succeeds,
        Fails
    }
    public class ExtensionTriggerDescriptor : TriggerDescriptor
    {
        public string ExtensionIdentifier { get; set; }
        public ExtensionConditionTrigger Trigger { get; set; }

        /// <summary>
        /// Create a descriptor for a trigger activated by execution of an Extension
        /// </summary>
        /// <param name="extensionIdentifier">Artifact path</param>
        /// <param name="trigger">Triggering operation</param>
        public ExtensionTriggerDescriptor(string extensionIdentifier, ExtensionConditionTrigger trigger)
        {
            ExtensionIdentifier = extensionIdentifier;
            Trigger = trigger;
        }

        public override string ToString()
        {
            return $"Extension Trigger ({ExtensionIdentifier} {Trigger.ToString()})";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ExtensionTriggerDescriptor castDescriptor = obj as ExtensionTriggerDescriptor;
            if (castDescriptor.ExtensionIdentifier == ExtensionIdentifier && castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ExtensionIdentifier, Trigger);
        }
    }

    public enum SystemEvents
    {
        TaskAddedToQueue,
        TaskStarted,
        TaskCompleted,
        TaskQueueDepleted
    }
    public class SystemEventTriggerDescriptor : TriggerDescriptor
    {
        /// <summary>
        /// System event trigger
        /// </summary>
        public SystemEvents SystemEvent { get; set; }

        /// <summary>
        /// Create a descriptor for a trigger activated by a System Event
        /// </summary>
        /// <param name="systemEvent">Triggering system event</param>
        public SystemEventTriggerDescriptor(SystemEvents systemEvent)
        {
            SystemEvent = systemEvent;
        }

        public override string ToString()
        {
            return $"System Event Trigger ({SystemEvent.ToString()})";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            SystemEventTriggerDescriptor castDescriptor = obj as SystemEventTriggerDescriptor;
            if (castDescriptor.SystemEvent == SystemEvent)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SystemEvent);
        }
    }
}
