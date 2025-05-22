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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using NUnit.Framework;
using QuantConnect.Configuration;
using Moq;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Threading;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Binance.Tests
{
    [TestFixture]
    public partial class BinanceBrokerageTests : BrokerageTests
    {
        private BinanceBaseRestApiClient _binanceApi;
        private RateGate _apiRageGate;

        protected RateGate ApiRateGate
        {
            get
            {
                if (_apiRageGate == null)
                {
                    _apiRageGate = new RateGate(100, TimeSpan.FromSeconds(10));
                }
                return _apiRageGate;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _apiRageGate?.DisposeSafely();
            _binanceApi?.DisposeSafely();
        }

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
                { Symbol, CreateSecurity(Symbol) }
            };
            var algorithmSettings = new AlgorithmSettings();
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.Securities).Returns(securities);
            algorithm.Setup(a => a.BrokerageModel).Returns(new BinanceBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions, algorithmSettings));

            var apiKey = Config.Get("binance-api-key");
            var apiSecret = Config.Get("binance-api-secret");
            var apiUrl = Config.Get("binance-api-url", "https://api.binance.com");
            var websocketUrl = Config.Get("binance-websocket-url", "wss://stream.binance.com:9443/ws");

            _binanceApi = new BinanceSpotRestApiClient(
                SymbolMapper,
                algorithm.Object?.Portfolio,
                apiKey,
                apiSecret,
                apiUrl,
                ApiRateGate);

            return new BinanceBrokerage(
                    apiKey,
                    apiSecret,
                    apiUrl,
                    websocketUrl,
                    algorithm.Object,
                    new AggregationManager(),
                    new LiveNodePacket()
                );
        }

        /// <summary>
        /// Gets Binance symbol mapper
        /// </summary>
        protected virtual ISymbolMapper SymbolMapper => new SymbolPropertiesDatabaseSymbolMapper(Market.Binance);

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => StaticSymbol;
        private static Symbol StaticSymbol => Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Binance);

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => Symbol.SecurityType;

        public static TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(StaticSymbol)).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(StaticSymbol, HighPrice, LowPrice)).SetName("LimitOrder"),
            new TestCaseData(new StopLimitOrderTestParameters(StaticSymbol, HighPrice, LowPrice)).SetName("StopLimitOrder"),
        };

        /// <summary>
        /// Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        private const decimal HighPrice = 0.04m;

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        private const decimal LowPrice = 0.01m;

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
        /// Gets the default order quantity. Min order 10USD.
        /// </summary>
        protected override decimal GetDefaultQuantity() => 0.01m;

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }

        [Test, Ignore("Holdings are now set to 0 swaps at the start of each launch. Not meaningful.")]
        public override void GetAccountHoldings()
        {
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            var before = Brokerage.GetAccountHoldings();
            Assert.AreEqual(0, before.Count());

            PlaceOrderWaitForStatus(new MarketOrder(Symbol, GetDefaultQuantity(), DateTime.Now));
            Thread.Sleep(3000);

            var after = Brokerage.GetAccountHoldings();
            Assert.AreEqual(0, after.Count());
        }

        protected override void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            Assert.Pass("Order update not supported. Please cancel and re-create.");
        }

        public static Security CreateSecurity(Symbol symbol)
        {
            var timezone = TimeZones.NewYork;

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                symbol,
                Resolution.Hour,
                timezone,
                timezone,
                true,
                false,
                false);

            return new Security(
                SecurityExchangeHours.AlwaysOpen(timezone),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }
    }
}
