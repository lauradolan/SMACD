using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SMACD.ScannerEngine.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurableAttribute : Attribute
    {
        public ConfigurableAttribute([CallerMemberName] string name = null)
        {
            Name = name;
        }

        public string Name { get; set; }

        public static IEnumerable<ConfigurableAttribute> Get<T>()
        {
            return Get(typeof(T));
        }

        public static IEnumerable<ConfigurableAttribute> Get(Type t)
        {
            return t.GetProperties().Where(p =>
            {
                var attr = p.GetCustomAttribute<ConfigurableAttribute>();
                return attr != null;
            }).Select(prop => prop.GetCustomAttribute<ConfigurableAttribute>());
        }

        public static IEnumerable<PropertyInfo> GetProperties<T>()
        {
            return GetProperties(typeof(T));
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type t)
        {
            return t.GetProperties().Where(p =>
            {
                var attr = p.GetCustomAttribute<ConfigurableAttribute>();
                return attr != null;
            });
        }
    }
}