using mqtt2otel.Parser;
using System.Linq.Expressions;

namespace mqtt2otel.Tests
{
    public class PayloadParserTests
    {
        /// <summary>
        /// Provides an empty parsing context for testing purposes.
        /// </summary>
        private static ParsingContext _emptyContext = new ParsingContext(new List<Variable>());

        [Fact]
        public async Task ShouldProcessConstantValue()
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            var result = await parser.ParseExpression<int>("Test", "123", "CONST('42')", _emptyContext);

            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData("'$.Test.ValueA'", 42)]
        [InlineData("'$.Test.ValueB'", 10)]
        [InlineData("'$.Test.ValueC.ValueD'", 11)]
        [InlineData("'$.ValueE'", 23)]
        public async Task ShouldProcessAJsonPath(string pattern, int expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "{ \"Test\": { ValueA: 42, ValueB: 10, ValueC: { ValueD: 11 } }, ValueE: 23 }";
            var result = await parser.ParseExpression<int>("Test", payload, $"JSONPATH({pattern})", _emptyContext);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ShouldUseAnExplicitTypeForAParsingStrategy()
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "{ \"Test\": { ValueA: 42.2, ValueB: 10, ValueC: { ValueD: 11 } }, ValueE: 23 }";
            var result = await parser.ParseExpression<float>("Test", payload, "JSONPATH('int', '$.Test.ValueA')", _emptyContext);

            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ShouldReturnThePayloadWhenTextStrategyIsApplied()
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "{ \"Test\": { ValueA: 42 } }";
            var result = await parser.ParseExpression<string>("Test", payload, "PAYLOAD()", _emptyContext);

            Assert.Equal(payload, result);
        }

        [Theory]
        [InlineData("1.0 / 2.0", 0.5)]
        [InlineData("4*(2+3)", 20)]
        [InlineData("4*2+3", 11)]
        [InlineData("Sqrt(9)", 3)]
        public async Task ShoulPerformBasicMathematicalOperations(string expression, float expectedResult)
        {
            var parser = new PayloadParser();

            string payload = string.Empty;
            var result = await parser.ParseExpression<float>("Test", payload, expression, _emptyContext);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("ParseDateTime('2022-01-16')", "2022-01-16")]
        [InlineData("ParseDateTime('2022-01-16 11:23:47')", "2022-01-16 11:23:47")]
        [InlineData("ParseDateTime('2022-01.16', 'yyyy-MM.dd')", "2022-01-16")]
        public async Task ShouldReturnTheParsedDateTimeValue(string expression, string expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "{ \"Test\": { ValueA: 42 } }";
            var result = await parser.ParseExpression<DateTime>("Test", payload, expression, _emptyContext);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("AddDays(ParseDateTime('2022-01-16'), 1)", "2022-01-17")]
        [InlineData("AddMonths(ParseDateTime('2022-01-16'), 1)", "2022-02-16")]
        [InlineData("AddYears(ParseDateTime('2022-01-16'), 1)", "2023-01-16")]
        [InlineData("AddHours(ParseDateTime('2022-01-16 14:34:15'), 1)", "2022-01-16 15:34:15")]
        [InlineData("AddMinutes(ParseDateTime('2022-01-16 14:34:15'), 1)", "2022-01-16 14:35:15")]
        [InlineData("AddSeconds(ParseDateTime('2022-01-16 14:34:15'), 1)", "2022-01-16 14:34:16")]
        [InlineData("ConvertTimezone(ParseDateTime('2024-12-31 18:00:00'), 'America/New_York', 'Asia/Tokyo')", "2025-01-01 08:00:00")]
        public async Task ShouldEnsureThatDateTimeFunctionsWorkAsExpected(string expression, string expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "{ \"Test\": { ValueA: 42 } }";
            var result = await parser.ParseExpression<DateTime>("Test", payload, expression, _emptyContext);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("ParseDateTime()", "ParseDateTime", 1, 2, 0)]
        [InlineData("ParseDateTime(1,2,3)", "ParseDateTime", 1, 2, 3)]
        [InlineData("AddDays()", "AddDays", 2, 2, 0)]
        [InlineData("AddDays(1,2,3)", "AddDays", 2, 2, 3)]
        [InlineData("AddMonths()", "AddMonths", 2, 2, 0)]
        [InlineData("AddMonths(1,2,3)", "AddMonths", 2, 2, 3)]
        [InlineData("AddYears()", "AddYears", 2, 2, 0)]
        [InlineData("AddYears(1,2,3)", "AddYears", 2, 2, 3)]
        [InlineData("AddHours()", "AddHours", 2, 2, 0)]
        [InlineData("AddHours(1,2,3)", "AddHours", 2, 2, 3)]
        [InlineData("AddMinutes()", "AddMinutes", 2, 2, 0)]
        [InlineData("AddMinutes(1,2,3)", "AddMinutes", 2, 2, 3)]
        [InlineData("AddSeconds()", "AddSeconds", 2, 2, 0)]
        [InlineData("AddSeconds(1,2,3)", "AddSeconds", 2, 2, 3)]
        public async Task ShouldThrowInvalidArgumentCountException(string expression, string functionName, int expectedMinCount, int expectedMaxCount, int actualCount)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = string.Empty;
            var exception = await Assert.ThrowsAsync<ExpressionParsingException>( async () => await parser.ParseExpression<DateTime>("Test", payload, expression, _emptyContext) );

            Assert.NotNull(exception);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(typeof(InvalidArgumentCountException), exception?.InnerException?.GetType());

            Assert.NotNull(exception);

            var innerException = (InvalidArgumentCountException)exception.InnerException;

            Assert.Equal(functionName, innerException.FunctionName);
            Assert.Equal(expectedMinCount, innerException.ExpectedArgumentCountMin);
            Assert.Equal(expectedMaxCount, innerException.ExpectedArgumentCountMax);
            Assert.Equal(actualCount, innerException.ActualArgumentCount);
        }

        [Theory]
        [InlineData("AddDays(ParseDateTime('2022-01-16'), 'hello world')", "AddDays", 1, typeof(int), "'hello world'")]
        [InlineData("AddMonths(ParseDateTime('2022-01-16'), 'hello world')", "AddMonths", 1, typeof(int), "'hello world'")]
        [InlineData("AddYears(ParseDateTime('2022-01-16'), 'hello world')", "AddYears", 1, typeof(int), "'hello world'")]
        [InlineData("AddHours(ParseDateTime('2022-01-16'), 'hello world')", "AddHours", 1, typeof(int), "'hello world'")]
        [InlineData("AddMinutes(ParseDateTime('2022-01-16'), 'hello world')", "AddMinutes", 1, typeof(int), "'hello world'")]
        [InlineData("AddSeconds(ParseDateTime('2022-01-16'), 'hello world')", "AddSeconds", 1, typeof(int), "'hello world'")]
        public async Task ShouldThrowArgumentTypeException(string expression, string functionName, int argumentIndex, Type expectedType, string actualArgument)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = string.Empty;
            var exception = await Assert.ThrowsAsync<ExpressionParsingException>(async () => await parser.ParseExpression<DateTime>("Test", payload, expression, _emptyContext));

            Assert.NotNull(exception);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(typeof(ArgumentTypeException), exception?.InnerException?.GetType());

            Assert.NotNull(exception);

            var innerException = (ArgumentTypeException) exception.InnerException;
            Assert.Equal(functionName, innerException.FunctionName);
            Assert.Equal(expectedType, innerException.ExpectedArgumentType);
            Assert.Equal(argumentIndex, innerException.ArgumentIndex);
            Assert.Equal(actualArgument, innerException.ActualArgument);
        }

        [Theory]
        [InlineData("ParseDateTime('42')", "ParseDateTime", 0, typeof(DateTime), "42")]
        [InlineData("ParseDateTime('42', 'dd-MM-yyy')", "ParseDateTime", 0, typeof(DateTime), "42 [format: dd-MM-yyy]")]
        public async Task ShouldThrowParsingFailedException(string expression, string functionName, int argumentIndex, Type expectedType, string actualArgument)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = string.Empty;
            var exception = await Assert.ThrowsAsync<ExpressionParsingException>(async () => await parser.ParseExpression<DateTime>("Test", payload, expression, _emptyContext));

            Assert.NotNull(exception);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(typeof(ParsingFailedException), exception?.InnerException?.GetType());

            Assert.NotNull(exception);

            var innerException = (ParsingFailedException)exception.InnerException;
            Assert.Equal(functionName, innerException.FunctionName);
            Assert.Equal(expectedType, innerException.ExpectedTargetType);
            Assert.Equal(argumentIndex, innerException.ArgumentIndex);
            Assert.Equal(actualArgument, innerException.ActualArgument);
        }

        [Theory]
        [InlineData("REGEX('[a-zA-Z]+_[0-9]+')", "aA_42")]
        [InlineData("REGEX('[a-zA-Z]')", "a")]
        [InlineData("REGEX('_[0-9]+')", "_42")]
        [InlineData("REGEX('.*_[0-9]')", "  1232134 aA_4")]
        [InlineData("REGEX('[0-9]+')", "1232134")]
        public async Task ShouldProcessRegExStrategyWithStringResult(string expression, string expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "  1232134 aA_42 239847";
            var result = await parser.ParseExpression<string>("test", payload, expression, _emptyContext);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("REGEX('[0-9]+')", 1232134)]
        [InlineData("REGEX('_([0-9]+)')", 42)] // Use only value in first matched group
        public async Task ShouldProcessRegExStrategyWithIntResult(string expression, int expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "  1232134 aA_42 239847";
            var result = await parser.ParseExpression<int>("test", payload, expression, _emptyContext);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("REGEX('[0-9]{4}-[0-9]{2}-[0-9]{2}')", "2022-02-16")]
        [InlineData("REGEX('--(.*)--')", "2022-02-16")] // Use only value in first matched group
        public async Task ShouldProcessRegExStrategyWithDateTimeResult(string expression, string expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = " --2022-02-16--";
            var result = await parser.ParseExpression<DateTime>("test", payload, expression, _emptyContext);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("XMLPATH('/root/child[1]')", 42)]
        [InlineData("XMLPATH('/root/child[2]')", 11)]
        public async Task ShouldProcessXmlPathStrategyWithIntResult(string expression, int expectedResult)
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = " <root><child>42</child><child>11</child></root>";
            var result = await parser.ParseExpression<int>("test", payload, expression, _emptyContext);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ShouldProcessVariableStrategyAndReturnVariableValueAsString()
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "";
            string expectedResult = "world";
            var context = new ParsingContext(new List<Variable>() { new Variable() { Key = "hello", Value = expectedResult } });
            var result = await parser.ParseExpression<string>("test", payload, "VAR('hello')", context);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ShouldProcessVariableStrategyAndReturnVariableValueAsInt()
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "";
            int expectedResult = 42;
            var context = new ParsingContext(new List<Variable>() { new Variable() { Key = "answer", Value = expectedResult } });
            var result = await parser.ParseExpression<int>("test", payload, "VAR('answer')", context);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ShouldTryToProcessNonExistingVariableStrategyAndThrow()
        {
            var parser = new PayloadParser();

            parser.AutoDetectStrategies();

            string payload = "";
            int expectedResult = 42;
            var context = new ParsingContext(new List<Variable>() { new Variable() { Key = "answer", Value = expectedResult } });
            var exception = await Assert.ThrowsAsync<ExpressionParsingException>( async () =>  await parser.ParseExpression<int>("test", payload, "VAR('question')", context));
        }
    }
}
