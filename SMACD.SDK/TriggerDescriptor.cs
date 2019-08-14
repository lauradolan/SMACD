using SMACD.Artifacts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.SDK
{
    public class TriggerDescriptor
    {
        public bool Inherit { get; }

        public static ArtifactTriggerDescriptor ArtifactTrigger(string artifactPath, ArtifactTrigger trigger, bool inherit = false)
        {
            return new ArtifactTriggerDescriptor(artifactPath, trigger, inherit);
        }

        public static ExtensionTriggerDescriptor ExtensionTrigger(string extensionIdentifier, ExtensionConditionTrigger trigger, bool inherit = false)
        {
            return new ExtensionTriggerDescriptor(extensionIdentifier, trigger, inherit);
        }

        public static SystemEventTriggerDescriptor SystemEventTrigger(SystemEvents trigger, bool inherit = false)
        {
            return new SystemEventTriggerDescriptor(trigger, inherit);
        }

        protected TriggerDescriptor(bool inherit)
        {
            Inherit = inherit;
        }

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

        public bool PathMatches(Artifact triggeringArtifact, string path)
        {
            return RecurseMatch(triggeringArtifact, path.Split("|;|").ToList());
        }

        private bool RecurseMatch(Artifact artifact, List<string> pathElements)
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

        public string GeneratePath(Artifact a)
        {
            List<Artifact> fullPath = a.GetPathToRoot();
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
        public string ArtifactPath { get; set; }
        public ArtifactTrigger Trigger { get; set; }

        public ArtifactTriggerDescriptor(string artifactPath, ArtifactTrigger trigger, bool inherit = false) : base(inherit)
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

        public ExtensionTriggerDescriptor(string extensionIdentifier, ExtensionConditionTrigger trigger, bool inherit = false) : base(inherit)
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
        public SystemEvents SystemEvent { get; set; }

        public SystemEventTriggerDescriptor(SystemEvents systemEvent, bool inherit = false) : base(inherit)
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
