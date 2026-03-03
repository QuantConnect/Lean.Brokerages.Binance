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
using QuantConnect.Orders;
using Newtonsoft.Json.Converters;
using QuantConnect.Brokerages.Binance.Enums;

namespace QuantConnect.Brokerages.Binance.Messages
{
#pragma warning disable 1591

    public class Execution : BaseMessage
    {
        public override EventType @Event => EventType.Execution;

        [JsonProperty("i")]
        public string OrderId { get; set; }

        /// <summary>
        /// Strategy (algorithm) order identifier.
        /// </summary>
        /// <remarks>
        /// The field may be absent or default to <c>0</c> in the response.
        /// </remarks>
        [JsonProperty("si")]
        public string AlgoOrderId { get; set; }

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

        /// <summary>
        /// Quote asset volume (Original Quantity)
        /// </summary>
        [JsonProperty("q")]
        public decimal OriginalAmount { get; set; }

        /// <summary>
        /// The total quote asset volume from taker buy orders.
        /// <para><c>Available</c>: Binance Spot.</para>
        /// <para><c>Not available</c>: Binance Futures.</para>
        /// </summary>
        [JsonProperty("Q")]
        public decimal TakerBuyOriginalAmount { get; set; }

        /// <summary>
        /// Order (limit) price.
        /// </summary>
        [JsonProperty("p")]
        public decimal Price { get; set; }

        /// <summary>
        /// Stop price (Spot / Margin).
        /// </summary>
        [JsonProperty("P")]
        public decimal StopPrice { get; set; }

        /// <summary>
        /// Stop price (Futures — field key differs from Spot).
        /// </summary>
        [JsonProperty("sp")]
        public decimal FuturesStopPrice { get; set; }

        /// <summary>
        /// Order type as reported by the exchange (e.g. LIMIT, MARKET, STOP_LOSS_LIMIT).
        /// </summary>
        [JsonProperty("o")]
        public string OrderType { get; set; }

        [JsonProperty("O")]
        public long OrderCreationTime { get; set; }

        /// <summary>
        /// Expiry reason reported by Binance (Futures <c>ORDER_TRADE_UPDATE</c> only, field <c>"er"</c>).
        /// <see cref="FuturesExpiredReason.None"/> (0) means no error; for <c>x:EXPIRED</c> events on
        /// STOP / STOP_MARKET orders it indicates the stop trigger was consumed normally and a child
        /// order will follow in a separate <c>x:NEW</c> event. Any other value means the order failed.
        /// See <see href="https://developers.binance.com/docs/derivatives/usds-margined-futures/user-data-streams/Event-Order-Update">
        /// Binance Futures — Event: Order Update</see> for the full specification.
        /// </summary>
        [JsonProperty("er")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FuturesExpiredReason ExpiredReason { get; set; }

        public OrderDirection Direction => Side.Equals("BUY", StringComparison.OrdinalIgnoreCase) ? OrderDirection.Buy : OrderDirection.Sell;
    }

#pragma warning restore 1591
}
