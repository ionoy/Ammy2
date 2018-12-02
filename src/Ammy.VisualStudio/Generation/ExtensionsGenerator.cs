using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Ammy.VisualStudio.Generation
{
    internal class ExtensionsGenerator
    {
        public ConcurrentDictionary<string, TypeExtensionsGenerator> ExtensionInstances { get; } = new ConcurrentDictionary<string, TypeExtensionsGenerator>();

        public TypeExtensionsGenerator GetTypeExtensionsGenerator(string typeName, string typeShortName, bool isSealed) => 
            ExtensionInstances.GetOrAdd(typeName, _ => new TypeExtensionsGenerator(typeName, typeShortName, isSealed, this));

        readonly HashSet<string> _addedNames = new HashSet<string>();
        readonly HashSet<string> _collisions = new HashSet<string>();

        public void Generate(StringBuilder builder)
        {
            foreach (var generator in ExtensionInstances.Values)
                generator.Generate(builder);
        }

        public void AddName(string propertyOrEventName)
        {
            if (!_addedNames.Add(propertyOrEventName))
                _collisions.Add(propertyOrEventName);
        }

        public bool HasCollisions(string propertyOrEventName) => _collisions.Contains(propertyOrEventName);
    }
}