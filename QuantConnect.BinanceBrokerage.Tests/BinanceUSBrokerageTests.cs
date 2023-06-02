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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Tests.Common.Securities;
using System;
using System.Linq;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public partial class BinanceUSBrokerageTests : BinanceBrokerageTests
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

            var apiKey = Config.Get("binanceus-api-key");
            var apiSecret = Config.Get("binanceus-api-secret");
            var apiUrl = Config.Get("binanceus-api-url", "https://api.binance.us");
            var websocketUrl = Config.Get("binanceus-websocket-url", "wss://stream.binance.us:9443/ws");

            _binanceApi = new BinanceSpotRestApiClient(
                SymbolMapper,
                algorithm.Object?.Portfolio,
                apiKey,
                apiSecret,
                apiUrl);

            return new BinanceUSBrokerage(
                    apiKey,
                    apiSecret,
                    apiUrl,
                    websocketUrl,
                    algorithm.Object,
                    new AggregationManager(),
                    null
                );
        }

        /// <summary>
        /// Gets Binance.US symbol mapper
        /// </summary>
        protected override ISymbolMapper SymbolMapper => new SymbolPropertiesDatabaseSymbolMapper(Market.BinanceUS);

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => StaticSymbol;
        private static Symbol StaticSymbol => Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Binance);

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
        /// Gets the default order quantity. Min order 10USD.
        /// </summary>
        protected override decimal GetDefaultQuantity() => 0.005m;

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
    }
}
