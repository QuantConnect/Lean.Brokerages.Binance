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

using Moq;
using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Brokerages.Binance.Tests
{
    [TestFixture]
    public partial class BinanceFuturesBrokerageTests : BrokerageTests
    {
        private BinanceBaseRestApiClient _binanceApi;

        /// <summary>
        /// Creates the brokerage under test and connects it
        /// </summary>
        /// <param name="orderProvider"></param>
        /// <param name="securityProvider"></param>
        /// <returns></returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                { Symbol, BinanceBrokerageTests.CreateSecurity(Symbol) }
            };
            var algorithmSettings = new AlgorithmSettings();
            var transactions = new SecurityTransactionManager(null, securities);
            var orderProcessor = new FakeOrderProcessor();
            transactions.SetOrderProcessor(orderProcessor);

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.Securities).Returns(securities);
            algorithm.Setup(a => a.BrokerageModel).Returns(new BinanceFuturesBrokerageModel(AccountType.Margin));
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions, algorithmSettings));

            var apiKey = Config.Get("binance-api-key");
            var apiSecret = Config.Get("binance-api-secret");
            var apiUrl = Config.Get(BinanceFuturesBrokerageFactory.ApiUrlKeyName, "https://fapi.binance.com");
            var websocketUrl = Config.Get(BinanceFuturesBrokerageFactory.WebSocketUrlKeyName, "wss://fstream.binance.com/ws");

            _binanceApi = new BinanceFuturesRestApiClient(
                SymbolMapper,
                algorithm.Object?.Portfolio,
                apiKey,
                apiSecret,
                apiUrl);

            var broekrage = new BinanceFuturesBrokerage(
                    apiKey,
                    apiSecret,
                    apiUrl,
                    websocketUrl,
                    algorithm.Object,
                    new AggregationManager(),
                    null
                );

            broekrage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                var orderEvent = orderEvents[0];

                switch (orderEvent.Status)
                {
                    case OrderStatus.Submitted:
                        var externalOrder = OrderProvider.GetOrderById(orderEvent.OrderId);
                        orderProcessor.AddOrder(externalOrder);
                        break;
                    case OrderStatus.Canceled:
                    case OrderStatus.Filled:
                        var order = orderProcessor.GetOrderById(orderEvent.OrderId);
                        order.Status = orderEvent.Status;
                        break;
                };
            };

            return broekrage;
        }

        /// <summary>
        /// Gets Binance symbol mapper
        /// </summary>
        protected virtual ISymbolMapper SymbolMapper => new SymbolPropertiesDatabaseSymbolMapper(Market.Binance);

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => StaticSymbol;
        private static Symbol StaticSymbol => Symbol.Create("XRPUSDT", SecurityType.CryptoFuture, Market.Binance);

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => Symbol.SecurityType;

        public static IEnumerable<TestCaseData> OrderParametersFutures
        {
            get
            {
                yield return new TestCaseData(new MarketOrderTestParameters(StaticSymbol));
                yield return new TestCaseData(new LimitOrderTestParameters(StaticSymbol, HighPrice, LowPrice));
                yield return new TestCaseData(new StopMarketOrderTestParameters(StaticSymbol, HighPrice, LowPrice));
            }

        }

        /// <summary>
        /// Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        private const decimal HighPrice = 3m;

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        private const decimal LowPrice = 2m;

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var brokerageSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

            var prices = _binanceApi.GetTickers();

            return prices
                .FirstOrDefault(t => t.Symbol == brokerageSymbol)
                .Price;
        }

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync() => false;

        /// <summary>
        /// Gets the default order quantity. Min order 5USD.
        /// </summary>
        protected override decimal GetDefaultQuantity() => 30m;

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParametersFutures))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }
    }
}
