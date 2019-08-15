using System;
using Xamarin.Forms.Dynamic;

namespace SMACD.Artifacts
{
    public class UrlRequestArtifact : Artifact
    {
        /// <summary>
        /// Artifact Identifier
        /// </summary>
        public override string Identifier => $"{HashCode.Combine(fields, headers)}";

        private ObservableDictionary<string, string> fields = new ObservableDictionary<string, string>();
        private ObservableDictionary<string, string> headers = new ObservableDictionary<string, string>();

        /// <summary>
        /// Fields to be sent with request
        /// </summary>
        public ObservableDictionary<string, string> Fields
        {
            get => fields;
            set
            {
                fields = value;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Headers to be sent with request
        /// </summary>
        public ObservableDictionary<string, string> Headers
        {
            get => headers;
            set
            {
                headers = value;
                NotifyChanged();
            }
        }

        public UrlRequestArtifact()
        {
            fields.CollectionChanged += (s, e) => NotifyChanged();
            headers.CollectionChanged += (s, e) => NotifyChanged();
        }

        public override string ToString()
        {
            return $"URL Request";
        }
    }
}
