using Microsoft.Extensions.Logging;
using mqtt2otel.Configuration;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Provides static helper methods to work with types.
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Calls a method with a given generic type.
        /// </summary>
        /// <param name="instance">The class instance on which the method should be called.</param>
        /// <param name="genericTypeName">The name of the generic type.</param>
        /// <param name="methodName">The name of the method that will be called.</param>
        /// <param name="parameters">The parameters that will be passed to the method.</param>
        /// <returns>The method result.</returns>
        /// <exception cref="ArgumentException">Thrown, when an unknown type name is provided.</exception>
        public static object? CallMethodWithGenericType(object instance, string genericTypeName, string methodName, object[] parameters)
        {
            if (!TypeHelper.TypeMap.TryGetValue(genericTypeName, out var genericType))
                throw new ArgumentException($"Unknown type '{genericTypeName}'");

            return TypeHelper.CallMethodWithGenericType(instance, genericType, methodName, parameters);
        }

        /// <summary>
        /// Calls a method with a given generic type.
        /// </summary>
        /// <param name="instance">The class instance on which the method should be called.</param>
        /// <param name="genericTypeName">The name of the generic type.</param>
        /// <param name="methodName">The name of the method that will be called.</param>
        /// <param name="parameters">The parameters that will be passed to the method.</param>
        /// <returns>The method result.</returns>
        /// <exception cref="ArgumentException">Thrown, when an unknown type name is provided.</exception>
        public static object? CallMethodWithGenericType(object instance, SignalDataType genericTypeName, string methodName, object[] parameters)
        {
            if (!TypeHelper.SignalTypeMap.TryGetValue(genericTypeName, out var genericType))
                throw new ArgumentException($"Unknown type '{genericTypeName}'");

            return TypeHelper.CallMethodWithGenericType(instance, genericType, methodName, parameters);
        }

        /// <summary>
        /// Calls a method with a given generic type.
        /// </summary>
        /// <param name="instance">The class instance on which the method should be called.</param>
        /// <param name="genericType">The generic type.</param>
        /// <param name="methodName">The name of the method that will be called.</param>
        /// <param name="parameters">The parameters that will be passed to the method.</param>
        /// <returns>The method result.</returns>
        public static object? CallMethodWithGenericType(object instance, Type genericType, string methodName, object[] parameters)
        {
            var method = instance.GetType()
                             .GetMethod(
                                 methodName,
                                 BindingFlags.Instance | BindingFlags.NonPublic
                             )?
                             .MakeGenericMethod(genericType);

            if (method != null) return method.Invoke(instance, parameters);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts an object to the given type.
        /// </summary>
        /// <typeparam name="TResult">The type to which the object should be converted to.</typeparam>
        /// <param name="value">The value that should be converted.</param>
        /// <returns></returns>
        public static TResult ConvertObject<TResult>(object value)
        {
            return (TResult)System.Convert.ChangeType(value, typeof(TResult));
        }

        /// <summary>
        /// Gets the type of a provided <see cref="SignalDataType"/>.
        /// </summary>
        /// <param name="dataType">The signal data type.</param>
        /// <returns>The type associated with the signal data type.</returns>
        public static Type GetType(SignalDataType dataType)
        {
            return TypeHelper.SignalTypeMap[dataType];
        }

        /// <summary>
        /// Gets the type based on a string description.
        /// </summary>
        /// <param name="dataType">The string representation of a type.</param>
        /// <returns>The type represented by the string.</returns>
        public static Type GetType(string dataType)
        {
            return TypeHelper.TypeMap[dataType];
        }

        /// <summary>
        /// Maps <see cref="SignalDataType"/> to a system type.
        /// </summary>
        private static readonly Dictionary<SignalDataType, Type> SignalTypeMap = new()
        {
            [SignalDataType.Int] = typeof(int),
            [SignalDataType.Float] = typeof(float),
            [SignalDataType.Double] = typeof(double),
            [SignalDataType.Decimal] = typeof(decimal),
            [SignalDataType.String] = typeof(string),
            [SignalDataType.Long] = typeof(long),
            [SignalDataType.DateTime] = typeof(DateTime),
        };

        /// <summary>
        /// Maps strings to system types.
        /// </summary>
        private static readonly Dictionary<string, Type> TypeMap = new()
        {
            ["int"] = typeof(int),
            ["float"] = typeof(float),
            ["double"] = typeof(double),
            ["decimal"] = typeof(decimal),
            ["string"] = typeof(string),
            ["long"] = typeof(long),
            ["DateTime"] = typeof(DateTime),
        };

        /// <summary>
        /// Maps strings to <see cref="LogLevel"/>.
        /// </summary>
        private static readonly Dictionary<string, LogLevel> LogLevelMap = new()
        {
            ["T"] = LogLevel.Trace,
            ["TRC"] = LogLevel.Trace,
            ["TRACE"] = LogLevel.Trace,
            ["D"] = LogLevel.Debug,
            ["DBG"] = LogLevel.Debug,
            ["DEBUG"] = LogLevel.Debug,
            ["I"] = LogLevel.Information,
            ["INF"] = LogLevel.Information,
            ["INFO"] = LogLevel.Information,
            ["INFORMATION"] = LogLevel.Information,
            ["W"] = LogLevel.Warning,
            ["WRN"] = LogLevel.Warning,
            ["WARN"] = LogLevel.Warning,
            ["WARNING"] = LogLevel.Warning,
            ["E"] = LogLevel.Error,
            ["ERR"] = LogLevel.Error,
            ["ERROR"] = LogLevel.Error,
            ["C"] = LogLevel.Critical,
            ["CTL"] = LogLevel.Critical,
            ["CRIT"] = LogLevel.Critical,
            ["CRITICAL"] = LogLevel.Critical,
            ["A"] = LogLevel.Critical,
            ["ALT"] = LogLevel.Critical,
            ["ALRT"] = LogLevel.Critical,
            ["ALERT"] = LogLevel.Critical,
            ["F"] = LogLevel.Critical,
            ["FTL"] = LogLevel.Critical,
            ["FATAL"] = LogLevel.Critical,
        };

        /// <summary>
        /// Gets a string representation for the provide type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The string representation.</returns>
        /// <exception cref="NotImplementedException">Thrown if type is not supported.</exception>
        public static string GetTypeRepresentation(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(long)) return "long";
            if (type == typeof(DateTime)) return "DateTime";

            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses a string to a given type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="input">The string that will be parsed.</param>
        /// <returns>The value as the given type.</returns>
        public static T Parse<T>(string input)
        {
            var type = typeof(T);
            object result = 0;
            
            if (type == typeof(int)) result = int.Parse(input, CultureInfo.InvariantCulture);
            if (type == typeof(float)) result = float.Parse(input, CultureInfo.InvariantCulture);
            if (type == typeof(string)) result = input;
            if (type == typeof(double)) result = double.Parse(input, CultureInfo.InvariantCulture);
            if (type == typeof(decimal)) result = decimal.Parse(input, CultureInfo.InvariantCulture);
            if (type == typeof(long)) result = long.Parse(input, CultureInfo.InvariantCulture);
            if (type == typeof(DateTime)) result = DateTime.Parse(input, CultureInfo.InvariantCulture);

            return (T)result;
        }

        /// <summary>
        /// Parses a list of strings to a list of typed values.
        /// </summary>
        /// <typeparam name="T">The type of the list values.</typeparam>
        /// <param name="input">The list values.</param>
        /// <returns>A list with typed values.</returns>
        public static List<T> Parse<T>(List<string> input)
        {
            var result = new List<T>();

            foreach (var item in input)
            {
                result.Add(TypeHelper.Parse<T>(item));
            }

            return result;
        }

        /// <summary>
        /// Tries to parse a log level from a string.
        /// </summary>
        /// <param name="input">The input that will be parsed.</param>
        /// <param name="loglevel">The parsed log level as an output parameter. Will be Information, if parsing was not successful.</param>
        /// <returns>A value indicating whether parsing was successful.</returns>
        public static bool TryParseLogLevel(string input, out LogLevel loglevel)
        {
            input = input.ToUpper();
            if (!TypeHelper.LogLevelMap.ContainsKey(input))
            {
                loglevel = LogLevel.Information;
                return false;
            }

            loglevel = TypeHelper.LogLevelMap[input];
            return true;
        }
    }
}
