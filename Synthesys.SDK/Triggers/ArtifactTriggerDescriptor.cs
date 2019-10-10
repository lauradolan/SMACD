using SMACD.AppTree;
using System;

namespace Synthesys.SDK.Triggers
{
    public enum AppTreeNodeEvents
    {
        IsCreated,
        IsUpdated,
        AddsChild
    }

    public class ArtifactTriggerDescriptor : TriggerDescriptor
    {
        /// <summary>
        ///     Create a descriptor for a trigger activated by an operation on a node in the Application Tree
        /// </summary>
        /// <param name="nodePath">Application Tree node path</param>
        /// <param name="trigger">Triggering operation</param>
        public ArtifactTriggerDescriptor(string nodePath, AppTreeNodeEvents trigger)
        {
            NodePath = nodePath;
            Trigger = trigger;
        }

        /// <summary>
        ///     Create a descriptor for a trigger activated by an operation on a node in the Application Tree
        /// </summary>
        /// <param name="node">Application Tree node instance</param>
        /// <param name="trigger">Triggering operation</param>
        public ArtifactTriggerDescriptor(AppTreeNode node, AppTreeNodeEvents trigger)
        {
            Node = node;
            NodePath = node.GetUUIDPath();
            Trigger = trigger;
        }

        /// <summary>
        ///     Artifact Instance
        /// </summary>
        public AppTreeNode Node { get; set; }

        /// <summary>
        ///     Path to Artifact
        /// </summary>
        public string NodePath { get; set; }

        /// <summary>
        ///     Artifact operation causing trigger
        /// </summary>
        public AppTreeNodeEvents Trigger { get; set; }

        public override string ToString()
        {
            if (Node != null)
            {
                return $"Artifact Trigger ({Node.GetUUIDPath()} {Trigger.ToString()})";
            }

            return $"Artifact Trigger Path {NodePath} {Trigger.ToString()}";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ArtifactTriggerDescriptor castDescriptor = obj as ArtifactTriggerDescriptor;
            if (castDescriptor.Node == Node && castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (Node != null)
            {
                return HashCode.Combine(Node.GetUUIDPath(), Trigger);
            }
            else
            {
                return HashCode.Combine(NodePath, Trigger);
            }
        }
    }
}