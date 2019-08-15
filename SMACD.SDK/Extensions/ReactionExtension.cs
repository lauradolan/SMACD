namespace SMACD.SDK.Extensions
{
    public abstract class ReactionExtension : Extension
    {
        /// <summary>
        /// Called when the conditions given by a Trigger are met
        /// </summary>
        /// <param name="trigger">Trigger that met a condition</param>
        /// <returns></returns>
        public abstract ExtensionReport React(TriggerDescriptor trigger);
    }
}
