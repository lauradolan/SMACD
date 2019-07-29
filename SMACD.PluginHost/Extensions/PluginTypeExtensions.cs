using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SMACD.PluginHost.Extensions
{
    public enum PluginTypes
    {
        [Description("unknown")] Unknown,
        [Description("attack")] AttackTool,
        [Description("score")] Scorer,
        [Description("decide")] Decision
    }

    internal static class PluginTypeExtensions
    {
        private static readonly List<string> VALID_TYPES = Enum.GetNames(typeof(PluginTypes))
            .Select(n =>
                ((DescriptionAttribute) Attribute.GetCustomAttribute(
                    typeof(PluginTypes).GetMember(n).Single(),
                    typeof(DescriptionAttribute))).Description).ToList();

        /// <summary>
        ///     Retrieve the document-stored identifier for this plugin type
        /// </summary>
        /// <param name="type">Plugin type</param>
        /// <returns>PluginType string</returns>
        public static string GetPluginTypeString(this PluginTypes type)
        {
            return Enum.GetNames(typeof(PluginTypes))
                .Where(n => n == Enum.GetName(typeof(PluginTypes), type))
                .Select(n =>
                    ((DescriptionAttribute) Attribute.GetCustomAttribute(
                        typeof(PluginTypes).GetMember(n).Single(),
                        typeof(DescriptionAttribute))).Description).FirstOrDefault();
        }

        internal static PluginTypes GetPluginType(string identifier)
        {
            if (!identifier.Contains('.'))
                throw new Exception("Action name must be of format <type>.<name>");
            var type = identifier.Split('.')[0];
            if (!VALID_TYPES.Contains(type))
                throw new Exception("Action type must be " + string.Join(", ", VALID_TYPES));
            switch (type)
            {
                case "attack":
                    return PluginTypes.AttackTool;
                case "score":
                    return PluginTypes.Scorer;
                case "decision":
                    return PluginTypes.Decision;
                default:
                    return PluginTypes.Unknown;
            }
        }
    }
}