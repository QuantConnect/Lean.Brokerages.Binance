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

using System;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Binance.Converters;

namespace QuantConnect.Brokerages.Binance.Messages
{
#pragma warning disable 1591
    [JsonConverter(typeof(OrderResponseConverter))]
    public class Order
    {
        [JsonProperty("orderId")]
        public virtual string Id { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public virtual decimal StopPrice { get; set; }
        [JsonProperty("origQty")]
        public virtual decimal OriginalAmount { get; set; }
        [JsonProperty("executedQty")]
        public decimal ExecutedAmount { get; set; }
        public virtual string Status { get; set; }
        public virtual string Type { get; set; }
        public string Side { get; set; }

        public virtual long Time { get; set; }

        public decimal Quantity => string.Equals(Side, "buy", StringComparison.OrdinalIgnoreCase) ? OriginalAmount : -OriginalAmount;
    }

    public class AlgoOrder : Order
    {
        [JsonProperty("algoId")]
        public override string Id { get; set; }

        [JsonProperty("createTime")]
        public override long Time { get; set; }

        [JsonProperty("triggerPrice")]
        public override decimal StopPrice { get; set; }

        [JsonProperty("orderType")]
        public override string Type { get; set; }

        [JsonProperty("quantity")]
        public override decimal OriginalAmount { get; set; }

        [JsonProperty("algoStatus")]
        public override string Status { get; set; }
    }
#pragma warning restore 1591
}
