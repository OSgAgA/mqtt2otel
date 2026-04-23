using mqtt2otel.Helper;
using mqtt2otel.Parser;
using MQTTnet;
using NCalc;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// An abstract base class for defining parsers that will be able to add parsing strategies dynamically.
    /// </summary>
    /// <typeparam name="T">The type of the supported strategies.</typeparam>
    public abstract class StrategyParser<T> where T : IKeyObject
    {
        /// <summary>
        /// Gets or sets a map that maps a strategy name to the according strategy.
        /// </summary>
        protected Dictionary<string, T> NameStrategyMapping { get; set; } = new();

        /// <summary>
        /// Adds a new stratgy.
        /// </summary>
        /// <param name="strategy">The strategy to be added.</param>
        public void AddStrategy(T strategy)
        {
            this.NameStrategyMapping[strategy.Key] = strategy;
        }

        /// <summary>
        /// Automatically adds all types that derive from <see cref="T"/> as strategies.
        /// </summary>
        public void AutoDetectStrategies()
        {
            foreach (var strategy in this.DetectStrategies())
            {
                this.AddStrategy(strategy);
            }
        }

        /// <summary>
        /// Parses a payload by applying a NCalc expression.
        /// </summary>
        /// <typeparam name="TResult">The expected result type.</typeparam>
        /// <param name="name">The rule name to identify the settings in case of an error.</param>
        /// <param name="payload">The payload to be parsed.</param>
        /// <param name="expressionString">The NCalc expression that will be applied to the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed expression.</returns>
        /// <exception cref="ExpressionParsingException">Thrown if the expression could not be parsed.</exception>
        public async Task<TResult> ParseExpression<TResult>(string name, string payload, string expressionString, ParsingContext context)
        {
            try
            {
                var expression = new NCalc.AsyncExpression(expressionString);

                foreach (var strategyName in this.NameStrategyMapping.Keys)
                {
                    this.ApplyStrategy<TResult>(payload, expression, strategyName, context);
                }

                CustomExpressionFunctions.AddTo(expression);

                var result = await expression.EvaluateAsync() ?? throw new Exception();

                return TypeHelper.ConvertObject<TResult>(result);
            }
            catch (Exception ex)
            {
                throw new ExpressionParsingException(ex, name, expressionString);
            }
        }

        /// <summary>
        /// Applies a strategy to the payload.
        /// </summary>
        /// <typeparam name="TResult">The expected result type.</typeparam>
        /// <param name="payload">The payload that should be parsed.</param>
        /// <param name="expression">The NCalc expression to be applied.</param>
        /// <param name="strategyName">The name of the strategy that should be used for parsing the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <exception cref="InvalidArgumentCountException"></exception>
        private void ApplyStrategy<TResult>(string payload, AsyncExpression expression, string strategyName, ParsingContext context)
        {
            expression.Functions[strategyName] = async (args) =>
            {
                if (this.NameStrategyMapping.ContainsKey(strategyName))
                {
                    string? pattern = null;
                    string returnType = TypeHelper.GetTypeRepresentation(typeof(TResult));

                    if (args.Count() == 2)
                    {
                        returnType = await CustomExpressionFunctions.GetArgument<string>(strategyName, 0, args);
                        pattern = await CustomExpressionFunctions.GetArgument<string>(strategyName, 1, args);

                    }
                    else if (args.Count() == 1)
                    {
                        pattern = await CustomExpressionFunctions.GetArgument<string>(strategyName, 0, args);
                    }
                    else if (args.Count() == 0)
                    {
                        pattern = string.Empty;
                    }

                    if (pattern != null) return TypeHelper.CallMethodWithGenericType(this, returnType, nameof(this.ApplyStrategy), new object[] { this.NameStrategyMapping[strategyName], payload, pattern, context });
                }

                throw new InvalidArgumentCountException(strategyName, 0, 2, args.Count());
            };
        }

        /// <summary>
        /// An abstract method that needs to be overridden in derived classes. Will define how a strategy of the given type can be applied.
        /// </summary>
        /// <typeparam name="TResult">The expected result type.</typeparam>
        /// <param name="strategy">The strategy that should be applied.</param>
        /// <param name="payload">The payload that should be processed by the strategy.</param>
        /// <param name="pattern">The pattern describing how the payload should be processed.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parse result.</returns>
        protected abstract TResult ApplyStrategy<TResult>(T strategy, string payload, string pattern, ParsingContext context);

        /// <summary>
        /// Detects all types that derive from <see cref="T"/>. These can then be added as strategies to the instance.
        /// </summary>
        /// <returns>All detected strategies.</returns>
        private List<T> DetectStrategies()
        {
            var strategyType = typeof(T);

            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    // Some assemblies may fail to load types
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    strategyType.IsAssignableFrom(t) &&   // implements interface
                    t.IsClass &&                          // is a class
                    !t.IsAbstract &&                      // not abstract
                    t.GetConstructor(Type.EmptyTypes) != null) // has public new()
                .Select(t => (T)Activator.CreateInstance(t)!)
                .ToList();
        }

    }
}
