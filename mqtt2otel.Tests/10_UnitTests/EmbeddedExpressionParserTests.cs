using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
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
            var context = new Parser.ParsingContext(new List<Variable>(), payload: "", topic: "");

            string result = EmbeddedExpressionParser.Expand("This is a test", context, payloadParserMock.Object);

            Assert.Equal("This is a test", result);
        }

        [Fact]
        public void ShouldParseVariable()
        {
            var payloadParserMock = new Mock<IPayloadParser>();
            var context = new Parser.ParsingContext(new List<Variable>() { new Variable() { Key = "test", Value = "important test" } }, payload: "", topic: "");

            string result = EmbeddedExpressionParser.Expand("This is a $test", context, payloadParserMock.Object);

            Assert.Equal("This is a important test", result);
        }

        [Fact]
        public void ShouldParseSimpleEmbeddedExpressionUsingDefaultParser()
        {
            var context = new Parser.ParsingContext(new List<Variable>(), payload: "", topic: "");

            string result = EmbeddedExpressionParser.Expand("My lucky number is: $(84/2)", context);

            Assert.Equal("My lucky number is: 42", result);
        }

        [Theory]
        [InlineData("This is an $(embeddedExpression)", "embeddedExpression")]
        [InlineData("This is an $(embeddedExpression())", "embeddedExpression()")]
        [InlineData("This is an $(embeddedExpression(sub()))", "embeddedExpression(sub())")]
        [InlineData("This is an $(embeddedExpression(sub(')')))", "embeddedExpression(sub(')'))")]
        [InlineData("This is an $(embeddedExpression('sub()'))", "embeddedExpression('sub()')")]
        public void ShouldIdentifyCorrectEmbeddedExpressions(string input, string expectedExpression)
        {
            var payloadParserMock = new Mock<IPayloadParser>();
            var context = new Parser.ParsingContext(new List<Variable>(), payload: "", topic: "");

            EmbeddedExpressionParser.Expand(input, context, payloadParserMock.Object);

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
            var context = new Parser.ParsingContext(new List<Variable>(), payload: "", topic: "");

            EmbeddedExpressionParser.Expand(input, context, payloadParserMock.Object);

            payloadParserMock.Verify(parser => parser.Parse<string>(It.IsAny<string>(), It.IsAny<string>(), context), Times.Never);
        }
    }
}
