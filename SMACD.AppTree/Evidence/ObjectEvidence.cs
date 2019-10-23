using Newtonsoft.Json;
using System.Text;

namespace SMACD.AppTree.Evidence
{
    /// <summary>
    ///     Represents Evidence that contains a serialized object
    /// </summary>
    public class ObjectEvidence : Evidence
    {
        /// <summary>
        ///     Represents Evidence that contains a serialized object
        /// </summary>
        /// <param name="name">Evidence name</param>
        public ObjectEvidence(string name) : base(name)
        {
        }

        /// <summary>
        ///     Get a deserialized instance of the object
        /// </summary>
        /// <returns></returns>
        public object Get()
        {
            return JsonConvert.DeserializeObject(Encoding.Unicode.GetString(StoredData),
                new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.All,
                    SerializationBinder = new AggressiveTypeResolutionBinder(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );
        }

        /// <summary>
        ///     Get a deserialized instance of the object (strongly typed)
        /// </summary>
        /// <typeparam name="T">Deserialized object's Type</typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            return JsonConvert.DeserializeObject<T>(Encoding.Unicode.GetString(StoredData),
                new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.All,
                    SerializationBinder = new AggressiveTypeResolutionBinder(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );
        }

        /// <summary>
        ///     Set the value of the Artifact to a given object, which will be JSON serialized
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object to serialize</param>
        public void Set<T>(T obj)
        {
            StoredData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj,
                new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.All,
                    SerializationBinder = new AggressiveTypeResolutionBinder(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            ));
        }
    }
}