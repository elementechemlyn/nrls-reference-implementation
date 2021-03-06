﻿using Demonstrator.NRLSAdapter.Helpers.Exceptions;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demonstrator.NRLSAdapter.Helpers
{
    public class FhirResponse
    {
        public Resource Resource { get; set; }

        public Uri ResponseLocation { get; set; }

        public List<Bundle.EntryComponent> Entries => ((Bundle)Resource).Entry;

        public T GetResource<T>() where T : Resource
        {
            var type = typeof(T);

            return (T)Resource;
        }

        public List<T> GetResources<T>() where T : Resource
        {
            var type = typeof(T);

            if (Resource.ResourceType == ResourceType.Bundle && ResourceTypeMap.ContainsKey(type))
            {
                return Entries
                    .Where(entry => entry.Resource.ResourceType.Equals(ResourceTypeMap[type]))
                    .Select(entry => (T)entry.Resource)
                    .ToList();
            }

            var resourceType = Resource.GetType();

            if (type == resourceType)
            {
                return new List<T>
                {
                    (T)Resource
                };
            }


            throw new InvalidResourceException(type.Name);
        }

        private static Dictionary<Type, ResourceType> ResourceTypeMap => new Dictionary<Type, ResourceType>
        {
            {typeof(Patient), ResourceType.Patient},
            {typeof(Organization), ResourceType.Organization},
            {typeof(DocumentReference), ResourceType.DocumentReference}
         };
    }
}
