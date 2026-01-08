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
using NUnit.Framework;
using QuantConnect.Brokerages.Binance.Messages;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Binance.Tests
{
    [TestFixture]
    public class BinanceJsonConverterTests
    {
        private static IEnumerable<OrderResponse> PlaceOrderResponses
        {
            get
            {
                yield return new OrderResponse(OrderType.FutureAlgoStopLimit, @"{
    ""algoId"": 2000000179554688,
    ""clientAlgoId"": ""xxxxxxxxxxxxxxxxxxxxx"",
    ""algoType"": ""CONDITIONAL"",
    ""orderType"": ""STOP"",
    ""symbol"": ""ACHUSDT"",
    ""side"": ""BUY"",
    ""positionSide"": ""BOTH"",
    ""timeInForce"": ""GTC"",
    ""quantity"": ""530"",
    ""algoStatus"": ""NEW"",
    ""triggerPrice"": ""3.0000000"",
    ""price"": ""2.0000000"",
    ""icebergQuantity"": null,
    ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
    ""workingType"": ""CONTRACT_PRICE"",
    ""priceMatch"": ""NONE"",
    ""closePosition"": false,
    ""priceProtect"": false,
    ""reduceOnly"": false,
    ""createTime"": 1767808055018,
    ""updateTime"": 1767808055018,
    ""triggerTime"": 0,
    ""goodTillDate"": 0
}", "2000000179554688");

                yield return new OrderResponse(OrderType.SpotStopLimit, @"{
                ""symbol"": ""ACHUSDT"",
                ""orderId"": 1258090419,
                ""orderListId"": -1,
                ""clientOrderId"": ""xxxxxxxxxxxxxxxxxxxxx"",
                ""transactTime"": 1767813080244
                }", "1258090419");

                yield return new OrderResponse(OrderType.SpotMarket, @"{
    ""symbol"": ""ACHUSDT"",
    ""orderId"": 1258091474,
    ""orderListId"": -1,
    ""clientOrderId"": ""xxxxxxxxxxxxxxxxxxxxx"",
    ""transactTime"": 1767813269540,
    ""price"": ""0.00000000"",
    ""origQty"": ""600.00000000"",
    ""executedQty"": ""600.00000000"",
    ""origQuoteOrderQty"": ""0.00000000"",
    ""cummulativeQuoteQty"": ""5.77200000"",
    ""status"": ""FILLED"",
    ""timeInForce"": ""GTC"",
    ""type"": ""MARKET"",
    ""side"": ""BUY"",
    ""workingTime"": 1767813269540,
    ""fills"": [
        {
            ""price"": ""0.00962000"",
            ""qty"": ""600.00000000"",
            ""commission"": ""0.60000000"",
            ""commissionAsset"": ""ACH"",
            ""tradeId"": 63821721
        }
    ],
    ""selfTradePreventionMode"": ""EXPIRE_MAKER""
}", "1258091474");

                yield return new OrderResponse(OrderType.SpotLimit, @"{
    ""symbol"": ""ACHUSDT"",
    ""orderId"": 1258092222,
    ""orderListId"": -1,
    ""clientOrderId"": ""xxxxxxxxxxxxxxxxxxxxx"",
    ""transactTime"": 1767813435766,
    ""price"": ""0.00900000"",
    ""origQty"": ""600.00000000"",
    ""executedQty"": ""0.00000000"",
    ""origQuoteOrderQty"": ""0.00000000"",
    ""cummulativeQuoteQty"": ""0.00000000"",
    ""status"": ""NEW"",
    ""timeInForce"": ""GTC"",
    ""type"": ""LIMIT"",
    ""side"": ""BUY"",
    ""workingTime"": 1767813435766,
    ""fills"": [],
    ""selfTradePreventionMode"": ""EXPIRE_MAKER""
}", "1258092222");

                yield return new OrderResponse(OrderType.FutureAlgoStopMarket, @"{
    ""algoId"": 2000000184401794,
    ""clientAlgoId"": ""47Qn1gLQ29TTEJCBbQGPkb"",
    ""algoType"": ""CONDITIONAL"",
    ""orderType"": ""STOP_MARKET"",
    ""symbol"": ""ACHUSDT"",
    ""side"": ""BUY"",
    ""positionSide"": ""BOTH"",
    ""timeInForce"": ""GTC"",
    ""quantity"": ""600"",
    ""algoStatus"": ""NEW"",
    ""triggerPrice"": ""0.0094000"",
    ""price"": ""0.0000000"",
    ""icebergQuantity"": null,
    ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
    ""workingType"": ""CONTRACT_PRICE"",
    ""priceMatch"": ""NONE"",
    ""closePosition"": false,
    ""priceProtect"": false,
    ""reduceOnly"": false,
    ""createTime"": 1767883004685,
    ""updateTime"": 1767883004685,
    ""triggerTime"": 0,
    ""goodTillDate"": 0
}", "2000000184401794");
            }
        }

        [TestCaseSource(nameof(PlaceOrderResponses))]
        public void DeserializeSubmitOrderResponse(OrderResponse response)
        {
            var (_, json, expectedOrderId) = response;

            var raw = JsonConvert.DeserializeObject<Order>(json);

            Assert.IsNotNull(raw);
            Assert.IsFalse(string.IsNullOrEmpty(raw?.Id));
            Assert.AreEqual(expectedOrderId, raw.Id);
            Assert.Greater(raw.Time, 0);
        }

        private static IEnumerable<OrderResponse> GetOpenOrderResponses
        {
            get
            {
                yield return new OrderResponse(OrderType.FutureAlgoStopLimit, @"    {
        ""algoId"": 2000000183677643,
        ""clientAlgoId"": ""kdnykczqKjjqsCYHJaxVsk"",
        ""algoType"": ""CONDITIONAL"",
        ""orderType"": ""STOP"",
        ""symbol"": ""ACHUSDT"",
        ""side"": ""BUY"",
        ""positionSide"": ""BOTH"",
        ""timeInForce"": ""GTC"",
        ""quantity"": ""560.0"",
        ""algoStatus"": ""NEW"",
        ""actualOrderId"": """",
        ""actualQty"": ""0.0"",
        ""triggerPrice"": ""0.0094"",
        ""price"": ""0.009"",
        ""icebergQuantity"": null,
        ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
        ""workingType"": ""CONTRACT_PRICE"",
        ""priceMatch"": ""NONE"",
        ""closePosition"": false,
        ""priceProtect"": false,
        ""reduceOnly"": false,
        ""createTime"": 1767872015384,
        ""updateTime"": 1767872015384,
        ""triggerTime"": 0,
        ""goodTillDate"": 0
    }", "2000000183677643");

                yield return new OrderResponse(OrderType.SpotStopLimit, @"    {
        ""symbol"": ""ACHUSDT"",
        ""orderId"": 1258673217,
        ""orderListId"": -1,
        ""clientOrderId"": ""XmR3BMF1fI7Y9xpQ6sq5VO"",
        ""price"": ""0.00900000"",
        ""origQty"": ""600.00000000"",
        ""executedQty"": ""0.00000000"",
        ""cummulativeQuoteQty"": ""0.00000000"",
        ""status"": ""NEW"",
        ""timeInForce"": ""GTC"",
        ""type"": ""STOP_LOSS_LIMIT"",
        ""side"": ""BUY"",
        ""stopPrice"": ""0.00980000"",
        ""icebergQty"": ""0.00000000"",
        ""time"": 1767872409547,
        ""updateTime"": 1767872409547,
        ""isWorking"": false,
        ""workingTime"": -1,
        ""origQuoteOrderQty"": ""0.00000000"",
        ""selfTradePreventionMode"": ""EXPIRE_MAKER""
    }", "1258673217");

                yield return new OrderResponse(OrderType.FutureAlgoStopMarket, @"{
        ""algoId"": 2000000184401794,
        ""clientAlgoId"": ""47Qn1gLQ29TTEJCBbQGPkb"",
        ""algoType"": ""CONDITIONAL"",
        ""orderType"": ""STOP_MARKET"",
        ""symbol"": ""ACHUSDT"",
        ""side"": ""BUY"",
        ""positionSide"": ""BOTH"",
        ""timeInForce"": ""GTC"",
        ""quantity"": ""600.0"",
        ""algoStatus"": ""NEW"",
        ""actualOrderId"": """",
        ""actualQty"": ""0.0"",
        ""triggerPrice"": ""0.0094"",
        ""price"": ""0.0"",
        ""icebergQuantity"": null,
        ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
        ""workingType"": ""CONTRACT_PRICE"",
        ""priceMatch"": ""NONE"",
        ""closePosition"": false,
        ""priceProtect"": false,
        ""reduceOnly"": false,
        ""createTime"": 1767883004685,
        ""updateTime"": 1767883004685,
        ""triggerTime"": 0,
        ""goodTillDate"": 0
    }", "2000000184401794");
            }
        }

        [TestCaseSource(nameof(GetOpenOrderResponses))]
        public void DeserializeGetOpenOrderResponse(OrderResponse response)
        {
            var (orderType, json, expectedOrderId) = response;

            var raw = JsonConvert.DeserializeObject<Order>(json);

            Assert.IsNotNull(raw);
            Assert.AreEqual(expectedOrderId, raw.Id);
            Assert.IsFalse(string.IsNullOrEmpty(raw.Type));
            Assert.IsFalse(string.IsNullOrEmpty(raw.Status));
            Assert.AreEqual("NEW", raw.Status.ToUpperInvariant());
            Assert.That(raw.Time, Is.GreaterThan(0), "The Time <= 0");

            Assert.IsFalse(string.IsNullOrEmpty(raw.Side));
            switch (raw.Side)
            {
                case "BUY":
                    Assert.Greater(raw.Quantity, 0);
                    break;
                case "SELL":
                    Assert.Less(raw.Quantity, 0);
                    break;
                default:
                    Assert.Fail($"Unexpected Side value: {raw.Side}");
                    break;
            }

            switch (orderType)
            {
                case OrderType.FutureAlgoStopLimit:
                case OrderType.SpotStopLimit:
                    Assert.Greater(raw.StopPrice, 0);
                    Assert.Greater(raw.Price, 0);
                    break;
            }
        }

        public record OrderResponse(OrderType OrderType, string Json, string ExpectedOrderId)
        {
            public override string ToString() => OrderType.ToString();
        }

        public enum OrderType
        {
            SpotStopLimit,
            SpotLimit,
            SpotMarket,
            FutureAlgoStopLimit,
            FutureAlgoStopMarket
        }
    }
}
