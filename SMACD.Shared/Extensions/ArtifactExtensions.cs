﻿using System.IO;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins.AttackTools;
using SMACD.Shared.Plugins.Scorers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.Shared.Extensions
{
    public static class ArtifactExtensions
    {
        /// <summary>
        ///     Load a serialized object
        /// </summary>
        /// <param name="fileName">File name (no path)</param>
        public static void SaveResultArtifact<T>(this PluginPointerModel pointer, string fileName, T obj)
        {
            SaveResultArtifact(Path.Combine(AttackToolManager.Instance.GetChildWorkingDirectory(pointer), fileName),
                obj);
        }

        /// <summary>
        ///     Load a serialized object
        /// </summary>
        /// <param name="scorer">Scorer</param>
        /// <param name="fileName">File name (no path)</param>
        public static void SaveResultArtifact<T>(this Scorer scorer, string fileName, T obj)
        {
            SaveResultArtifact(Path.Combine(scorer.WorkingDirectory, fileName), obj);
        }

        /// <summary>
        ///     Save a serialized object with its absolute path
        /// </summary>
        /// <param name="fileName">Absolute path to file</param>
        /// <param name="obj">Object to serialize</param>
        public static void SaveResultArtifact<T>(string fileName, T obj)
        {
            using (var sw = new StreamWriter(fileName))
            {
                sw.WriteLine(new SerializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build()
                    .Serialize(obj));
            }
        }

        /// <summary>
        ///     Load a serialized object
        /// </summary>
        /// <param name="fileName">File name (no path)</param>
        public static T LoadResultArtifact<T>(this PluginPointerModel pointer, string fileName)
        {
            return LoadResultArtifact<T>(Path.Combine(AttackToolManager.Instance.GetChildWorkingDirectory(pointer),
                fileName));
        }

        /// <summary>
        ///     Load a serialized object
        /// </summary>
        /// <param name="scorer">Scorer</param>
        /// <param name="fileName">File name (no path)</param>
        public static T LoadResultArtifact<T>(this Scorer scorer, string fileName)
        {
            return LoadResultArtifact<T>(Path.Combine(scorer.WorkingDirectory, fileName));
        }

        /// <summary>
        ///     Load a serialized object from its absolute path
        /// </summary>
        /// <param name="fileName">Absolute path to file</param>
        public static T LoadResultArtifact<T>(string fileName)
        {
            if (!File.Exists(fileName))
                return default;
            using (var sr = new StreamReader(fileName))
            {
                return new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build()
                    .Deserialize<T>(sr);
            }
        }
    }
}