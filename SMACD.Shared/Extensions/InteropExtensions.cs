using System;
using System.Threading.Tasks;
using SMACD.Shared.WorkspaceManagers;
using YamlDotNet.Serialization;

namespace SMACD.Shared.Extensions
{
    public static class InteropExtensions
    {
        [ThreadStatic] public static Task CurrentTask;

        internal static T AddLoadedTagMappings<T>(this T builder) where T : BuilderSkeleton<T>
        {
            ResourceManager.GetKnownResourceHandlers()
                .ForEach(h => builder = builder.WithTagMapping("!" + h.Item1, h.Item2));
            return builder;
        }
    }
}