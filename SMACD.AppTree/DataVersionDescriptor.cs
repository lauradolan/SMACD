using System;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Descriptor of the data provider who contributed a chunk of data
    /// </summary>
    public class DataVersionDescriptor
    {
        /// <summary>
        ///     Version identifier
        /// </summary>
        public Guid VersionId { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Extension identifier
        /// </summary>
        public string ExtensionIdentifier { get; set; }

        /// <summary>
        ///     Confidence in data accuracy
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        ///     Specificity of the data provider contributing the data
        /// </summary>
        public DataProviderSpecificity ProviderSpecificity { get; set; } = DataProviderSpecificity.Unknown;
        
        /// <summary>
        ///     Overall score of confidence in this version of the data, based on the confidence and specificity of provider
        /// </summary>
        public double Score => Confidence * (int)ProviderSpecificity;

        /// <summary>
        ///     Checks equalities based on the version descriptor's Extension Identifier and overall Score
        /// </summary>
        /// <param name="obj">Object to test</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (obj is DataVersionDescriptor &&
                ((DataVersionDescriptor)obj).Score == Score &&
                ((DataVersionDescriptor)obj).ExtensionIdentifier == ExtensionIdentifier);
        }

        /// <summary>
        ///     Generates a HashCode from the Extension Identifier and overall Score
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(ExtensionIdentifier, Score);
        }

        /// <summary>
        ///     Descriptor of the data provider who contributed a chunk of data
        /// </summary>
        public DataVersionDescriptor() { }

        /// <summary>
        ///     Descriptor of the data provider who contributed a chunk of data
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="dataProviderSpecificity">Specificity of data provider</param>
        /// <param name="confidence">Confidence in data accuracy</param>
        public DataVersionDescriptor(string extensionIdentifier, DataProviderSpecificity dataProviderSpecificity, double confidence = 1.0)
        {
            ExtensionIdentifier = extensionIdentifier;
            ProviderSpecificity = dataProviderSpecificity;
            Confidence = confidence;
        }
    }
}
