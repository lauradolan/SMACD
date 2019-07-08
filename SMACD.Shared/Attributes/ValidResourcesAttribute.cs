using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    /// Specify valid Resource Types that can be used by this plugin
    /// </summary>
    public class ValidResourcesAttribute : Attribute
    {
        /// <summary>
        /// Types of Resources that can be used by this plugin
        /// </summary>
        public List<Type> Types { get; private set; }

        /// <summary>
        /// Specify valid Resource Types that can be used by this plugin
        /// </summary>
        /// <param name="types">Types of Resources that can be used by this plugin</param>
        public ValidResourcesAttribute(params Type[] types) => Types = types.ToList();
    }
}