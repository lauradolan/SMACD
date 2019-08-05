using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SMACD.Workspace.Libraries
{
    /// <summary>
    /// Toolbox of functions to control and manage Actions
    /// </summary>
    public class LibraryToolbox : WorkspaceToolbox
    {
        private List<ExtensionLibrary> Providers { get; set; } = new List<ExtensionLibrary>();
        private List<ActionDescriptor> Actions => Providers.SelectMany(p => p.ActionsProvided).ToList();
        private List<ServiceDescriptor> Services => Providers.SelectMany(p => p.Services).ToList();

        /// <summary>
        /// List of Provider Libraries loaded
        /// </summary>
        public IReadOnlyList<ExtensionLibrary> LoadedActionProviders => Providers;

        /// <summary>
        /// List of Actions available from loaded Provider Libraries
        /// </summary>
        public IReadOnlyList<ActionDescriptor> LoadedActionDescriptors => Actions;

        /// <summary>
        /// List of Services available from loaded Provider Libraries
        /// </summary>
        public IReadOnlyList<ServiceDescriptor> LoadedServiceDescriptors => Services;

        internal LibraryToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Register Actions from a directory of ExtensionLibrary libraries
        /// </summary>
        /// <param name="directory">Directory to search</param>
        /// <param name="fileMask">Mask of files to search for</param>
        public void RegisterFromDirectory(string directory, string fileMask = "*.dll") => Directory.GetFiles(directory, fileMask).ToList()
            .ForEach(f => RegisterFrom(f));

        /// <summary>
        /// Register Actions from a given ExtensionLibrary library
        /// </summary>
        /// <param name="providerLibraryFile">Provider library</param>
        public void RegisterFrom(string providerLibraryFile)
        {
            string resolvedName = new FileInfo(providerLibraryFile).FullName;
            if (Providers.Any(p => p.FileName == resolvedName))
            {
                Logger.LogWarning("Provider library already loaded!");
                return;
            }
            Providers.Add(new ExtensionLibrary(resolvedName));
        }
    }
}
