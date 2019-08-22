using System;

namespace SMACD.SDK.Triggers
{
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
}
