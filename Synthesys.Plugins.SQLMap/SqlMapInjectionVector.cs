namespace Synthesys.Plugins.SQLMap
{
    /// <summary>
    ///     A way to request a page which results in the ability to read/write otherwise protected data, execute code, etc.
    /// </summary>
    public class SqlMapInjectionVector
    {
        /// <summary>
        ///     Parameter that is injectable
        /// </summary>
        public string Parameter { get; set; }

        /// <summary>
        ///     Type of injection
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     Title of injection
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     Injection payload
        /// </summary>
        public string Payload { get; set; }
    }
}