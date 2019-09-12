using Synthesys.SDK.Triggers;

namespace Synthesys.SDK.Extensions
{
    /// A ReactionExtension is added to the end of the Task Queue when some given event occurs during the scan process. The concept behind ReactionExtensions is to enrich the Artifact Tree and, in doing so, trigger other ReactionExtensions.
    ///
    /// ReactionExtensions can be triggered by:
    /// <list type="bullet">
    /// <item><description>ActionExtension completes or fails</description></item>
    /// <item><description>Artifact Tree element has a child added to it</description></item>
    /// <item><description>Artifact Tree element changes</description></item>
    /// <item><description>Artifact Tree element is created</description></item>
    /// <item><description>Task is started by the Task Queue</description></item>
    /// <item><description>Task is completed</description></item>
    /// <item><description>Task is added to the Task Queue</description></item>
    /// <item><description>Task Queue is completely drained</description></item>
    /// </list>
    public abstract class ReactionExtension : Extension
    {
        /// <summary>
        /// This method is called when the ReactionExtension is popped from the Task Queue and executed
        /// </summary>
        /// <param name="trigger">Trigger causing the ReactionExtension to fire</param>
        /// <returns></returns>
        public abstract ExtensionReport React(TriggerDescriptor trigger);
    }
}