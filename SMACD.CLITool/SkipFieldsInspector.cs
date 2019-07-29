using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace SMACD.CLITool
{
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