using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._10_UnitTests
{
    public class CustomFunctionTests
    {
        [Theory]
        [InlineData("TemperatureInC", "ToKebabCase", "temperature-in-c")]
        [InlineData("Temperature.InC", "ToKebabCase", "temperature-in-c")]
        [InlineData("Temperature in-C", "ToKebabCase", "temperature-in-c")]
        [InlineData("Temperature_InC", "ToKebabCase", "temperature-in-c")]
        [InlineData("Temperature_InC", "ToCamelCase", "temperatureInC")]
        [InlineData("Temperature_InC", "ToPascalCase", "TemperatureInC")]
        [InlineData("Temperature_In_C", "ToLower", "temperature_in_c")]
        [InlineData("Temperature_In_C", "ToUpper", "TEMPERATURE_IN_C")]
        [InlineData("Temperature_InC", "ToTrainCase", "Temperature-In-C")]
        [InlineData("Temperature_InC", "ToSnakeCase", "temperature_in_c")]
        public async Task ShouldConvertInputToProvidedCase(string input, string function, string expectedResult)
        {
            var parser = new PayloadParser();

            var context = new ParsingContext(new List<Variable>(), new MqttMessage());
            var expr = $"{function}('{input}')";
            var result = parser.ParseExpression<string>("Test", expr, context);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(3, "ToDouble", 3.00d, typeof(double))]
        [InlineData(3.0f, "ToDouble", 3.00d, typeof(double))]
        [InlineData("3", "ToDouble", 3.00d, typeof(double))]
        [InlineData(3L, "ToDouble", 3.00d, typeof(double))]
        [InlineData(3.00d, "ToDouble", 3.00d, typeof(double))]
        [InlineData(3, "ToFloat", 3.00f, typeof(float))]
        [InlineData(3.0f, "ToFloat", 3.00f, typeof(float))]
        [InlineData("3", "ToFloat", 3.00f, typeof(float))]
        [InlineData(3L, "ToFloat", 3.00f, typeof(float))]
        [InlineData(3.00d, "ToFloat", 3.00f, typeof(float))]
        [InlineData(3, "ToInt", 3, typeof(int))]
        [InlineData(3.0f, "ToInt", 3, typeof(int))]
        [InlineData("3", "ToInt", 3, typeof(int))]
        [InlineData(3L, "ToInt", 3, typeof(int))]
        [InlineData(3.00d, "ToInt", 3, typeof(int))]
        [InlineData(3, "ToLong", 3L, typeof(long))]
        [InlineData(3.0f, "ToLong", 3L, typeof(long))]
        [InlineData("3", "ToLong", 3L, typeof(long))]
        [InlineData(3L, "ToLong", 3L, typeof(long))]
        [InlineData(3.00d, "ToLong", 3L, typeof(long))]
        [InlineData(3, "ToString", "3", typeof(string))]
        [InlineData(3.0f, "ToString", "3", typeof(string))]
        [InlineData("3", "ToString", "3", typeof(string))]
        [InlineData(3L, "ToString", "3", typeof(string))]
        [InlineData(3.00d, "ToString", "3", typeof(string))]
        public async Task ShouldConvertInputToProvidedType(object input, string function, object expectedResult, Type expectedType)
        {
            var parser = new PayloadParser();

            var context = new ParsingContext(new List<Variable>(), new MqttMessage());

            string expr = string.Empty;
            if ((input is string))
            {
                expr = $"{function}('{input}')";
            }
            else
            {
                expr = $"{function}({input})";
            }

            var result = parser.ParseExpression("Test", expr, context);

            Assert.Equal(expectedResult, result);
            Assert.Equal(result.GetType(), expectedType);
        }

        [Theory]
        [InlineData("   just some text    ", "Trim", "just some text")]
        [InlineData("   just some text    ", "TrimStart", "just some text    ")]
        [InlineData("   just some text    ", "TrimEnd", "   just some text")]
        public async Task ShouldTrimInput(string input, string function, string expectedResult)
        {
            var parser = new PayloadParser();

            var context = new ParsingContext(new List<Variable>(), new MqttMessage());
            var expr = $"{function}('{input}')";
            var result = parser.ParseExpression<string>("Test", expr, context);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("This is a long text with a lot of content", "StartsWith", "This", true)]
        [InlineData("This is a long text with a lot of content", "StartsWith", "Not this", false)]
        [InlineData("This is a long text with a lot of content", "EndsWith", "content", true)]
        [InlineData("This is a long text with a lot of content", "EndsWith", "not content", false)]
        [InlineData("This is a long text with a lot of content", "Contains", "a lot", true)]
        [InlineData("This is a long text with a lot of content", "Contains", "a lot more", false)]
        [InlineData("This is a long text with a lot of content", "MatchesWildcard", "*a lot*", true)]
        [InlineData("This is a long text with a lot of content", "MatchesWildcard", "*a l?t*", true)]
        [InlineData("This is a long text with a lot of content", "MatchesWildcard", "*a lot", false)]
        [InlineData("This is a long text with a lot of content", "MatchesRegEx", ".*a lot.*", true)]
        [InlineData("This is a long text with a lot of content", "MatchesRegEx", ".*a l.t.*", true)]
        [InlineData("This is a long text with a lot of content", "MatchesRegEx", ".*a lott", false)]

        public async Task ShouldFindTextInInput(string input, string function, string parameter, bool expectedResult)
        {
            var parser = new PayloadParser();

            var context = new ParsingContext(new List<Variable>(), new MqttMessage());
            var expr = $"{function}('{input}', '{parameter}')";
            var result = parser.ParseExpression("Test", expr, context);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("This is a long text with a lot of content", "long", "short", "This is a short text with a lot of content")]
        [InlineData("This is a long text with a lot of content", "i", "u", "Thus us a long text wuth a lot of content")]
        public async Task ShouldReplaceTextInInput(string input, string replace, string replaceWith, string expectedResult)
        {
            var parser = new PayloadParser();

            var context = new ParsingContext(new List<Variable>(), new MqttMessage());
            var expr = $"Replace('{input}', '{replace}', '{replaceWith}')";
            var result = parser.ParseExpression("Test", expr, context);

            Assert.Equal(expectedResult, result);
        }
    }
}
