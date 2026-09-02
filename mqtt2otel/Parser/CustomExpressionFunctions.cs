using NCalc;
using NCalc.Exceptions;
using NCalc.Extensions;
using NCalc.Handlers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a collection of custom functions that are added to parse expressions.
    /// </summary>
    public static class CustomExpressionFunctions
    {
        /// <summary>
        /// Adds all custom functions to the given expression.
        /// </summary>
        /// <param name="context">The expression context to which the functions should be added.,</param>
        public static void AddTo(NCalc.ExpressionContext context)
        {
            AddParseDateTimeFunction(context, "ParseDateTime");

            AddDateTimeFunction(context, "AddDays", (date, increment) => date.AddDays(increment));
            AddDateTimeFunction(context, "AddMonths", (date, increment) => date.AddMonths(increment));
            AddDateTimeFunction(context, "AddYears", (date, increment) => date.AddYears(increment));
            AddDateTimeFunction(context, "AddHours", (date, increment) => date.AddHours(increment));
            AddDateTimeFunction(context, "AddMinutes", (date, increment) => date.AddMinutes(increment));
            AddDateTimeFunction(context, "AddSeconds", (date, increment) => date.AddSeconds(increment));

            AddConvertTimeZoneFunction(context, "ConvertTimezone");

            AddToLower(context, "ToLower");
            AddToUpper(context, "ToUpper");
            AddReplace(context, "Replace");

            AddCaseFunction(context, "ToPascalCase", string.Empty, firstIsLower: false, restIsLower: false);
            AddCaseFunction(context, "ToCamelCase", string.Empty, firstIsLower: true, restIsLower: false);
            AddCaseFunction(context, "ToSnakeCase", "_", firstIsLower: true, restIsLower: true);
            AddCaseFunction(context, "ToKebabCase", "-", firstIsLower: true, restIsLower: true);
            AddCaseFunction(context, "ToTrainCase", "-", firstIsLower: false, restIsLower: false);
        }

        /// <summary>
        /// Gets a function argument as a given type.
        /// </summary>
        /// <typeparam name="TResult">The expected type of the argument.</typeparam>
        /// <param name="functionName">The name of the function to which the argument was provided.</param>
        /// <param name="index">The zero based index of the argument.</param>
        /// <param name="args">The function arguments.</param>
        /// <returns>The argument as the given type.</returns>
        /// <exception cref="ArgumentTypeException">Thrown if argument could not be case to the given type.</exception>
        public static TResult GetArgument<TResult>(string functionName, ExpressionContext context, int index, FunctionData args)
        {
            try
            {
                return (TResult)(args[index].Evaluate(context) ?? throw new Exception());
            }
            catch (NCalcFunctionNotFoundException ex)
            {
                throw new FunctionNotFoundException(ex.FunctionName);
            }
            catch
            {
                throw new ArgumentTypeException(functionName, index, typeof(TResult), args[index]?.ToExpressionString() ?? string.Empty);
            }
        }

        /// <summary>
        /// Adds a function that is able to convert a DateTime of one timezone to another timezone.
        /// 
        /// Usage:
        ///   functionName( date, sourceTimezone, destTimezont )
        ///   
        /// Where timezones are as defined in <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>.
        /// </summary>
        /// <param name="context">The expresssion context to which this function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 3 arguments.</exception>
        private static void AddConvertTimeZoneFunction(NCalc.ExpressionContext context, string functionName)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 3)
                {
                    var date = GetArgument<DateTime>(functionName, context, 0, args);
                    var sourceTimezone = GetArgument<string>(functionName, context, 1, args);
                    var destTimezone = GetArgument<string>(functionName, context, 2, args);

                    var utc = TimeZoneInfo.ConvertTimeToUtc(date, TimeZoneInfo.FindSystemTimeZoneById(sourceTimezone));

                    return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(destTimezone));
                }

                throw new InvalidArgumentCountException(functionName, 1, 2, args.Count());
            };
        }

        /// <summary>
        /// Adds a function that is able to parse a string representation of a DateTime to a DateTime object.
        /// 
        /// Usage:
        ///   functionName( dateAsString )
        ///   functionName( dateAsString, formatString )
        ///   
        /// format strings are parsed with InvariantCulture.
        /// </summary>
        /// <param name="context">The expresssion context to which this function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not 1-2 arguments.</exception>
        /// <exception cref="ParsingFailedException">Thrown if the string could not be parsed to a DateTime.</exception>
        private static void AddParseDateTimeFunction(NCalc.ExpressionContext context, string functionName)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 1)
                {
                    var dateAsString = GetArgument<string>(functionName, context, 0, args);

                    try
                    {
                        return DateTime.Parse(dateAsString);
                    }
                    catch
                    {
                        throw new ParsingFailedException(functionName, 0, typeof(DateTime), dateAsString);
                    }
                }
                if (args.Count() == 2)
                {
                    var dateAsString = GetArgument<string>(functionName, context, 0, args);
                    var format = GetArgument<string>(functionName, context, 1, args);

                    try
                    {
                        return DateTime.ParseExact(dateAsString, format, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        throw new ParsingFailedException(functionName, 0, typeof(DateTime), $"{dateAsString} [format: {format}]");
                    }
                }

                throw new InvalidArgumentCountException(functionName, 1, 2, args.Count());
            };
        }

        /// <summary>
        /// Adds a function that will call a method on a dateTime object. The method will be provided with a single integer argument.
        /// 
        /// Usage:
        ///   functionName( date, intValue )
        ///   
        /// Examples:
        ///   AddMonths( date, months )
        ///   AddDays ( date, days )
        ///   
        /// Calls the provided function.
        /// </summary>
        /// <param name="context">The expresssion context to which this function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <param name="func">The function that should be called on the date argument.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 2 arguments.</exception>

        private static void AddDateTimeFunction(NCalc.ExpressionContext context, string functionName, Func<DateTime, int, DateTime> func)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 2)
                {
                    var date = GetArgument<DateTime>(functionName, context, 0, args);
                    var inc = GetArgument<int>(functionName, context, 1, args);

                    return func(date, inc);
                }

                throw new InvalidArgumentCountException(functionName, 2, 2, args.Count());
            };
        }

        /// <summary>
        /// Adds a function that will convert a given parameter to lowercase.
        /// 
        /// Usage:
        ///    functionName( parameter )
        ///    
        /// Examples:
        ///    functionName( "Hello World" ) => "hello world"
        /// </summary>
        /// <param name="context">The expression context, where the function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 1 arguments.</exception>
        private static void AddToLower(NCalc.ExpressionContext context, string functionName)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 1)
                {
                    return GetArgument<string>(functionName, context, 0, args).ToLower();
                }

                throw new InvalidArgumentCountException(functionName, 1, 1, args.Count());
            };
        }

        /// <summary>
        /// Adds a function that will convert a given parameter to upper case.
        /// 
        /// Usage:
        ///    functionName( parameter )
        ///    
        /// Examples:
        ///    functionName( "Hello World" ) => "HELLO WORLD"
        /// </summary>
        /// <param name="context">The expression context, where the function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 1 arguments.</exception>
        private static void AddToUpper(NCalc.ExpressionContext context, string functionName)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 1)
                {
                    return GetArgument<string>(functionName, context, 0, args).ToUpper();
                }

                throw new InvalidArgumentCountException(functionName, 1, 1, args.Count());
            };
        }

        /// <summary>
        /// Adds a function that replace substrings inside a string with a given replacement.
        /// 
        /// Usage:
        ///    functionName( source, old, replacement )
        ///    
        /// Examples:
        ///    functionName( "Hello_world", "_", " " ) => "Hello world"
        /// </summary>
        /// <param name="context">The expression context, where the function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 3 arguments.</exception>
        private static void AddReplace(NCalc.ExpressionContext context, string functionName)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 3)
                {
                    var src = GetArgument<string>(functionName, context, 0, args);
                    var old = GetArgument<string>(functionName, context, 1, args);
                    var replacement = GetArgument<string>(functionName, context, 2, args);

                    return src.Replace(old, replacement);
                }

                throw new InvalidArgumentCountException(functionName, 3, 3, args.Count());
            };
        }

        /// <summary>
        /// Adds a function that will convert a string using the specified casing converter.
        /// 
        /// Therefor the string is split using the following split characters: ' ', '_', '-', '.'
        /// 
        /// The it is reconstructed separating the different paths with the provided separator and the provided lower, or
        /// uppercasing function is applied to the first character of each part.
        /// </summary>
        /// <param name="context">The expression context, where the function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <param name="source">The source string that should be converted.</param>
        /// <param name="separator">The separator that will connect the different parts.</param>
        /// <param name="firstIsLower">A value indicating whether the first character of the first part should be put to lowercaes (true) or uppercase (false).</param>
        /// <param name="restIsLower">A value indicating whether the first character of the second and following parts should be put to lowercaes (true) or uppercase (false).</param>
        /// <returns>The converted source string.</returns>
        private static void AddCaseFunction(NCalc.ExpressionContext context, string functionName, string separator, bool firstIsLower, bool restIsLower)
        {
            context.Functions[functionName] = (args) =>
            {
                if (args.Count() == 1)
                {
                    string src = GetArgument<string>(functionName, context, 0, args).ToLower();
                    return CaseConverter(src, separator, firstIsLower, restIsLower);
                }

                throw new InvalidArgumentCountException(functionName, 1, 1, args.Count());
            };
        }

        /// <summary>
        /// Converts a string using the specified casing converter.
        /// 
        /// Therefor the string is split using the following split characters: ' ', '_', '-', '.'
        /// 
        /// The it is reconstructed separating the different paths with the provided separator and the provided lower, or
        /// uppercasing function is applied to the first character of each part.
        /// </summary>
        /// <param name="source">The source string that should be converted.</param>
        /// <param name="separator">The separator that will connect the different parts.</param>
        /// <param name="firstIsLower">A value indicating whether the first character of the first part should be put to lowercaes (true) or uppercase (false).</param>
        /// <param name="restIsLower">A value indicating whether the first character of the second and following parts should be put to lowercaes (true) or uppercase (false).</param>
        /// <returns>The converted source string.</returns>
        private static string CaseConverter(string source, string separator, bool firstIsLower, bool restIsLower)
        {
            var parts = source.Split(new char[] { ' ', '_', '-', '.' });

            bool isFirst = true;
            StringBuilder result = new StringBuilder();
            char firstChar;

            foreach (var part in parts)
            {
                if (isFirst)
                {
                    firstChar = firstIsLower ? char.ToLower(part[0]) : char.ToUpper(part[0]);
                    isFirst = false;
                }
                else
                {
                    result.Append(separator);
                    firstChar = restIsLower ? char.ToLower(part[0]) : char.ToUpper(part[0]);
                }

                result.Append(firstChar);
                result.Append(part[1..]);
            }

            return result.ToString();
        }
    }
}
