using System.Reflection;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace mqtt2otel.ManifestExplorer.Helper;

/// <summary>
/// Generates a json that can be used by the monacco editor to supply code completion hints.
/// </summary>
public sealed class ManifestCompletionService
{
    /// <summary>
    /// Generates the code completion hints for a manifest file.
    /// </summary>
    /// <returns>
    /// The hints as a monacco editor compatible json in the form
    /// <c>{ "root": {...}, "definitions": { "TypeKey": {...} } }</c>.
    /// Object types are only stored in <c>definitions</c> and are referenced via <c>{ "type": "ref", "ref": "TypeKey" }</c>.
    /// </returns>
    public string Generate()
    {
        var definitions = new Dictionary<string, object?>();
        var root = GenerateType(typeof(Manifest.Manifest), definitions);

        return JsonSerializer.Serialize(
            new
            {
                root,
                definitions
            },
            new JsonSerializerOptions
            {
                WriteIndented = false
            });
    }

    /// <summary>
    /// Generates an object representation of a type.
    /// </summary>
    /// <param name="type">The root type.</param>
    /// <param name="definitions">The shared table of already known object definitions.</param>
    /// <returns>The generated object representation.</returns>
    private object GenerateType(Type type, Dictionary<string, object?> definitions)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        // string
        if (type == typeof(string))
        {
            return new { type = "string" };
        }

        // bool
        if (type == typeof(bool))
        {
            return new { type = "boolean" };
        }

        // integer
        if (IsInteger(type))
        {
            return new { type = "integer" };
        }

        // floating point / decimal
        if (IsNumber(type))
        {
            return new { type = "number" };
        }

        // enum
        if (type.IsEnum)
        {
            return new
            {
                type = "string",
                values = Enum.GetNames(type)
            };
        }

        // dictionary
        if (TryGetDictionaryValueType(type, out var dictionaryValueType))
        {
            return new
            {
                type = "dictionary",
                valueType = GenerateType(dictionaryValueType, definitions)
            };
        }

        // collection
        if (TryGetEnumerableElementType(type, out var elementType))
        {
            return new
            {
                type = "array",
                elementType = GenerateType(elementType, definitions)
            };
        }

        // complex object
        return GenerateObjectReference(type, definitions);
    }

    /// <summary>
    /// Registers the object type in the definitions (if not already done) and returns a reference to it.
    /// </summary>
    /// <param name="type">The object type.</param>
    /// <param name="definitions">The shared table of already known object definitions.</param>
    /// <returns>A reference to the object definition.</returns>
    private object GenerateObjectReference(Type type, Dictionary<string, object?> definitions)
    {
        var key = GetDefinitionKey(type);

        // Already generated or currently being generated (recursion) -> just reference it.
        if (!definitions.ContainsKey(key))
        {
            // Reserve the slot BEFORE visiting the properties, so that self-references
            // (directly or indirectly) find the key and stop recursing.
            definitions[key] = null;

            var properties = new Dictionary<string, object>();

            foreach (var property in GetProperties(type))
            {
                var yamlName = GetYamlName(property);

                properties[yamlName] = GenerateType(property.PropertyType, definitions);
            }

            definitions[key] = new
            {
                type = "object",
                properties
            };
        }

        return new
        {
            type = "ref",
            @ref = key
        };
    }

    /// <summary>
    /// Gets a unique key for a type that is used in the definitions table.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The key.</returns>
    private static string GetDefinitionKey(Type type)
    {
        return type.FullName ?? type.Name;
    }

    /// <summary>
    /// Gets all public writeable properties of a type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>All public writeable properties.</returns>
    private static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Where(p => p.GetIndexParameters().Length == 0);
    }

    /// <summary>
    /// Gets the yaml name of a property.
    /// </summary>
    /// <param name="property">The property for which the yaml name should be determined.</param>
    /// <returns>The yaml name.</returns>
    private static string GetYamlName(PropertyInfo property)
    {
        var yamlMember = property.GetCustomAttribute<YamlMemberAttribute>();

        if (!string.IsNullOrWhiteSpace(yamlMember?.Alias))
            return yamlMember.Alias;

        return property.Name;
    }

    private static bool IsInteger(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong);
    }

    private static bool IsNumber(Type type)
    {
        return type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        var enumerable = type
            .GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable != null)
        {
            elementType = enumerable.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        var dictionary = type
            .GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

        if (dictionary != null)
        {
            valueType = dictionary.GetGenericArguments()[1];
            return true;
        }

        valueType = null!;
        return false;
    }
}