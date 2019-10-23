using SMACD.AppTree;
using System;
using System.Collections.Generic;

namespace Synthesys.SDK.Triggers
{
    /// <summary>
    ///     Events which can be fired by nodes in the AppTree
    /// </summary>
    public enum AppTreeNodeEvents
    {
        /// <summary>
        ///     Fired when a node is created
        /// </summary>
        IsCreated,

        /// <summary>
        ///     Fired when the data in a node is changed
        /// </summary>
        IsUpdated,

        /// <summary>
        ///     Fired when a child node is added to another node
        /// </summary>
        AddsChild
    }

    /// <summary>
    ///     Descriptor for a trigger activated by an operation on a node in the Application Tree
    /// </summary>
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

        /// <summary>
        ///     String representation of Artifact Trigger
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Node != null)
            {
                return $"Artifact Trigger ({Node.GetDisplayPath()} {Trigger.ToString()})";
            }

            return $"Artifact Trigger Path {NodePath} {Trigger.ToString()}";
        }

        /// <summary>
        ///     If an Artifact Trigger matches another
        /// </summary>
        /// <param name="obj">Artifact trigger to compare</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ArtifactTriggerDescriptor castDescriptor = obj as ArtifactTriggerDescriptor;
            if (castDescriptor.NodePath == NodePath && castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get hash code from path and trigger type
        /// </summary>
        /// <returns></returns>
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