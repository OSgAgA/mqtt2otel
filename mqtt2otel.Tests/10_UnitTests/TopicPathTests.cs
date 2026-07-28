using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._10_UnitTests
{
    public  class TopicPathTests
    {
        [Theory]
        [InlineData("this/is/a/test", "[0]", "this")]
        [InlineData("this/is/a/test", "[1]", "is")]
        [InlineData("this/is/a/test", "[4]", "")]
        [InlineData("this/is/a/test", "a", "test")]
        [InlineData("this/is/a/test", "a/", "test")]
        [InlineData("this/is/a/test", "nonExisting/", "")]
        [InlineData("this/is/a/test", "is/", "a")]
        [InlineData("this/is/a/test", "is/[0]", "a")]
        [InlineData("this/is/a/test", "is/[1]", "test")]
        [InlineData("this/is/is/test", "is", "is")]
        [InlineData("this/is/is/test", "[2]/is", "test")]
        [InlineData("this/is/is/test", "is/is", "test")]
        public void ShouldMatchTopicAndReturnExpectedResult(string topic, string pattern, string expected)
        {
            var topicPathParser = new TopicPathStrategy();
            var context = new ParsingContext(new List<Variable>(), string.Empty, topic);

            string result = topicPathParser.Parse<string>(pattern, context);

            Assert.Equal(expected, result);
        }
    }
}
