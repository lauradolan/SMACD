using System.Text;

namespace SMACD.Artifacts.Data
{
    /// <summary>
    ///     Represents an Artifact that contains a string
    /// </summary>
    public class StringArtifact : DataArtifact
    {
        public StringArtifact(string name) : base(name)
        {
        }

        /// <summary>
        ///     Get the saved string
        /// </summary>
        /// <returns></returns>
        public string Get()
        {
            return Encoding.UTF8.GetString(StoredData);
        }

        /// <summary>
        ///     Set the saved value to the given string
        /// </summary>
        /// <param name="data">String to commit</param>
        public void Set(string data)
        {
            StoredData = Encoding.UTF8.GetBytes(data);
        }
    }
}