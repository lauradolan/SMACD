using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace Synthesys.Tasks
{
    /// <summary>
    ///     Provides a more aggressive (computationally complex) avenue of Type resolution.
    ///     This is necessary to facilitate late-loaded libraries (Extensions) and their Types to be serialized cleanly.
    /// </summary>
    public class AggressiveTypeResolutionBinder : ISerializationBinder
    {
        /// <summary>
        ///     Get the AssemblyName and TypeName of a given Type
        /// </summary>
        /// <param name="serializedType">Type to investigate</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <param name="typeName">Type name</param>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }

        /// <summary>
        ///     Get the runtime Type described by a given Assembly and Type
        /// </summary>
        /// <param name="assemblyName">Assembly name</param>
        /// <param name="typeName">Type name</param>
        /// <returns>Resolved runtime Type</returns>
        public Type BindToType(string assemblyName, string typeName)
        {
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type attempt = assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName)?.GetType(typeName);
            if (attempt == null)
            {
                attempt = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName))
                    .FirstOrDefault(t => t != null);
            }

            return attempt;
        }
    }
}