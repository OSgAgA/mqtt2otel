using mqtt2otel.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace mqtt2otel.Provider
{
    /// <summary>
    /// A fake trace id provider that will provide trace and span ids in a deterministic way.
    /// </summary>
    public class FakeTraceIdProvider : ITraceIdProvider
    {
        /// <summary>
        /// Contains the list of all supported spans in the order in which they will be delivered..
        /// </summary>
        private string[] spanIds = new string[]
        {
            "8ad9cd275e9262b9", "ba74db659763c09f", "a1589e0ad0dee6ac", "a5ab7b5b13bce3de", "954c519c8dc17a1b", "7b3bb06dd6297503", "e8a571f542cff5e3", "b66e19bb09235aed", "d3ed2ab7091c77d2", "6ac95b092f3f65ae",
            "d73d81ce16a78d6b", "4c93ae562119d619", "0ca512b41dffc263", "9a565dd72b826a15", "358193bfe4e58e36", "e37ceb08b1a52b4a", "f75a21ce15d31008", "9dbe4cfed1a6e061", "0a4c7b4ece5c7e92", "cabe4e95b2970a4d",
            "7017ff8bb47bf323", "03b24e7f2a9c4686", "dbc3c57bce25cb62", "126facd6f1b59a34", "2e79733fbb3eb6da", "9cabe353934ad6ce", "438a0ce508fa2e76", "818c1419863ccf8c", "2a41b700edd8063c", "f06cc245c1559b64",
            "062b633ee16d0289", "7ba68bd11bcbed10", "2bf6c5d40803bb71", "fcaf0f9259f07d0b", "c5742cd95219bac6", "25ea813bf4f4f9d8", "553688276f21178c", "3a280e6f57bef5e9", "77dd6b3fda874440", "c672efa91cf4e31e",
            "6ecb1db5eee41f2b", "6bb4a45e8d2afdfe", "94f69b1232dbecc3", "10654b52878067da", "a4b4fba9aa5899ff", "3ffef10d0a13e600", "90dbd3c7d42b98ad", "5a2ce829d2db8c6d", "d347636a2472de04", "c431e968b32012ab"
        };

        /// <summary>
        /// Contains the list of all supported traces in the order in which they will be delivered..
        /// </summary>
        private string[] traceIds = new string[]
        {
            "dc60fcb9f59f80b41180ada8aaa3de36", "15b122b91701407734fd2fe19b66e4ac", "145fcce57bdedf99c500859790298775", "244d5459f93ce26a5099e5d5b242f37c", "34e293ec8dc65cdabe0a0c6ef8fa37e4",
            "7e28356e2444641040e6c82428d840ab", "d488ea411675df1a69a6adfa1002eb1e", "c71e6cf973b57b74e8e640e240f58ba6", "981bca15a7f40559e822d6bc38aabec7", "7970442e7e296c1d3bf2692351dbcf5c",
            "13aa368637a401da447bfc83c70e63b0", "4789cf861cd15081979173cd946a07ae", "5e0c4df5b894c8d00252eca748548d54", "fd4f502d196b8341c147c7e513368065", "548246fcb5e172c9e88d657856143308",
            "de2c28eaeb92f6f894fdd1829704db6d", "e1327472604911a7b141a223b1457bf4", "9480be0ab8bcb57a0a26754979517059", "6688f1517987faeeac2331a7ccfd0c03", "9865f6399c9f1296d33ff6614f4fc9dd",
            "2808cf00b8fce161c146009021c9ecab", "278a48f06b3c35095230d00ce08b7093", "ddd0abab51488c671aed0f2693f2a396", "4752915a09014b82839cb74795c7f28d", "380f1c8823bf842c7f0c957c995ef585",
            "4c84c1fd86c94b6374e8e817ef7a86c4", "e240e85e1ae7fd75db29012897efd127", "0649bb7bdc522e30fad84e88be7e3b40", "dfb1b58e0ea009f902f568124596a3b6", "92ae4923a0a89795564201662b28f867",
            "d096c231f591a72685ea16e8659329c9", "577a96881aad8e5862e9feee7c1e91ef", "790e4a9e6a39e527231fe7fb249c902f", "c3c8f13e84db0f7b72dad1159115ae20", "54b5ec24701302ab834d9dd0ca30d710",
            "b5e8129c166b93a1258e26d0db2b308e", "142cd019a58a984945f71f3f074f8d42", "f165ce7a6398ef178dda2567df5f07a6", "795d62f067c25defc11f0f9e9cbfa8b1", "8745d66007b6ee4f46d09fa6da1c3c1f",
            "7e696ff880882004fa6ab476f76660a5", "4b32119ec2a7e93ed23665ffa893b0a1", "d58b1471a52f1168cb51b62d9249d8b4", "f6e70de5dd54895e7cebb148f27db6ad", "701f78a14d28c677b43dba5068f6cf51",
            "bbb1898232fcf74005cf67e257f25547", "45f201cb979d7a86e218a809cea15f3b", "42ea30c3300aee40c9c8977b5b4a4c6b", "a19b0e54eeaf6c84a6df7b704d1b2251", "94f51fc3f9ff04183c0eaf2370a7a596"
        };

        /// <summary>
        /// The index of the current span id.
        /// </summary>
        private int spanIndex = -1;

        /// <summary>
        /// The index of the current trace id.
        /// </summary>
        private int traceIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderState"/> class.
        /// </summary>
        /// <param name="state">The initial provider state, or null if default state should be used.</param>
        public FakeTraceIdProvider(ProviderState? state)
        {
            if (state == null || state.ForceReset) return;

            this.spanIndex = state.GetValue<int>(0);
            this.traceIndex = state.GetValue<int>(1);
        }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        /// <returns>The current provider state.</returns>
        public ProviderState GetState()
        {
            return ProviderState.Generate(new object[] { this.spanIndex, this.traceIndex });
        }

        /// <summary>
        /// Resets the provider.
        /// </summary>
        public void Reset()
        {
            spanIndex = -1;
            traceIndex = -1;
        }

        /// <summary>
        /// Creates a new span id.
        /// </summary>
        /// <returns>The newly created id.</returns>
        public string CreateSpanId()
        {
            spanIndex++;

            if (spanIndex >= 50) spanIndex = 0;

            return spanIds[spanIndex];
        }

        /// <summary>
        /// Creates a new trace id.
        /// </summary>
        /// <returns>The newly created id.</returns>
        public string CreateTraceId()
        {
            traceIndex++;

            if (traceIndex >= 50) traceIndex = 0;

            return traceIds[traceIndex];
        }

        /// <summary>
        /// Creates new fake ids if needed.
        /// </summary>
        private void CreateFakeIdLists()
        {
            var traceIds = this.CreateList(() => ActivityTraceId.CreateRandom().ToString());
            var spanIds = this.CreateList(() => ActivitySpanId.CreateRandom().ToString());
        }

        /// <summary>
        /// Creates the string representation of a list of fake ids.
        /// </summary>
        /// <param name="createId">The function used to create a new fake id.</param>
        /// <returns>The created string representation of the list.</returns>
        private string CreateList(Func<string> createId)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{ ");

            for (int i = 0; i < 50; i++)
            {
                sb.Append('"');
                sb.Append(createId());
                sb.Append('"');
                sb.Append(", ");
            }

            sb.Append(" }");

            return sb.ToString();
        }
    }
}
