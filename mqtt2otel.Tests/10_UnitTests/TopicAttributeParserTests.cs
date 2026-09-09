using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._10_UnitTests
{
    public class TopicAttributeParserTests
    {
        [Theory]
        [InlineData("1234", "DeviceId")]
        [InlineData("tele/sensor/1234", "_/_/DeviceId")]
        [InlineData("tele/sensor/1234/test", "_/_/DeviceId/_")]
        [InlineData("tele/sensor/1234", "_/_/DeviceId/%")]
        [InlineData("tele/sensor/1234/ignore", "_/_/DeviceId/%")]
        [InlineData("tele/sensor/1234/ignore/ignore/ignore", "_/_/DeviceId/%")]
        public void ShouldMatchValidPathsToDeviceId(string topic, string pattern)
        {
            var attributes = TopicAttributeParser.Parse(topic, pattern);

            Assert.Single(attributes);

            var attribute = attributes.First();
            Assert.Equal("DeviceId", attribute.Key);
            Assert.Equal("1234", attribute.Value);
        }

        [Theory]
        [InlineData("", "DeviceId")]
        [InlineData("tele", "_/DeviceId")]
        [InlineData("tele", "_/_/DeviceId")]
        [InlineData("tele/sensor/1234", "_/%/DeviceId")]
        public void ShouldReturnEmptyResult(string topic, string pattern)
        {
            var attributes = TopicAttributeParser.Parse(topic, pattern);

            Assert.Empty(attributes);
        }
    }
}
