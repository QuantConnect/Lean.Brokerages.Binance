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

using Newtonsoft.Json;
using QuantConnect.Orders;
using System;

namespace QuantConnect.Brokerages.Binance.Messages
{
#pragma warning disable 1591

    public class Execution : BaseMessage
    {
        public override EventType @Event => EventType.Execution;

        [JsonProperty("i")]
        public string OrderId { get; set; }

        [JsonProperty("t")]
        public string TradeId { get; set; }

        [JsonProperty("I")]
        public string Ignore { get; set; }

        [JsonProperty("x")]
        public string ExecutionType { get; private set; }

        [JsonProperty("X")]
        public string OrderStatus { get; private set; }

        [JsonProperty("T")]
        public long TransactionTime { get; set; }

        [JsonProperty("L")]
        public decimal LastExecutedPrice { get; set; }

        [JsonProperty("l")]
        public decimal LastExecutedQuantity { get; set; }

        [JsonProperty("S")]
        public string Side { get; set; }

        [JsonProperty("n")]
        public decimal Fee { get; set; }

        [JsonProperty("N")]
        public string FeeCurrency { get; set; }

        public OrderDirection Direction => Side.Equals("BUY", StringComparison.OrdinalIgnoreCase) ? OrderDirection.Buy : OrderDirection.Sell;
    }

#pragma warning restore 1591
}
