using mqtt2otel.Server.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._30_SystemTests
{
    /// <summary>
    /// This fixture is handling the setup and cleanup of the json test cases.
    /// </summary>
    public class JsonTestsFixture : IAsyncLifetime
    {
        /// <summary>
        /// Only one helper should be used for all test cases, otherwise memory consumption goes up, even after a clean dispose!!!
        /// 
        /// Should be static, as class is reconstructed for each test case.
        /// </summary>
        public static MqttTestHelper? mqttHelper;

        /// <summary>
        /// Called befoe any json test case is startet. Ensures, that the mqtt server is available.
        /// </summary>
        public async ValueTask InitializeAsync()
        {
            if (mqttHelper == null) mqttHelper = new MqttTestHelper();

            await mqttHelper.EnsureServerIsStarted();
        }

        /// <summary>
        /// Called when all json test cases have completed. Disposes the mqttHelper.
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (mqttHelper != null) mqttHelper.Dispose();
        }
    }
}
