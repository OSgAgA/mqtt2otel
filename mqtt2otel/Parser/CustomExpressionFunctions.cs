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

        private static void AddDateTimeFunction(NCalc.ExpressionContext context, string functionName, Func<DateTime, int,  DateTime> func)
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
    }
}
