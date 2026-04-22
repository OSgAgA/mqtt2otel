using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a list that support item that can be imported from a file.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ImportEnabledList<T> : List<T> where T : NamedIdObject
    {
        /// <summary>
        /// Initializes the list. Every item in the list that has the property <see cref="NamedIdObject.ImportFrom"/> set, will trigger an
        /// import from this path. This import will then be added to this.
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="objectFactory">The yaml object factory used for creating the objects from the yaml file.</param>
        /// <exception cref="Exception">Thrown if path is not available or cannot be parsed.</exception>
        public void Initialize(ILogger internalLogger, IObjectFactory objectFactory)
        {
            if (objectFactory == null)
            {
                internalLogger.LogCritical($"Internal error: Calling {nameof(T)}.{nameof(Initialize)} without initializíng {nameof(ObjectFactory)} first. Providing default manifest.");
                return;
            }

            var result = new List<T>();

            foreach (var item in this)
            {
                if (item.ImportFrom != null)
                {
                    var directory = Path.GetDirectoryName(item.ImportFrom) ?? "./";
                    if (string.IsNullOrWhiteSpace(directory)) directory = "./";
                    var filename = Path.GetFileName(item.ImportFrom);
                    var files = Directory.EnumerateFiles(directory, filename);

                    foreach (var path in files)
                    {
                        internalLogger.LogInformation($"Importing {typeof(T).Name} from {Path.GetFullPath(path)}");
                        var yaml = File.ReadAllText(path);
                        var deserializer = new DeserializerBuilder().WithObjectFactory(objectFactory).Build();

                        result.AddRange(deserializer.Deserialize<List<T>>(yaml));
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            this.Clear();
            this.AddRange(result);
        }

        /// <summary>
        /// Traverses recursively through a root object, identifies all properties that have a type of <see cref="ImportEnabledList{T}"/> and calls there
        /// Initialize method.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="objectFactory">The yaml object factory used for creating the objects from the yaml file.</param>
        public static void InitializeImports(object root, ILogger internalLogger, IObjectFactory objectFactory)
        {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            Traverse(root, visited, internalLogger, objectFactory);
        }

        /// <summary>
        /// Traverses recursively through an object, identifies all properties that have a type of <see cref="ImportEnabledList{T}"/> and calls there
        /// Initialize method.
        /// </summary>
        /// <param name="obj">The object to traverse.</param>
        /// <param name="visited">A list of allready visited objects.</param>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="objectFactory">The yaml object factory used for creating the objects from the yaml file.</param>
        private static void Traverse(object obj, HashSet<object> visited, ILogger internalLogger, IObjectFactory objectFactory)
        {
            if (obj == null)
                return;

            var type = obj.GetType();

            // skip primitive / simple types
            if (IsSimple(type))
                return;

            // prevent cycles
            if (!visited.Add(obj))
                return;

            // If it's ImportEnabledList<T> → call Initialize()
            if (IsImportEnabledList(type))
            {
                var method = type.GetMethod("Initialize");
                method?.Invoke(obj, new object[] { internalLogger, objectFactory });
                return;
            }

            // If it's IEnumerable → iterate items
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    Traverse(item, visited, internalLogger, objectFactory);
                }

                return;
            }

            // Traverse properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                    continue;

                var value = prop.GetValue(obj);
                if (value == null)
                    continue;

                Traverse(value, visited, internalLogger, objectFactory);
            }
        }

        /// <summary>
        /// Tests whether the given type is a <see cref="ImportEnabledList{T}"/>.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns>A value indicating whether the type is of <see cref="IEnumerable{T}"/>.</returns>
        private static bool IsImportEnabledList(Type? type)
        {
            while (type != null)
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(ImportEnabledList<>))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Tests whether the type is a simple type, in the sense that it should not be traversed.
        /// </summary>
        /// <param name="type">The type to be tested.</param>
        /// <returns>A value indicating whether the type is simple</returns>
        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(Guid);
        }
    }
}
