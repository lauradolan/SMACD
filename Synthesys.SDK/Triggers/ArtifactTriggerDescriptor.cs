using SMACD.Artifacts;
using System;

namespace Synthesys.SDK.Triggers
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
        ///     Create a descriptor for a trigger activated by an operation on an Artifact
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Triggering operation</param>
        public ArtifactTriggerDescriptor(string artifactPath, ArtifactTrigger trigger)
        {
            ArtifactPath = artifactPath;
            Trigger = trigger;
        }

        /// <summary>
        ///     Create a descriptor for a trigger activated by an operation on an Artifact
        /// </summary>
        /// <param name="artifact">Artifact instance</param>
        /// <param name="trigger">Triggering operation</param>
        public ArtifactTriggerDescriptor(Artifact artifact, ArtifactTrigger trigger)
        {
            Artifact = artifact;
            ArtifactPath = artifact.GetUUIDPathToRoot();;
            Trigger = trigger;
        }

        /// <summary>
        ///     Artifact Instance
        /// </summary>
        public Artifact Artifact { get; set; }

        /// <summary>
        ///     Path to Artifact
        /// </summary>
        public string ArtifactPath { get; set; }

        /// <summary>
        ///     Artifact operation causing trigger
        /// </summary>
        public ArtifactTrigger Trigger { get; set; }

        public override string ToString()
        {
            if (Artifact != null)
                return $"Artifact Trigger ({Artifact.GetUUIDPathToRoot()} {Trigger.ToString()})";
            return $"Artifact Trigger Path {ArtifactPath} {Trigger.ToString()}";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ArtifactTriggerDescriptor castDescriptor = obj as ArtifactTriggerDescriptor;
            if (castDescriptor.Artifact == Artifact && castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (Artifact != null)
                return HashCode.Combine(Artifact.GetUUIDPathToRoot(), Trigger);
            else
                return HashCode.Combine(ArtifactPath, Trigger);
        }
    }
}