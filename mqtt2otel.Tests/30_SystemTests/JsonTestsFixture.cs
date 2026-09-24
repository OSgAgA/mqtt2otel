using mqtt2otel.Server.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._30_SystemTests
{
    public class JsonTestsFixture : IAsyncLifetime
    {
        public static MqttTestHelper? mqttHelper;

        public async ValueTask InitializeAsync()
        {
            if (mqttHelper == null) mqttHelper = new MqttTestHelper();

            await mqttHelper.EnsureServerIsStarted();
        }

        public async ValueTask DisposeAsync()
        {
            if (mqttHelper != null) mqttHelper.Dispose();
        }
    }
}
