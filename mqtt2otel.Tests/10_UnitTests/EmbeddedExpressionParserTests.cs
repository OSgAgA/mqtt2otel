using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Sdk;

namespace mqtt2otel.Tests._10_UnitTests
{
    public class EmbeddedExpressionParserTests
    {
        [Fact]
        public void ShouldParsePlainText()
        {
            var payloadParserMock = new Mock<IPayloadParser>();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParserMock.Object);

            var context = new Parser.ParsingContext(new List<Variable>(), new MqttMessage());

            string result = embeddedExpressionParser.Expand("This is a test", context);

            Assert.Equal("This is a test", result);
        }

        [Fact]
        public void ShouldParseVariable()
        {
            var payloadParserMock = new Mock<IPayloadParser>();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParserMock.Object);
            var context = new Parser.ParsingContext(new List<Variable>() { new Variable() { Key = "test", Value = "important test" } }, new MqttMessage());

            string result = embeddedExpressionParser.Expand("This is a $test", context);

            Assert.Equal("This is a important test", result);
        }

        [Fact]
        public void ShouldParseSimpleEmbeddedExpressionUsingDefaultParser()
        {
            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            var context = new Parser.ParsingContext(new List<Variable>(), new MqttMessage());

            string result = embeddedExpressionParser.Expand("My lucky number is: $(84/2)", context);

            Assert.Equal("My lucky number is: 42", result);
        }

        [Theory]
        [InlineData("This is an $(embeddedExpression)", "embeddedExpression")]
        [InlineData("This is an $(embeddedExpression())", "embeddedExpression()")]
        [InlineData("This is an $(embeddedExpression(sub()))", "embeddedExpression(sub())")]
        [InlineData("This is an $(embeddedExpression(sub(')')))", "embeddedExpression(sub(')'))")]
        [InlineData("This is an $(embeddedExpression('sub()'))", "embeddedExpression('sub()')")]
        [InlineData("$('5' + '1')", "'5' + '1'")]
        public void ShouldIdentifyCorrectEmbeddedExpressions(string input, string expectedExpression)
        {
            var payloadParserMock = new Mock<IPayloadParser>();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParserMock.Object);
            var context = new Parser.ParsingContext(new List<Variable>(), new MqttMessage());

            embeddedExpressionParser.Expand(input, context);

            payloadParserMock.Verify( parser => parser.Parse<string>(It.IsAny<string>(), expectedExpression, context));
        }

        [Theory]
        [InlineData("This is an $(incorrectEmbeddedExpression")]
        [InlineData("This is an $(incorrectEmbeddedExpression()")]
        [InlineData("This is an $(incorrectEmbeddedExpression(')')")]
        [InlineData("This is an $(incorrectEmbeddedExpression(sub())")]
        public void ShouldIdentifyInCorrectEmbeddedExpressions(string input)
        {
            var payloadParserMock = new Mock<IPayloadParser>();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParserMock.Object);
            var context = new Parser.ParsingContext(new List<Variable>(), new MqttMessage());

            embeddedExpressionParser.Expand(input, context);

            payloadParserMock.Verify(parser => parser.Parse<string>(It.IsAny<string>(), It.IsAny<string>(), context), Times.Never);
        }
    }
}
