namespace SMACD.SDK.Extensions
{
    public abstract class ReactionExtension : Extension
    {
        public abstract ExtensionReport React(TriggerDescriptor trigger);
    }
}
