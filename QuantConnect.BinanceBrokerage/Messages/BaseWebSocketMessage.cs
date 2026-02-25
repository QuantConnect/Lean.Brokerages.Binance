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

namespace QuantConnect.Brokerages.Binance.Messages
{
    /// <summary>
    /// The base class for WebSocket messages sent to Binance API. Contains common properties and JSON serialization logic.
    /// </summary>
    public class BaseWebSocketMessage
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
        public string Method { get; }

        /// <summary>
        /// Initializes a new instance of the BaseWebSocketMessage class with the specified method name.
        /// </summary>
        /// <param name="method">Method name for ws message</param>
        protected BaseWebSocketMessage(string method)
        {
            Id = Guid.NewGuid().ToString();
            Method = method;
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
