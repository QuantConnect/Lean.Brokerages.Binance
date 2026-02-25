/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Brokerages.Binance.Extensions;

namespace QuantConnect.Brokerages.Binance.Messages
{
    public class SubscribeSignature
    {
        /// <summary>
        /// JSON serialization settings used for WebSocket requests.
        /// Uses camelCase property names to match API requirements.
        /// </summary>
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Arbitrary ID used to match responses to requests
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Request method name
        /// </summary>
        public string Method => "userDataStream.subscribe.signature";

        /// <summary>
        /// Request parameters. May be omitted if there are no parameters
        /// </summary>
        public object Params { get; }

        /// <summary>
        /// Create instance of the subscribe signature request with the necessary authentication parameters.
        /// </summary>
        /// <param name="apiKey">The api key.</param>
        /// <param name="apiSecret">The api secret.</param>
        public SubscribeSignature(string apiKey, string apiSecret)
        {
            var timestamp = BinanceExtensions.GetNonce();
            var signature = BinanceExtensions.GetAuthenticationToken(apiSecret, payload: $"apiKey={apiKey}&timestamp={timestamp}");

            Id = Guid.NewGuid().ToString();
            Params = new
            {
                apiKey,
                signature,
                timestamp
            };
        }

        /// <summary>
        /// Serializes the request to JSON for WebSocket transmission.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, _jsonSettings);
        }
    }
}
