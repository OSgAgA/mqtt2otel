using NCalc;
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
        /// <param name="expression">The expression to which the functions should be added.,</param>
        public static void AddTo(AsyncExpression expression)
        {
            AddParseDateTimeFunction(expression, "ParseDateTime");

            AddDateTimeFunction(expression, "AddDays", (date, increment) => date.AddDays(increment));
            AddDateTimeFunction(expression, "AddMonths", (date, increment) => date.AddMonths(increment));
            AddDateTimeFunction(expression, "AddYears", (date, increment) => date.AddYears(increment));
            AddDateTimeFunction(expression, "AddHours", (date, increment) => date.AddHours(increment));
            AddDateTimeFunction(expression, "AddMinutes", (date, increment) => date.AddMinutes(increment));
            AddDateTimeFunction(expression, "AddSeconds", (date, increment) => date.AddSeconds(increment));

            AddConvertTimeZoneFunction(expression, "ConvertTimezone");
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
        public static async Task<TResult> GetArgument<TResult>(string functionName, int index, AsyncExpressionFunctionData args)
        {
            try
            {
                return (TResult)(await args[index].EvaluateAsync() ?? throw new Exception());
            }
            catch
            {
                throw new ArgumentTypeException(functionName, index, typeof(TResult), args[index]?.LogicalExpression?.ToString() ?? string.Empty);
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
        /// <param name="expression">The expresssion to which this function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 3 arguments.</exception>
        private static void AddConvertTimeZoneFunction(AsyncExpression expression, string functionName)
        {
            expression.Functions[functionName] = async (args) =>
            {
                if (args.Count() == 3)
                {
                    var date = await GetArgument<DateTime>(functionName, 0, args);
                    var sourceTimezone = await GetArgument<string>(functionName, 1, args);
                    var destTimezone = await GetArgument<string>(functionName, 2, args);

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
        /// <param name="expression">The expresssion to which this function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not 1-2 arguments.</exception>
        /// <exception cref="ParsingFailedException">Thrown if the string could not be parsed to a DateTime.</exception>
        private static void AddParseDateTimeFunction(AsyncExpression expression, string functionName)
        {
            expression.Functions[functionName] = async (args) =>
            {
                if (args.Count() == 1)
                {
                    var dateAsString = await GetArgument<string>(functionName, 0, args);

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
                    var dateAsString = await GetArgument<string>(functionName, 0, args);
                    var format = await GetArgument<string>(functionName, 1, args);

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
        /// <param name="expression">The expresssion to which this function should be added.</param>
        /// <param name="functionName">The function name.</param>
        /// <param name="func">The function that should be called on the date argument.</param>
        /// <exception cref="InvalidArgumentCountException">Thrown if the argument has not exactly 2 arguments.</exception>

        private static void AddDateTimeFunction(AsyncExpression expression, string functionName, Func<DateTime, int,  DateTime> func)
        {
            expression.Functions[functionName] = async (args) =>
            {
                if (args.Count() == 2)
                {
                    var date = await GetArgument<DateTime>(functionName, 0, args);
                    var inc = await GetArgument<int>(functionName, 1, args);

                    return func(date, inc);
                }

                throw new InvalidArgumentCountException(functionName, 2, 2, args.Count());
            };
        }
    }
}
