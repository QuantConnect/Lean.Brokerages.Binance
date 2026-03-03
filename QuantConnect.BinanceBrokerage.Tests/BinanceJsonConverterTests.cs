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
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using QuantConnect.Brokerages.Binance.Messages;

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

                yield return new OrderResponse(OrderType.FutureCoinStopMarket, @"{
    ""orderId"": 5834920738,
    ""symbol"": ""SANDUSD_PERP"",
    ""pair"": ""SANDUSD"",
    ""status"": ""NEW"",
    ""clientOrderId"": ""bN3K9RREvxx9drvN1EGth0"",
    ""price"": ""0"",
    ""avgPrice"": ""0.00"",
    ""origQty"": ""1"",
    ""executedQty"": ""0"",
    ""cumQty"": ""0"",
    ""cumBase"": ""0"",
    ""timeInForce"": ""GTC"",
    ""type"": ""STOP_MARKET"",
    ""reduceOnly"": false,
    ""closePosition"": false,
    ""side"": ""BUY"",
    ""positionSide"": ""BOTH"",
    ""stopPrice"": ""0.0857"",
    ""workingType"": ""CONTRACT_PRICE"",
    ""priceProtect"": false,
    ""origType"": ""STOP_MARKET"",
    ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
    ""updateTime"": 1772571245283,
    ""priceMatch"": ""NONE""
}", "5834920738");

                yield return new OrderResponse(OrderType.FutureCoinStopLimit, @"{
    ""orderId"": 5834921880,
    ""symbol"": ""SANDUSD_PERP"",
    ""pair"": ""SANDUSD"",
    ""status"": ""NEW"",
    ""clientOrderId"": ""pB70Kz7B72u2NfbUtj0lUc"",
    ""price"": ""0.0825"",
    ""avgPrice"": ""0.00"",
    ""origQty"": ""1"",
    ""executedQty"": ""0"",
    ""cumQty"": ""0"",
    ""cumBase"": ""0"",
    ""timeInForce"": ""GTC"",
    ""type"": ""STOP"",
    ""reduceOnly"": false,
    ""closePosition"": false,
    ""side"": ""BUY"",
    ""positionSide"": ""BOTH"",
    ""stopPrice"": ""0.0859"",
    ""workingType"": ""CONTRACT_PRICE"",
    ""priceProtect"": false,
    ""origType"": ""STOP"",
    ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
    ""updateTime"": 1772571305861,
    ""priceMatch"": ""NONE""
}", "5834921880");
            }
        }

        [TestCaseSource(nameof(PlaceOrderResponses))]
        public void DeserializeSubmitOrderResponse(OrderResponse response)
        {
            var (_, json, expectedOrderId) = response;

            var raw = JsonConvert.DeserializeObject<NewOrder>(json);

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

                yield return new OrderResponse(OrderType.FutureCoinStopMarket, @"    {
        ""orderId"": 5834895424,
        ""symbol"": ""SANDUSD_PERP"",
        ""pair"": ""SANDUSD"",
        ""status"": ""NEW"",
        ""clientOrderId"": ""web_coin_nlle6z9j33xonuhyjxjhz76"",
        ""price"": ""0"",
        ""avgPrice"": ""0"",
        ""origQty"": ""1"",
        ""executedQty"": ""0"",
        ""cumBase"": ""0"",
        ""timeInForce"": ""GTC"",
        ""type"": ""STOP_MARKET"",
        ""reduceOnly"": false,
        ""closePosition"": false,
        ""side"": ""BUY"",
        ""positionSide"": ""BOTH"",
        ""stopPrice"": ""0.0855"",
        ""workingType"": ""CONTRACT_PRICE"",
        ""priceProtect"": true,
        ""origType"": ""STOP_MARKET"",
        ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
        ""time"": 1772568178006,
        ""updateTime"": 1772568178006,
        ""priceMatch"": ""NONE""
    }", "5834895424");

                yield return new OrderResponse(OrderType.FutureCoinStopLimit, @"    {
        ""orderId"": 5834896554,
        ""symbol"": ""SANDUSD_PERP"",
        ""pair"": ""SANDUSD"",
        ""status"": ""NEW"",
        ""clientOrderId"": ""web_coin_c5vbvaj2gjacm5zzmibjvgf"",
        ""price"": ""0.0845"",
        ""avgPrice"": ""0"",
        ""origQty"": ""1"",
        ""executedQty"": ""0"",
        ""cumBase"": ""0"",
        ""timeInForce"": ""GTC"",
        ""type"": ""STOP"",
        ""reduceOnly"": false,
        ""closePosition"": false,
        ""side"": ""BUY"",
        ""positionSide"": ""BOTH"",
        ""stopPrice"": ""0.0855"",
        ""workingType"": ""CONTRACT_PRICE"",
        ""priceProtect"": true,
        ""origType"": ""STOP"",
        ""selfTradePreventionMode"": ""EXPIRE_MAKER"",
        ""time"": 1772568376819,
        ""updateTime"": 1772568376819,
        ""priceMatch"": ""NONE""
    }", "5834896554");
            }
        }

        [TestCaseSource(nameof(GetOpenOrderResponses))]
        public void DeserializeGetOpenOrderResponse(OrderResponse response)
        {
            var (orderType, json, expectedOrderId) = response;

            var raw = JsonConvert.DeserializeObject<OpenOrder>(json);

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
                case OrderType.FutureCoinStopLimit:
                    Assert.Greater(raw.StopPrice, 0);
                    Assert.Greater(raw.Price, 0);
                    break;
                case OrderType.FutureCoinStopMarket:
                    Assert.Greater(raw.StopPrice, 0);
                    break;
            }
        }

        private static IEnumerable<OrderResponse> OrderWebSocketMessages
        {
            get
            {
                yield return new OrderResponse(OrderType.FutureMarket, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1767884140553,
    ""E"": 1767884140553,
    ""o"": {
        ""s"": ""ACHUSDT"",
        ""c"": ""L7mGp1i08MTITjZk83hDih"",
        ""S"": ""BUY"",
        ""o"": ""MARKET"",
        ""f"": ""GTC"",
        ""q"": ""600"",
        ""p"": ""0"",
        ""ap"": ""0.009066"",
        ""sp"": ""0"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 4759130004,
        ""l"": ""600"",
        ""z"": ""600"",
        ""L"": ""0.009066"",
        ""n"": ""0.0027198"",
        ""N"": ""USDT"",
        ""T"": 1767884140553,
        ""t"": 247236558,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": false,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""MARKET"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 0,
        ""ss"": 0,
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""gtd"": 0,
        ""er"": ""0""
    }
}", "4759130004");

                yield return new OrderResponse(OrderType.SpotMarket, @"{
    ""e"": ""executionReport"",
    ""E"": 1767883925104,
    ""s"": ""ACHUSDT"",
    ""c"": ""vleXDHU2V2W89JArlx8F7X"",
    ""S"": ""BUY"",
    ""o"": ""MARKET"",
    ""f"": ""GTC"",
    ""q"": ""600.00000000"",
    ""p"": ""0.00000000"",
    ""P"": ""0.00000000"",
    ""F"": ""0.00000000"",
    ""g"": -1,
    ""C"": """",
    ""x"": ""TRADE"",
    ""X"": ""FILLED"",
    ""r"": ""NONE"",
    ""i"": 1258777293,
    ""l"": ""600.00000000"",
    ""z"": ""600.00000000"",
    ""L"": ""0.00905000"",
    ""n"": ""0.60000000"",
    ""N"": ""ACH"",
    ""T"": 1767883925104,
    ""t"": 63858276,
    ""I"": 2576796861,
    ""w"": false,
    ""m"": false,
    ""M"": true,
    ""O"": 1767883925104,
    ""Z"": ""5.43000000"",
    ""Y"": ""5.43000000"",
    ""Q"": ""0.00000000"",
    ""W"": 1767883925104,
    ""V"": ""EXPIRE_MAKER""
}", "1258777293");

                yield return new OrderResponse(OrderType.FutureLimit, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1767886244297,
    ""E"": 1767886244298,
    ""o"": {
        ""s"": ""ACHUSDT"",
        ""c"": ""8KQJhcz7Z9li4cM1Pei7YB"",
        ""S"": ""BUY"",
        ""o"": ""LIMIT"",
        ""f"": ""GTC"",
        ""q"": ""600"",
        ""p"": ""0.00934"",
        ""ap"": ""0.009336"",
        ""sp"": ""0"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 4759268152,
        ""l"": ""600"",
        ""z"": ""600"",
        ""L"": ""0.009336"",
        ""n"": ""0.0028008"",
        ""N"": ""USDT"",
        ""T"": 1767886244297,
        ""t"": 247245300,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": false,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""LIMIT"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 0,
        ""ss"": 0,
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""gtd"": 0,
        ""er"": ""0""
    }
}", "4759268152");

                yield return new OrderResponse(OrderType.FutureAlgoStopMarket, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1767887223537,
    ""E"": 1767887223538,
    ""o"": {
        ""s"": ""ACHUSDT"",
        ""c"": ""4TAmvfO43aunsU0Qzo95FS"",
        ""S"": ""BUY"",
        ""o"": ""MARKET"",
        ""f"": ""GTC"",
        ""q"": ""600"",
        ""p"": ""0"",
        ""ap"": ""0.00936"",
        ""sp"": ""0"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 4759320317,
        ""l"": ""600"",
        ""z"": ""600"",
        ""L"": ""0.00936"",
        ""n"": ""0.002808"",
        ""N"": ""USDT"",
        ""T"": 1767887223537,
        ""t"": 247248897,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": false,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""MARKET"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 2000000184830109,
        ""ss"": -1,
        ""st"": ""ALGO_CONDITION"",
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""gtd"": 0,
        ""er"": ""0""
    }
}", "4759320317");

                yield return new OrderResponse(OrderType.FutureAlgoStopLimit, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1767888140311,
    ""E"": 1767888140311,
    ""o"": {
        ""s"": ""ACHUSDT"",
        ""c"": ""drH8UtUU3zDViGppOPVho3"",
        ""S"": ""BUY"",
        ""o"": ""LIMIT"",
        ""f"": ""GTC"",
        ""q"": ""600"",
        ""p"": ""0.00943"",
        ""ap"": ""0.00943"",
        ""sp"": ""0"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 4759374544,
        ""l"": ""600"",
        ""z"": ""600"",
        ""L"": ""0.00943"",
        ""n"": ""0.0011316"",
        ""N"": ""USDT"",
        ""T"": 1767888140311,
        ""t"": 247252518,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": true,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""LIMIT"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 2000000184902398,
        ""ss"": -1,
        ""st"": ""ALGO_CONDITION"",
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""gtd"": 0,
        ""er"": ""0""
    }
}", "4759374544");

                yield return new OrderResponse(OrderType.NewApiSpotMarket, @"{
    ""subscriptionId"": 0,
    ""event"": {
        ""e"": ""executionReport"",
        ""E"": 1772454841401,
        ""s"": ""ACHUSDT"",
        ""c"": ""15ccpBJHVXKrRcctVUy6uX"",
        ""S"": ""BUY"",
        ""o"": ""MARKET"",
        ""f"": ""GTC"",
        ""q"": ""800.00000000"",
        ""p"": ""0.00000000"",
        ""P"": ""0.00000000"",
        ""F"": ""0.00000000"",
        ""g"": -1,
        ""C"": """",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""r"": ""NONE"",
        ""i"": 1281704667,
        ""l"": ""800.00000000"",
        ""z"": ""800.00000000"",
        ""L"": ""0.00663000"",
        ""n"": ""0.00000639"",
        ""N"": ""BNB"",
        ""T"": 1772454841401,
        ""t"": 65043716,
        ""I"": 2623801879,
        ""w"": false,
        ""m"": false,
        ""M"": true,
        ""O"": 1772454841401,
        ""Z"": ""5.30400000"",
        ""Y"": ""5.30400000"",
        ""Q"": ""0.00000000"",
        ""W"": 1772454841401,
        ""V"": ""EXPIRE_MAKER""
    }
}", "1281704667");

                yield return new OrderResponse(OrderType.FutureCoinMarket, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1772573412996,
    ""E"": 1772573412996,
    ""i"": ""FzAusRuXAuoCFzSg"",
    ""o"": {
        ""s"": ""SANDUSD_PERP"",
        ""c"": ""web_usdt_vtdcx4sv95iwl5mf9ju6zx7"",
        ""S"": ""BUY"",
        ""o"": ""MARKET"",
        ""f"": ""GTC"",
        ""q"": ""1"",
        ""p"": ""0"",
        ""ap"": ""0.0836"",
        ""sp"": ""0"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 5834939854,
        ""l"": ""1"",
        ""z"": ""1"",
        ""L"": ""0.0836"",
        ""n"": ""0.05980861"",
        ""N"": ""SAND"",
        ""T"": 1772573412996,
        ""t"": 52897735,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": false,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""MARKET"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""ma"": ""SAND"",
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 0,
        ""ss"": 0,
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""er"": ""0""
    }
}", "5834939854");

                yield return new OrderResponse(OrderType.FutureCoinStopMarket, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1772573616997,
    ""E"": 1772573616998,
    ""i"": ""FzAusRuXAuoCFzSg"",
    ""o"": {
        ""s"": ""SANDUSD_PERP"",
        ""c"": ""web_usdt_bbtxn67bpvyjpsqpsnlpdci"",
        ""S"": ""SELL"",
        ""o"": ""LIMIT"",
        ""f"": ""GTC"",
        ""q"": ""1"",
        ""p"": ""0.0836"",
        ""ap"": ""0.0836"",
        ""sp"": ""0"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 5834940971,
        ""l"": ""1"",
        ""z"": ""1"",
        ""L"": ""0.0836"",
        ""n"": ""0.02392344"",
        ""N"": ""SAND"",
        ""T"": 1772573616997,
        ""t"": 52897740,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": true,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""LIMIT"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""ma"": ""SAND"",
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 0,
        ""ss"": 0,
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""er"": ""0""
    }
}", "5834940971");

                yield return new OrderResponse(OrderType.FutureCoinStopMarket, @"{
    ""e"": ""ORDER_TRADE_UPDATE"",
    ""T"": 1772573992256,
    ""E"": 1772573992257,
    ""i"": ""FzAusRuXAuoCFzSg"",
    ""o"": {
        ""s"": ""SANDUSD_PERP"",
        ""c"": ""web_usdt_qvncdefuaj9gkbmejzhik7n"",
        ""S"": ""BUY"",
        ""o"": ""MARKET"",
        ""f"": ""GTC"",
        ""q"": ""1"",
        ""p"": ""0"",
        ""ap"": ""0.0836"",
        ""sp"": ""0.0836"",
        ""x"": ""TRADE"",
        ""X"": ""FILLED"",
        ""i"": 5834942621,
        ""l"": ""1"",
        ""z"": ""1"",
        ""L"": ""0.0836"",
        ""n"": ""0.05980861"",
        ""N"": ""SAND"",
        ""T"": 1772573992256,
        ""t"": 52897763,
        ""b"": ""0"",
        ""a"": ""0"",
        ""m"": false,
        ""R"": false,
        ""wt"": ""CONTRACT_PRICE"",
        ""ot"": ""TAKE_PROFIT_MARKET"",
        ""ps"": ""BOTH"",
        ""cp"": false,
        ""ma"": ""SAND"",
        ""rp"": ""0"",
        ""pP"": false,
        ""si"": 0,
        ""ss"": 0,
        ""V"": ""EXPIRE_MAKER"",
        ""pm"": ""NONE"",
        ""er"": ""0""
    }
}", "5834942621");
            }
        }

        [TestCaseSource(nameof(OrderWebSocketMessages))]
        public void DeserializeWebSocketMessage(OrderResponse response)
        {
            var (orderType, message, expectedOrderId) = response;

            var objData = JObject.Parse(message);

            Assert.IsTrue(BinanceBrokerage.TryGetExecution(objData, out var execution));

            Assert.IsTrue(execution.ExecutionType.Equals("TRADE", StringComparison.OrdinalIgnoreCase)
                || execution.ExecutionType.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase));

            Assert.IsFalse(string.IsNullOrEmpty(execution.OrderId));
            Assert.AreEqual(expectedOrderId, execution.OrderId);

            Assert.Greater(execution.LastExecutedPrice, 0);
            Assert.IsTrue(execution.Direction
                is Orders.OrderDirection.Buy or Orders.OrderDirection.Sell);
            Assert.Greater(execution.LastExecutedQuantity, 0);
            Assert.Greater(execution.TransactionTime, 0);
            Assert.IsFalse(string.IsNullOrEmpty(execution.FeeCurrency));
            Assert.Greater(execution.Fee, 0);
            Assert.IsFalse(string.IsNullOrEmpty(execution.OrderStatus));

            switch (orderType)
            {
                case OrderType.FutureAlgoStopMarket:
                case OrderType.FutureAlgoStopLimit:
                    Assert.IsFalse(string.IsNullOrEmpty(execution.AlgoOrderId));
                    break;
                case OrderType.NewApiSpotMarket:
                case OrderType.SpotMarket:
                    Assert.IsTrue(string.IsNullOrEmpty(execution.AlgoOrderId));
                    break;
                default:
                    Assert.IsTrue(execution.AlgoOrderId.Equals("0", StringComparison.InvariantCultureIgnoreCase));
                    break;
            }
        }

        private static IEnumerable<TestCaseData> NotSupportWebSocketMessages
        {
            get
            {
                yield return new TestCaseData(@"{
    ""subscriptionId"": 0,
    ""event"": {
        ""e"": ""outboundAccountPosition"",
        ""E"": 1772457243788,
        ""u"": 1772457243788,
        ""B"": [
            {
                ""a"": ""BNB"",
                ""f"": ""0.00594843"",
                ""l"": ""0.00000000""
            },
            {
                ""a"": ""USDT"",
                ""f"": ""7.08944059"",
                ""l"": ""0.00000000""
            },
            {
                ""a"": ""ACH"",
                ""f"": ""0.38000000"",
                ""l"": ""0.00000000""
            }
        ]
    }
}").SetArgDisplayNames("outboundAccountPosition");

                yield return new TestCaseData(@"{
    ""id"": ""7f7d5503-7a00-41e6-8740-8155fb37a6c0"",
    ""status"": 200,
    ""result"": {
        ""subscriptionId"": 0
    },
    ""rateLimits"": [
        {
            ""rateLimitType"": ""REQUEST_WEIGHT"",
            ""interval"": ""MINUTE"",
            ""intervalNum"": 1,
            ""limit"": 6000,
            ""count"": 4
        }
    ]
}").SetArgDisplayNames("subscriptionResponse");

                yield return new TestCaseData(@"{
    ""e"": ""TRADE_LITE"",
    ""E"": 1772461802803,
    ""T"": 1772461802803,
    ""s"": ""ACHUSDT"",
    ""q"": ""800"",
    ""p"": ""0.0000000"",
    ""m"": false,
    ""c"": ""nOVzgnvTSGAbIWUtqT8yBT"",
    ""S"": ""BUY"",
    ""L"": ""0.0065670"",
    ""l"": ""800"",
    ""t"": 258306880,
    ""i"": 4903991227
}").SetArgDisplayNames("Future.TRADE_LITE");

                yield return new TestCaseData(@"{
    ""e"": ""ACCOUNT_UPDATE"",
    ""T"": 1772461802803,
    ""E"": 1772461802803,
    ""a"": {
        ""B"": [
            {
                ""a"": ""USDT"",
                ""wb"": ""6.02834824"",
                ""cw"": ""0.77598304"",
                ""bc"": ""0""
            }
        ],
        ""P"": [
            {
                ""s"": ""ACHUSDT"",
                ""pa"": ""800"",
                ""ep"": ""0.006567"",
                ""cr"": ""1.19615403"",
                ""up"": ""-0.001392"",
                ""mt"": ""isolated"",
                ""iw"": ""5.2523652"",
                ""ps"": ""BOTH"",
                ""ma"": ""USDT"",
                ""bep"": ""0.0065702835""
            }
        ],
        ""m"": ""ORDER""
    }
}").SetArgDisplayNames("Future.ACCOUNT_UPDATE");
            }
        }

        [TestCaseSource(nameof(NotSupportWebSocketMessages))]
        public void DeserializeNotSupportWebSocketMessage(string message)
        {
            var objData = JObject.Parse(message);

            Assert.IsFalse(BinanceBrokerage.TryGetExecution(objData, out var execution));
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
            FutureMarket,
            FutureLimit,
            FutureAlgoStopLimit,
            FutureAlgoStopMarket,
            NewApiSpotMarket,
            FutureCoinMarket,
            FutureCoinLimit,
            FutureCoinStopMarket,
            FutureCoinStopLimit
        }

        [Test]
        public void SerializeSubscribeSignatureRequestShouldContainCorrectCamelCaseFields()
        {
            var apiKey = "123";
            var apiSecret = "456";

            var request = new SubscribeSignature(apiKey, apiSecret);

            var json = request.ToJson();

            Assert.IsNotEmpty(json, "Serialized JSON should not be empty.");

            var obj = JObject.Parse(json);

            Assert.IsTrue(Guid.TryParse(obj.Value<string>("id"), out _), "'id' should be a valid GUID.");
            Assert.AreEqual("userDataStream.subscribe.signature", obj.Value<string>("method"), "'method' field is incorrect.");

            var @params = obj["params"] as JObject;

            var expectedKeys = new[] { "apiKey", "signature", "timestamp" };
            foreach (var key in expectedKeys)
            {
                Assert.IsTrue(@params.ContainsKey(key), $"Param '{key}' is missing or not camelCase.");
            }

            Assert.AreEqual(apiKey, @params.Value<string>("apiKey"), "'apiKey' value is incorrect.");
            Assert.IsFalse(string.IsNullOrEmpty(@params.Value<string>("signature")), "'signature' value is incorrect.");
            Assert.AreNotEqual(0, @params.Value<long>("timestamp"), "'timestamp' value is incorrect.");
        }
    }
}
