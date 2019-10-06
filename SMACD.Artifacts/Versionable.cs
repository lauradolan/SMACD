using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.Artifacts
{
    public enum DataProviderSpecificity : int
    {
        Unknown = 1,
        GeneralPurposeScanner = 2,
        ServiceSpecificScanner = 3,
        ExploitSpecificScanner = 4,
        Explicit = 5
    }

    public class Versionable
    {
    }

    public class Versionable<TData> : Versionable where TData : new()
    {
        public static Func<List<Tuple<DataVersionDescriptor, TData>>, TData> CoalesceFunction { get; set; } = DefaultCoalesceFunction;
        public List<Tuple<DataVersionDescriptor, TData>> UnderlyingCollection { get; set; } = new List<Tuple<DataVersionDescriptor, TData>>();

        public TData this[Guid uuid]
        {
            get
            {
                if (UnderlyingCollection.Any(i => i.Item1.VersionId == uuid))
                    return UnderlyingCollection.First(i => i.Item1.VersionId == uuid).Item2;
                return default(TData);
            }
        }

        public Versionable(TData data, DataVersionDescriptor dataVersionDescriptor)
        {
            Set(data, dataVersionDescriptor);
        }
        public Versionable(TData data, string extensionIdentifier, DataProviderSpecificity dataProviderSpecificity, double confidence = 1.0) :
            this(data, new DataVersionDescriptor(extensionIdentifier, dataProviderSpecificity, confidence))
        { }

        public Versionable()
        {
        }

        public void Set(TData data, DataVersionDescriptor dataVersionDescriptor)
        {
            if (UnderlyingCollection.Any(i => i.Item1 == dataVersionDescriptor))
                return;
            else
                UnderlyingCollection.Add(Tuple.Create(dataVersionDescriptor, data));
        }
        public void Set(TData data, string extensionIdentifier, DataProviderSpecificity dataProviderSpecificity, double confidence = 1.0)
        {
            Set(data, new DataVersionDescriptor(extensionIdentifier, dataProviderSpecificity, confidence));
        }

        public TData Coalesced() => CoalesceFunction == null ? UnderlyingCollection.First().Item2 : CoalesceFunction(UnderlyingCollection);

        public static implicit operator TData(Versionable<TData> v) => v.Coalesced();

        public override string ToString() => Coalesced().ToString();

        private static TData DefaultCoalesceFunction(List<Tuple<DataVersionDescriptor, TData>> data)
        {
            if (data.Count == 0) return new TData();
            if (data.Count == 1) return data.First().Item2;

            var orderedDataLayers = data.OrderBy(d => d.Item1.Score).Select(d => d.Item2);
            var coalesced = orderedDataLayers.First();
            foreach (var layer in orderedDataLayers.Skip(1))
            {
                foreach (var property in typeof(TData).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    var valueToMerge = property.GetValue(layer);

                    if (property.PropertyType.IsValueType && valueToMerge == Activator.CreateInstance(property.PropertyType)) ;
                    else if (!property.PropertyType.IsValueType && valueToMerge == null)
                        continue;

                    property.SetValue(coalesced, valueToMerge);
                }
            }
            return coalesced;
        }
    }

    public class DataVersionDescriptor
    {
        public Guid VersionId { get; set; } = Guid.NewGuid();
        public string ExtensionIdentifier { get; set; }
        public double Confidence { get; set; }
        public DataProviderSpecificity ProviderSpecificity { get; set; } = DataProviderSpecificity.Unknown;
        public double Score => Confidence * (int)ProviderSpecificity;

        public override bool Equals(object obj) =>
            (obj is DataVersionDescriptor &&
                ((DataVersionDescriptor)obj).Score == this.Score &&
                ((DataVersionDescriptor)obj).ExtensionIdentifier == this.ExtensionIdentifier);

        public override int GetHashCode() => HashCode.Combine(ExtensionIdentifier, Score);

        public DataVersionDescriptor() { }
        public DataVersionDescriptor(string extensionIdentifier, DataProviderSpecificity dataProviderSpecificity, double confidence = 1.0)
        {
            ExtensionIdentifier = extensionIdentifier;
            ProviderSpecificity = dataProviderSpecificity;
            Confidence = confidence;
        }
    }
}
