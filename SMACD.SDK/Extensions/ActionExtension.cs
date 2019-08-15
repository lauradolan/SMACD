namespace SMACD.SDK.Extensions
{
    public abstract class ActionExtension : Extension
    {
        /// <summary>
        /// Called when an Action is either manually queued or queued by another Extension
        /// </summary>
        /// <returns></returns>
        public abstract ExtensionReport Act();
    }
}
