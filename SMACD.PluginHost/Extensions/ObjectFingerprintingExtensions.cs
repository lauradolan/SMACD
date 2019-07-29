using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SMACD.PluginHost.Extensions
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
                allSkippedFields.AddRange(new[] { "resourceId", "systemCreated" });

            allSkippedFields.AddRange(skippedFields);
            allSkippedFields = allSkippedFields.Distinct().ToList();

            using (var sha1 = new SHA1Managed())
            {
                var str = Global.SerializeToString(obj, allSkippedFields.ToArray());
                return sha1.ComputeHash(Encoding.ASCII.GetBytes(str))
                    .Aggregate(new StringBuilder(), (current, next) => current.Append(next.ToString("X2")))
                    .ToString()
                    .Substring(0, hashLength);
            }
        }

        /// <summary>
        ///     Calculate SHA1 hash of a string (not cryptographically safe operation!)
        /// </summary>
        /// <param name="str">String to hash</param>
        /// <returns></returns>
        public static string SHA1(this string str)
        {
            try
            {
                var hash = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(str));
                var ret = new StringBuilder();

                for (var i = 0; i < hash.Length; i++)
                    ret.Append(hash[i].ToString("x2"));

                return ret.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}