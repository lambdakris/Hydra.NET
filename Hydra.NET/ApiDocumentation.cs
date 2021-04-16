﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hydra.NET
{
    public class ApiDocumentation
    {
        // Cached operation attributes
        private ILookup<Type, OperationAttribute>? _cachedOperationAttributes;

        // Supported classes
        private readonly List<SupportedClass> _supportedClasses = new List<SupportedClass>();

        public ApiDocumentation(Uri id) => (Context, Id) = (InitializeContext(), id);

        [JsonProperty(PropertyName = "@context", Order = 1)]
        public Context Context { get; private set; }

        [JsonProperty(PropertyName = "@id", Order = 2)]
        public Uri Id { get; }

        [JsonProperty(PropertyName = "supportedClass", Order = 4)]
        public List<SupportedClass> SupportedClasses => _supportedClasses;

        [JsonProperty(PropertyName = "@type", Order = 3)]
        public string Type => "ApiDocumentation";

        public ApiDocumentation AddSupportedClass<T>()
        {
            // Get the type of the supported class to be added
            Type type = typeof(T);

            // Get the class's SupportedClassAttribute
            SupportedClassAttribute? supportedClassAttribute = 
                type.GetCustomAttribute<SupportedClassAttribute>();

            // If the class doesn't have a SupportedClassAttribute, throw an ArgumentException
            if (supportedClassAttribute == null)
            {
                throw new ArgumentException($"{type.Name} cannot be added to API documentation " +
                    $"because it's not decorated with {nameof(SupportedClassAttribute)}.");
            }

            // Create a supported class from the attribute
            var supportedClass = new SupportedClass(supportedClassAttribute);

            // Get supported property attributes
            IEnumerable<SupportedPropertyAttribute> supportedPropertyAttributes =
                 type.GetProperties()
                    .Select(p => p.GetCustomAttribute<SupportedPropertyAttribute>())
                    .Where(a => a != null);

            // Create supported properties from the attributes
            if (supportedPropertyAttributes.Any())
            {
                supportedClass.SupportedProperties = supportedPropertyAttributes.Select(
                    a => new SupportedProperty(a));
            }

            // Add supported operations
            supportedClass.SupportedOperations = GetSupportedOperations(type);

            // Add the supported class
            _supportedClasses.Add(supportedClass);

            // Add a collection for the class, if specified
            TryAddSupportedCollection<T>(supportedClass.Id!, type);

            // Return the object for fluent-style functionality
            return this;
        }

        /// <summary>
        /// Adds a supported collection for the type, if specified.
        /// </summary>\
        /// <param name="memberId">The id of collection member type.</param>
        /// <param name="type">Type.</param>
        /// <returns>True if collection documentation was added; false, otherwise.</returns>
        private bool TryAddSupportedCollection<T>(Uri memberId, Type type)
        {
            // Get the class's SupportedCollectionAttribute
            SupportedCollectionAttribute? supportedCollectionAttribute =
                type.GetCustomAttribute<SupportedCollectionAttribute>();

            // Return false if the type doesn't have a collection specified
            if (supportedCollectionAttribute == null)
                return false;

            // Create a SupportedCollection object from the attribute
            var supportedCollection = new SupportedCollection(
                memberId, supportedCollectionAttribute);

            // Add supported operations
            supportedCollection.SupportedOperations = GetSupportedOperations(
                type, typeof(Collection<T>));

            // Add the collection to supported classes
            _supportedClasses.Add(supportedCollection);
            return true;
        }

        /// <summary>
        /// Gets supported operations for a type.
        /// </summary>
        /// <param name="type">The type for which to get supported operations.</param>
        /// <returns>Supported operations if any were found; null, otherwise.</returns>
        private IEnumerable<Operation>? GetSupportedOperations(
            Type type, Type? collectionType = null)
        {
            if (_cachedOperationAttributes == null)
            {
                _cachedOperationAttributes = Assembly
                    .GetAssembly(type)
                    .GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .SelectMany(m => m.GetCustomAttributes<OperationAttribute>())
                    .ToLookup(a => a.SupportedClassType);
            }

            Type searchType = collectionType ?? type;

            if (!_cachedOperationAttributes.Contains(searchType))
                return null;

            return _cachedOperationAttributes[searchType].Select(a => new Operation(a));
        }

        private Context InitializeContext()
        {
            return new Context(new Dictionary<string, Uri>()
            {
                { "hydra", new Uri("https://www.w3.org/ns/hydra/core#") },
                { "rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#") },
                { "rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#") },
                { "xsd", new Uri("http://www.w3.org/2001/XMLSchema#") },
                { "ApiDocumentation", new Uri("hydra:ApiDocumentation") },
                { "Class", new Uri("hydra:Class") },
                { "Collection", new Uri("hydra:Collection") },
                { "description", new Uri("hydra:description") },
                { "memberAssertion", new Uri("hydra:memberAssertion") },
                { "object", new Uri("hydra:object") },
                { "Operation", new Uri("hydra:Operation") },
                { "property", new Uri("hydra:property") },
                { "range", new Uri("rdfs:range") },
                { "readable", new Uri("hydra:readable") },
                { "required", new Uri("hydra:required") },
                { "supportedClass", new Uri("hydra:supportedClass") },
                { "supportedOperation", new Uri("hydra:supportedOperation") },
                { "supportedProperty", new Uri("hydra:supportedProperty") },
                { "SupportedProperty", new Uri("hydra:SupportedProperty") },
                { "title", new Uri("hydra:title") },
                { "writable", new Uri("hydra:writable") }
            });
        }
    }
}