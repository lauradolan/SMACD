using Microsoft.AspNetCore.Components.Rendering;
using Synthesys.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compass
{
    /// <summary>
    ///     Contains helper methods to work with dynamically generated Razor components
    /// </summary>
    public static class DynamicComponentGenerator
    {
        private static Dictionary<string, Type> _cachedViewTypes = new Dictionary<string, Type>();

        /// <summary>
        ///     Inject a dynamically generated Razor component from its name to a specified RenderTreeBuilder.
        ///     Attributes for component can be specified.
        /// </summary>
        /// <param name="builder">RenderTreeBuilder to inject the component into</param>
        /// <param name="name">Name of component Type</param>
        /// <param name="attributes">Attributes to set in component instance</param>
        public static void Inject(RenderTreeBuilder builder, string name, params KeyValuePair<string, object>[] attributes)
        {
            var resolved = SearchOrResolveComponentType(name);
            if (resolved == null)
                return;
            builder.OpenComponent(1, SearchOrResolveComponentType(name));
            foreach (var attrib in attributes)
                builder.AddAttribute(2, attrib.Key, attrib.Value);
            builder.CloseComponent();
        }
        private static Type SearchOrResolveComponentType(string name, Type defaultType = null)
        {
            if (!_cachedViewTypes.ContainsKey(name))
                _cachedViewTypes.Add(name, ResolveComponentType(name, defaultType));
            return _cachedViewTypes[name];
        }
        private static Type ResolveComponentType(string name, Type defaultType = null)
        {
            Type componentType = null;
            if (!string.IsNullOrEmpty(name))
            {
                componentType = Type.GetType(name);
                if (componentType == null && name.Contains(".Components."))
                    componentType = typeof(Compass.Components._Imports).Assembly.GetType(name);

                if (componentType == null)
                {
                    componentType = ExtensionToolbox.Instance.ExtensionLibraries
                        .Select(l => l.Assembly.GetType(name))
                        .FirstOrDefault(a => a != null);
                }
            }
            if (componentType == null && defaultType != null)
                return defaultType;
            return componentType;
        }
    }
}
