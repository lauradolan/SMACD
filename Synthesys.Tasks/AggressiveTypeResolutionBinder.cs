using System;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Synthesys.Tasks
{
    /// <summary>
    ///     Provides a more aggressive (computationally complex) avenue of Type resolution.
    ///     This is necessary to facilitate late-loaded libraries (Extensions) and their Types to be serialized cleanly.
    /// </summary>
    public class AggressiveTypeResolutionBinder : ISerializationBinder
    {
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var attempt = assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName)?.GetType(typeName);
            if (attempt == null)
                attempt = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName))
                    .FirstOrDefault(t => t != null);
            return attempt;
        }
    }
}