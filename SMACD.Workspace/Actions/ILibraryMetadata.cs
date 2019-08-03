using System;

namespace SMACD.Workspace.Actions
{
    public interface ILibraryMetadata
    {
        /// <summary>
        /// Name of Library that contains Actions
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Version of Library
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Author of Library
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Website with more information about Author or Library
        /// </summary>
        string Website { get; }

        /// <summary>
        /// Description of what the Library provides
        /// </summary>
        string Description { get; }
    }
}
