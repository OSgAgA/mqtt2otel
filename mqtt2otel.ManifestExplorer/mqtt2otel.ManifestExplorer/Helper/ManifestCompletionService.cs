using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// <returns>The hints as a monacco editor compatible json.</returns>
    public string Generate()
    {
        var schema = GenerateType(typeof(Manifest.Manifest));

        return JsonSerializer.Serialize(
            schema,
            new JsonSerializerOptions
            {
                WriteIndented = false
            });
    }

    /// <summary>
    /// Generates an object representation of a type.
    /// </summary>
    /// <param name="type">The root type.</param>
    /// <returns>The generated object representation.</returns>
    private object GenerateType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        // string
        if (type == typeof(string))
        {
            return new
            {
                type = "string"
            };
        }

        // bool
        if (type == typeof(bool))
        {
            return new
            {
                type = "boolean"
            };
        }

        // integer
        if (IsInteger(type))
        {
            return new
            {
                type = "integer"
            };
        }

        // floating point / decimal
        if (IsNumber(type))
        {
            return new
            {
                type = "number"
            };
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
                valueType = GenerateType(dictionaryValueType)
            };
        }

        // collection
        if (TryGetEnumerableElementType(type, out var elementType))
        {
            return new
            {
                type = "array",
                elementType = GenerateType(elementType)
            };
        }

        // complex object
        return GenerateObject(type);
    }

    /// <summary>
    /// Generate an object representation for the type.
    /// </summary>
    /// <param name="type">The root type.</param>
    /// <returns>The generated object.</returns>
    private object GenerateObject(Type type)
    {
        var properties = new Dictionary<string, object>();

        foreach (var property in GetProperties(type))
        {
            var yamlName = GetYamlName(property);

            properties[yamlName] = GenerateType(property.PropertyType);
        }

        return new
        {
            type = "object",
            properties
        };
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
    /// <returns></returns>
    private static string GetYamlName(PropertyInfo property)
    {
        var yamlMember = property.GetCustomAttribute<YamlMemberAttribute>();

        if (!string.IsNullOrWhiteSpace(yamlMember?.Alias))
            return yamlMember.Alias;

        return property.Name;
    }

    /// <summary>
    /// Tests if type is an integer.
    /// </summary>
    /// <param name="type">The type to be tested.</param>
    /// <returns>A value indicating whether the type is an integer.</returns>
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

    /// <summary>
    /// Tests whether the type is a number.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>A value indicating whether the type is a number.</returns>
    private static bool IsNumber(Type type)
    {
        return type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }

    /// <summary>
    /// Tries to get an enumberable element type.
    /// </summary>
    /// <param name="type">The base type.</param>
    /// <param name="elementType">The enumerable element type.</param>
    /// <returns>A value indicating success.</returns>
    private static bool TryGetEnumerableElementType(
        Type type,
        out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        var enumerable = type
            .GetInterfaces()
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

    /// <summary>
    /// Tries to get a dictionary value type.
    /// </summary>
    /// <param name="type">The base type.</param>
    /// <param name="elementType">The dictionary value type.</param>
    /// <returns>A value indicating success.</returns>
    private static bool TryGetDictionaryValueType(
        Type type,
        out Type valueType)
    {
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            valueType = type.GetGenericArguments()[1];
            return true;
        }

        var dictionary = type
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionary != null)
        {
            valueType = dictionary.GetGenericArguments()[1];
            return true;
        }

        valueType = null!;
        return false;
    }
}