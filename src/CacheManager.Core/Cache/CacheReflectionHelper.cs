﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    internal static class CacheReflectionHelper
    {
        internal static ICollection<BaseCacheHandle<TCacheValue>> CreateCacheHandles<TCacheValue>(BaseCacheManager<TCacheValue> manager)
        {
            var managerConfiguration = manager.Configuration;
            var handles = new List<BaseCacheHandle<TCacheValue>>();

            foreach (var handleConfiguration in managerConfiguration.CacheHandleConfigurations)
            {
                Type handleType = handleConfiguration.HandleType;
                Type instanceType = null;

                ValidateCacheHandleGenericTypeArguments(handleType);

                // if the configured type doesn't have a generic type definition ( <T> is not
                // defined )
                if (handleType.IsGenericTypeDefinition)
                {
                    instanceType = handleType.MakeGenericType(new Type[] { typeof(TCacheValue) });
                }
                else
                {
                    instanceType = handleType;
                }

                var handleInstance = Activator.CreateInstance(instanceType, new object[] { manager, handleConfiguration });
                var instance = handleInstance as BaseCacheHandle<TCacheValue>;
                handles.Add(instance);
            }
            
            return handles;
        }

        internal static CacheBackPlate CreateBackPlate<TCacheValue>(BaseCacheManager<TCacheValue> manager)
        {
            if(manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if(manager.Configuration == null)
            {
                throw new ArgumentException("Manager's configuration must not be null.", "manager");
            }

            if (manager.Configuration.BackPlateType != null)
            {
                if (!manager.CacheHandles.Any(p => p.Configuration.IsBackPlateSource))
                {
                    throw new InvalidOperationException("At least one cache handle must be marked as the back plate source.");
                }

                try
                {
                    var backPlate = (CacheBackPlate)Activator.CreateInstance(
                        manager.Configuration.BackPlateType,
                        new object[]
                        {
                            manager.Configuration,
                            manager.Name
                        });

                    return backPlate;
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }

            return null;
        }

        public static IEnumerable<Type> GetGenericBaseTypes(this Type type)
        {
            if (!type.BaseType.IsGenericType)
            {
                return Enumerable.Empty<Type>();
            }

            var genericBaseType = type.BaseType.IsGenericTypeDefinition ? type.BaseType : type.BaseType.GetGenericTypeDefinition();
            return Enumerable.Repeat(genericBaseType, 1)
                             .Concat(type.BaseType.GetGenericBaseTypes());
        }

        public static void ValidateCacheHandleGenericTypeArguments(Type handle)
        {
            // not really needed due to the generic type from callees being restricted.
            if (!handle.GetGenericBaseTypes().Any(p => p == typeof(BaseCacheHandle<>)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Configured cache handle does not implement base cache handle [{0}].",
                        handle.ToString()));
            }

            if (handle.IsGenericType && !handle.IsGenericTypeDefinition)
            {
                if (handle.GetGenericArguments().Count() != 1)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Invalid number of generic type arguments found for handle [{0}].",
                            handle.ToString()));
                }
                if (handle.GetGenericArguments().Any())
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cache handle type [{0}] should not have any generic arguments defined.",
                            handle.ToString()));
                }
            }
        }
    }
}