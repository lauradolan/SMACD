using System;
using System.Collections.Generic;
using System.Reflection;

namespace SMACD.ScannerEngine.Attributes
{
    /// <summary>
    /// Specifies what types of Resources can be used by this Plugini
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AllowResourceTypeAttribute : Attribute
    {
        public AllowResourceTypeAttribute(Type resourceType)
        {
            ResourceType = resourceType;
        }

        public Type ResourceType { get; set; }

        public static IEnumerable<AllowResourceTypeAttribute> Get<T>()
        {
            return Get(typeof(T));
        }

        public static IEnumerable<AllowResourceTypeAttribute> Get(Type t)
        {
            return t.GetCustomAttributes<AllowResourceTypeAttribute>();
        }
    }
}