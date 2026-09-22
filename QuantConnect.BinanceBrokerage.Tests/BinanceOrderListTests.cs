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
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Tests.Common.Orders;

namespace QuantConnect.Brokerages.Binance.Tests
{
    /// <summary>
    /// Hermetic tests of the contingent orders support through the OCO, OTO and OTOCO order lists
    /// </summary>
    [TestFixture]
    public class BinanceOrderListTests
    {
        private static readonly DateTime Time = new DateTime(2024, 1, 2, 15, 0, 0);
        private static readonly Symbol BTCUSDT = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance);
        private BinanceSpotRestApiClient _client;

        [SetUp]
        public void SetUp()
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar), BTCUSDT, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, false, false);
            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), config, new Cash("USDT", 0, 1), SymbolProperties.GetDefault("USDT"),
                ErrorCurrencyConverter.Instance, RegisteredSecurityDataTypesProvider.Null, new SecurityCache());
            security.SetMarketPrice(new Tick(Time, BTCUSDT, 100000, 99999, 100001));
            _client = new BinanceSpotRestApiClient(new SymbolPropertiesDatabaseSymbolMapper(Market.Binance), new TestSecurityProvider(security),
                "key", "secret", "https://api.binance.com", new RateGate(10, TimeSpan.FromSeconds(1)));
        }

        [TearDown]
        public void TearDown()
        {
            _client.DisposeSafely();
        }

        [Test]
        public void OneCancelsOtherBody()
        {
            var manager = new OrderContingency(1, 2, []);
            var orders = new List<Order>
            {
                Member(1, manager, new LimitOrder(BTCUSDT, -1, 110000, Time)),
                Member(2, manager, new StopLimitOrder(BTCUSDT, -1, 90000, 89000, Time))
            };

            var body = _client.CreateOrderListBody(orders, out var endpoint);

            Assert.AreEqual("orderList/oco", endpoint);
            Assert.AreEqual("BTCUSDT", body["symbol"]);
            Assert.AreEqual("SELL", body["side"]);
            Assert.AreEqual("1", body["quantity"]);
            // the limit is above, as a limit maker which takes no time in force, the stop below
            Assert.AreEqual("LIMIT_MAKER", body["aboveType"]);
            Assert.AreEqual("110000", body["abovePrice"]);
            Assert.IsFalse(body.ContainsKey("aboveTimeInForce"));
            Assert.AreEqual("STOP_LOSS_LIMIT", body["belowType"]);
            Assert.AreEqual("90000", body["belowStopPrice"]);
            Assert.AreEqual("89000", body["belowPrice"]);
            Assert.AreEqual("GTC", body["belowTimeInForce"]);
        }

        [Test]
        public void OneTriggersOtherBody()
        {
            var manager = new OrderContingency(1, 2, []);
            var parent = new LimitOrder(BTCUSDT, 1, 95000, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Parent)])
            };
            var child = new LimitOrder(BTCUSDT, -1, 110000, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child)])
            };

            var body = _client.CreateOrderListBody(new List<Order> { parent, child }, out var endpoint);

            Assert.AreEqual("orderList/oto", endpoint);
            Assert.AreEqual("LIMIT", body["workingType"]);
            Assert.AreEqual("BUY", body["workingSide"]);
            Assert.AreEqual("95000", body["workingPrice"]);
            Assert.AreEqual("1", body["workingQuantity"]);
            Assert.AreEqual("GTC", body["workingTimeInForce"]);
            Assert.AreEqual("LIMIT", body["pendingType"]);
            Assert.AreEqual("SELL", body["pendingSide"]);
            Assert.AreEqual("110000", body["pendingPrice"]);
            Assert.AreEqual("GTC", body["pendingTimeInForce"]);
        }

        [Test]
        public void OneTriggersOneCancelsOtherBody()
        {
            var bracket = ContingentOrderTests.CreateBracket();
            var manager = new OrderContingency(1, 3, []);
            var orders = new List<Order>
            {
                Copy(bracket[0], new LimitOrder(BTCUSDT, 1, 95000, Time), manager),
                Copy(bracket[1], new LimitOrder(BTCUSDT, -1, 110000, Time), manager),
                Copy(bracket[2], new StopLimitOrder(BTCUSDT, -1, 90000, 89000, Time), manager)
            };

            var body = _client.CreateOrderListBody(orders, out var endpoint);

            Assert.AreEqual("orderList/otoco", endpoint);
            Assert.AreEqual("LIMIT", body["workingType"]);
            Assert.AreEqual("SELL", body["pendingSide"]);
            Assert.AreEqual("1", body["pendingQuantity"]);
            Assert.AreEqual("LIMIT_MAKER", body["pendingAboveType"]);
            Assert.AreEqual("110000", body["pendingAbovePrice"]);
            Assert.AreEqual("STOP_LOSS_LIMIT", body["pendingBelowType"]);
            Assert.AreEqual("90000", body["pendingBelowStopPrice"]);
            Assert.AreEqual("89000", body["pendingBelowPrice"]);
        }

        [Test]
        public void UnsupportedShapes()
        {
            var manager = new OrderContingency(1, 2, []);
            // different quantities
            var orders = new List<Order>
            {
                Member(1, manager, new LimitOrder(BTCUSDT, -1, 110000, Time)),
                Member(2, manager, new StopLimitOrder(BTCUSDT, -2, 90000, 89000, Time))
            };
            Assert.Throws<NotSupportedException>(() => _client.CreateOrderListBody(orders, out _));

            // market working order
            var parent = new MarketOrder(BTCUSDT, 1, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Parent)])
            };
            var child = new LimitOrder(BTCUSDT, -1, 110000, Time)
            {
                Contingency = manager.WithLinks([new(1, ContingencyType.OneTriggersOther, ContingencyRole.Child)])
            };
            Assert.Throws<NotSupportedException>(() => _client.CreateOrderListBody(new List<Order> { parent, child }, out _));
        }

        [Test]
        public void OrderListResponseDeserialization()
        {
            var json = @"{""orderListId"": 1, ""contingencyType"": ""OCO"", ""listStatusType"": ""EXEC_STARTED"", ""listOrderStatus"": ""EXECUTING"", ""transactionTime"": 1704207600000, ""symbol"": ""BTCUSDT"",
                ""orders"": [{""symbol"": ""BTCUSDT"", ""orderId"": 10, ""clientOrderId"": ""a""}, {""symbol"": ""BTCUSDT"", ""orderId"": 11, ""clientOrderId"": ""b""}],
                ""orderReports"": [
                    {""symbol"": ""BTCUSDT"", ""orderId"": 10, ""orderListId"": 1, ""transactTime"": 1704207600000, ""price"": ""110000"", ""origQty"": ""1"", ""executedQty"": ""0"", ""status"": ""NEW"", ""type"": ""LIMIT_MAKER"", ""side"": ""SELL""},
                    {""symbol"": ""BTCUSDT"", ""orderId"": 11, ""orderListId"": 1, ""transactTime"": 1704207600000, ""price"": ""89000"", ""origQty"": ""1"", ""executedQty"": ""0"", ""status"": ""NEW"", ""type"": ""STOP_LOSS_LIMIT"", ""side"": ""SELL"", ""stopPrice"": ""90000""}
                ]}";

            var orderList = JsonConvert.DeserializeObject<Messages.OrderList>(json);

            Assert.AreEqual(1, orderList.OrderListId);
            Assert.AreEqual(2, orderList.OrderReports.Count);
            Assert.AreEqual("10", orderList.OrderReports[0].Id);
            Assert.AreEqual(110000, orderList.OrderReports[0].Price);
            Assert.AreEqual(1, orderList.OrderReports[0].OrderListId);
            Assert.AreEqual("11", orderList.OrderReports[1].Id);
            Assert.AreEqual(90000, orderList.OrderReports[1].StopPrice);
            Assert.AreEqual(1704207600000, orderList.OrderReports[1].Time);
        }

        [Test]
        public void ExecutionCarriesTheOrderListId()
        {
            var execution = JsonConvert.DeserializeObject<Messages.Execution>(@"{""e"":""executionReport"",""s"":""BTCUSDT"",""i"":10,""g"":1,""x"":""NEW"",""X"":""PENDING_NEW"",""S"":""SELL"",""q"":""1"",""T"":1704207600000}");
            Assert.AreEqual(1, execution.OrderListId);
            Assert.AreEqual(-1, JsonConvert.DeserializeObject<Messages.Execution>(@"{""e"":""executionReport"",""i"":10,""x"":""NEW"",""X"":""NEW""}").OrderListId);
        }

        private static Order Member(int id, OrderContingency set, Order order)
        {
            order.Contingency = set.WithLinks([new(1, ContingencyType.OneCancelsOther)]);
            return order;
        }

        private static Order Copy(Order source, Order target, OrderContingency set)
        {
            target.Contingency = set.WithLinks(source.Contingency.Links.Select(x => x.Clone()));
            return target;
        }

        private class TestSecurityProvider : ISecurityProvider
        {
            private readonly Security _security;
            public TestSecurityProvider(Security security)
            {
                _security = security;
            }
            public Security GetSecurity(Symbol symbol)
            {
                return symbol == _security.Symbol ? _security : null;
            }
        }
    }
}
