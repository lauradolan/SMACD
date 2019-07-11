using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.Shared.Extensions
{
    public static class ConfigAttributeExtensions
    {
        public static TProperty GetConfigAttribute<TAttribute, TProperty>(this object obj,
            Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            return GetConfigAttributes(obj.GetType(), propertySelectionAction).FirstOrDefault();
        }

        public static TProperty GetConfigAttribute<TParent, TAttribute, TProperty>(
            Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            return GetConfigAttributes(typeof(TParent), propertySelectionAction)
                .FirstOrDefault();
        }

        public static IEnumerable<TProperty> GetConfigAttributes<TAttribute, TProperty>(this object obj,
            Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            return GetConfigAttributes(obj.GetType(), propertySelectionAction);
        }

        public static IEnumerable<TProperty> GetConfigAttributes<TParent, TAttribute, TProperty>(
            Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            return GetConfigAttributes(typeof(TParent), propertySelectionAction);
        }

        private static IEnumerable<TProperty> GetConfigAttributes<TAttribute, TProperty>(Type type,
            Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            if (!type.CustomAttributes.Any(a => typeof(TAttribute).IsAssignableFrom(a.AttributeType)))
                return new List<TProperty>();
            return type.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>()
                .Select(a => propertySelectionAction(a));
        }

        public static TProperty GetConfigAttribute<TAttribute, TProperty>(this Type type,
            Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            if (!type.CustomAttributes.Any(a => typeof(TAttribute).IsAssignableFrom(a.AttributeType)))
                return default;
            return type.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>()
                .Select(a => propertySelectionAction(a)).FirstOrDefault();
        }
    }
}