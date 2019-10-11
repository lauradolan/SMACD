
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Specificity of the data provider which created this data
    /// </summary>
    public enum DataProviderSpecificity : int
    {
        /// <summary>
        ///     Unknown specificity
        /// </summary>
        Unknown = 1,

        /// <summary>
        ///     Data provider was a general-purpose scanner
        /// </summary>
        GeneralPurposeScanner = 2,

        /// <summary>
        ///     Data provider is designed for the specific service
        /// </summary>
        ServiceSpecificScanner = 3,

        /// <summary>
        ///     Data provider is designed for a specific exploit
        /// </summary>
        ExploitSpecificScanner = 4,

        /// <summary>
        ///     Data provider is explicit (i.e. known data)
        /// </summary>
        Explicit = 5
    }

    /// <summary>
    ///     Represents data which can be [partially] written to by multiple sources and coalesced to a single object
    /// </summary>
    public class Versionable
    {
    }

    /// <summary>
    ///     Represents data which can be [partially] written to by multiple sources and coalesced to a single object
    /// </summary>
    public class Versionable<TData> : Versionable where TData : new()
    {
        /// <summary>
        ///     Coalesce function; Default function orders layers by score and writes non-null values
        /// </summary>
        public static Func<List<Tuple<DataVersionDescriptor, TData>>, TData> CoalesceFunction { get; set; } = DefaultCoalesceFunction;

        /// <summary>
        ///     Underlying collection containing each version of data
        /// </summary>
        public List<Tuple<DataVersionDescriptor, TData>> UnderlyingCollection { get; set; } = new List<Tuple<DataVersionDescriptor, TData>>();

        /// <summary>
        ///     Get a copy of data by its Version ID
        /// </summary>
        /// <param name="uuid">Version ID</param>
        /// <returns></returns>
        public TData this[Guid uuid]
        {
            get
            {
                if (UnderlyingCollection.Any(i => i.Item1.VersionId == uuid))
                {
                    return UnderlyingCollection.First(i => i.Item1.VersionId == uuid).Item2;
                }

                return default(TData);
            }
        }

        /// <summary>
        ///     Represents data which can be [partially] written to by multiple sources and coalesced to a single object
        /// </summary>
        /// <param name="data">Initial version of data</param>
        /// <param name="dataVersionDescriptor">Initial data version descriptor</param>
        public Versionable(TData data, DataVersionDescriptor dataVersionDescriptor)
        {
            Set(data, dataVersionDescriptor);
        }

        /// <summary>
        ///     Represents data which can be [partially] written to by multiple sources and coalesced to a single object
        /// </summary>
        /// <param name="data">Initial version of data</param>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="dataProviderSpecificity">Specificity of data provider</param>
        /// <param name="confidence">Confidence in data accuracy</param>
        public Versionable(TData data, string extensionIdentifier, DataProviderSpecificity dataProviderSpecificity, double confidence = 1.0) :
            this(data, new DataVersionDescriptor(extensionIdentifier, dataProviderSpecificity, confidence))
        { }

        /// <summary>
        ///     Represents data which can be [partially] written to by multiple sources and coalesced to a single object
        /// </summary>
        public Versionable()
        {
        }

        /// <summary>
        ///     Add a version of data based on a given object and version descriptor
        /// </summary>
        /// <param name="data">Data to add</param>
        /// <param name="dataVersionDescriptor">Descriptor of version for this data payload</param>
        public void Set(TData data, DataVersionDescriptor dataVersionDescriptor)
        {
            if (UnderlyingCollection.Any(i => i.Item1 == dataVersionDescriptor))
            {
                return;
            }
            else
            {
                UnderlyingCollection.Add(Tuple.Create(dataVersionDescriptor, data));
            }
        }

        /// <summary>
        ///     Add a version of data based on a given object and version descriptor
        /// </summary>
        /// <param name="data">Data to add</param>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="dataProviderSpecificity">Specificity of data provider</param>
        /// <param name="confidence">Confidence in data accuracy</param>
        public void Set(TData data, string extensionIdentifier, DataProviderSpecificity dataProviderSpecificity, double confidence = 1.0)
        {
            Set(data, new DataVersionDescriptor(extensionIdentifier, dataProviderSpecificity, confidence));
        }

        /// <summary>
        ///     Get a flattened copy of the Versionable which represents the "best" and most "complete" data available from all version layers
        /// </summary>
        /// <returns></returns>
        public TData Coalesced()
        {
            return CoalesceFunction == null ? UnderlyingCollection.First().Item2 : CoalesceFunction(UnderlyingCollection);
        }

        /// <summary>
        ///     Convert the Versionable to its native TData
        /// </summary>
        /// <param name="v">Versionable data</param>
        public static implicit operator TData(Versionable<TData> v)
        {
            return v.Coalesced();
        }

        /// <summary>
        ///     String representation of coalesced value
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Coalesced().ToString();
        }

        /// <summary>
        ///     Default coalesce function - Orders versions by Score, applies non-null values from worst to best score
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static TData DefaultCoalesceFunction(List<Tuple<DataVersionDescriptor, TData>> data)
        {
            if (data.Count == 0)
            {
                return new TData();
            }

            if (data.Count == 1)
            {
                return data.First().Item2;
            }

            IEnumerable<TData> orderedDataLayers = data.OrderBy(d => d.Item1.Score).Select(d => d.Item2);
            TData coalesced = orderedDataLayers.First();
            foreach (TData layer in orderedDataLayers.Skip(1))
            {
                foreach (System.Reflection.PropertyInfo property in typeof(TData).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    object valueToMerge = property.GetValue(layer);
                    object defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;

                    if (valueToMerge.Equals(defaultValue))
                    {
                        continue;
                    }

                    property.SetValue(coalesced, valueToMerge);
                }
            }
            return coalesced;
        }
    }
}