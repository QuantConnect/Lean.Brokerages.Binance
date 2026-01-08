/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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

using Fasterflect;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Binance.Messages;
using System;

namespace QuantConnect.Brokerages.Binance.Converters
{
    public class OrderResponseConverter : JsonConverter<Order>
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
        /// </summary>
        /// <value><c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.</value>
        public override bool CanWrite => false;

        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can read JSON.
        /// </summary>
        /// <value><c>true</c> if this <see cref="JsonConverter"/> can read JSON; otherwise, <c>false</c>.</value>
        public override bool CanRead => true;

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override Order ReadJson(JsonReader reader, Type objectType, Order existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jToken = JToken.Load(reader);

            var order = jToken["algoId"] != null ? new AlgoOrder() : new Order();

            using (var tokenReader = jToken.CreateReader())
            {
                serializer.Populate(tokenReader, order);
            }

            if (order.Time == 0)
            {
                order.Time = ExtractOrderTime(jToken);
            }

            return order;
        }

        /// <summary>
        /// List of possible JSON keys that may contain the order's timestamp.
        /// </summary>
        private static readonly string[] TimeKeys = ["transactTime", "createTime", "time"];

        /// <summary>
        /// Extracts the timestamp from a <see cref="JToken"/> using known possible keys.
        /// Returns 0 if none of the keys are found.
        /// </summary>
        /// <param name="token">The <see cref="JToken"/> representing the order JSON.</param>
        /// <returns>The timestamp as a <see cref="long"/> if found; otherwise, 0.</returns>
        private static long ExtractOrderTime(JToken token)
        {
            foreach (var key in TimeKeys)
            {
                if (token[key] != null)
                {
                    return token[key].Value<long>();
                }
            }

            return 0;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Order value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
