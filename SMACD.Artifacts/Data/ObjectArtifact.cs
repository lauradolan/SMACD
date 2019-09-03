using Polenter.Serialization;
using System;
using System.IO;

namespace SMACD.Artifacts.Data
{
    public class CustomTypeConverter : Polenter.Serialization.Advanced.Serializing.ITypeNameConverter
    {
        private Func<string, Type> ExternalTypeResolver { get; set; }
        public Type ConvertToType(string typeName)
        {
            if (typeName == null)
                return null;
            else if (Type.GetType(typeName) != null)
                return Type.GetType(typeName);
            else if (ExternalTypeResolver != null)
                return ExternalTypeResolver(typeName);
            else return null;
        }

        public string ConvertToTypeName(Type type)
        {
            return type.FullName;
        }

        public CustomTypeConverter(Func<string, Type> externalTypeResolver)
        {
            ExternalTypeResolver = externalTypeResolver;
        }
    }

    /// <summary>
    /// Represents an Artifact that contains a serialized object
    /// </summary>
    public class ObjectArtifact : DataArtifact
    {
        public ObjectArtifact(string name) : base(name) { }

        /// <summary>
        /// Get a deserialized instance of the object
        /// </summary>
        /// <returns></returns>
        public object Get()
        {
            return new SharpSerializer(new SharpSerializerXmlSettings()
            {
                AdvancedSettings = new Polenter.Serialization.Core.AdvancedSharpSerializerXmlSettings()
                {
                    TypeNameConverter = new CustomTypeConverter(t => Type.GetType(t))
                }
            }).Deserialize(new MemoryStream(StoredData));
        }

        /// <summary>
        /// Get a deserialized instance of the object (strongly typed)
        /// </summary>
        /// <typeparam name="T">Deserialized object's Type</typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            return (T)new SharpSerializer(new SharpSerializerXmlSettings()
            {
                AdvancedSettings = new Polenter.Serialization.Core.AdvancedSharpSerializerXmlSettings()
                {
                    TypeNameConverter = new CustomTypeConverter(t => Type.GetType(t))
                }
            }).Deserialize(new MemoryStream(StoredData));
        }

        /// <summary>
        /// Set the value of the Artifact to a given object, which will be JSON serialized
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object to serialize</param>
        public void Set<T>(T obj)
        {
            MemoryStream ms = new MemoryStream();
            new SharpSerializer(new SharpSerializerXmlSettings()
            {
                AdvancedSettings = new Polenter.Serialization.Core.AdvancedSharpSerializerXmlSettings()
                {
                    TypeNameConverter = new CustomTypeConverter(t => Type.GetType(t))
                }
            }).Serialize(obj, ms);
            ms.Seek(0, SeekOrigin.Begin);
            StoredData = ms.ToArray();
        }
    }
}
