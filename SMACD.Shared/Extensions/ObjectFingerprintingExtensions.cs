using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace SMACD.Shared.Extensions
{
    public static class ObjectFingerprintingExtensions
    {
        private const int HASH_LENGTH = 8;

        /// <summary>
        ///     Gets a fingerprint hash of a given object
        /// </summary>
        /// <param name="obj">Object to hash</param>
        /// <param name="hashLength">Length of string to emit (from beginning)</param>
        /// <param name="skippedFields">Fields to skip in fingerprinting</param>
        /// <param name="serializeEphemeralData">
        ///     If <c>TRUE</c> the fingerprinting process will include the ResourceId and
        ///     SystemGenerated properties (not recommended)
        /// </param>
        /// <returns></returns>
        public static string Fingerprint<TObject>(this TObject obj, int hashLength = HASH_LENGTH,
            bool serializeEphemeralData = false, params string[] skippedFields)
        {
            var allSkippedFields = new List<string>();
            if (!serializeEphemeralData)
                allSkippedFields.AddRange(new[] {"resourceId", "systemCreated"});

            allSkippedFields.AddRange(skippedFields);
            allSkippedFields = allSkippedFields.Distinct().ToList();

            using (var sha1 = new SHA1Managed())
            {
                var str = new SerializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .WithTypeInspector(i => new SkipFieldsInspector(i, allSkippedFields.ToArray()))
                    .Build()
                    .Serialize(obj).Replace(Environment.NewLine, "\n");

                return sha1.ComputeHash(Encoding.ASCII.GetBytes(str))
                    .Aggregate(new StringBuilder(), (current, next) => current.Append(next.ToString("X2")))
                    .ToString()
                    .Substring(0, hashLength);
            }
        }

        /// <summary>
        /// Calculate SHA1 hash of a string (not cryptographically safe operation!)
        /// </summary>
        /// <param name="str">String to hash</param>
        /// <returns></returns>
        public static string SHA1(this string str)
        {
            try
            {
                byte[] hash = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(str));
                StringBuilder ret = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                    ret.Append(hash[i].ToString("x2"));

                return ret.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class SkipFieldsInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector _innerTypeDescriptor;

        // Skip Resource system fields that would break a Fingerprint because they include ephemeral data
        private readonly string[] _skippedFields;

        public SkipFieldsInspector(ITypeInspector innerTypeDescriptor, params string[] skippedFields)
        {
            _innerTypeDescriptor = innerTypeDescriptor;
            _skippedFields = skippedFields;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            return _innerTypeDescriptor.GetProperties(type, container)
                .Where(p => !_skippedFields.Contains(p.Name));
        }
    }
}