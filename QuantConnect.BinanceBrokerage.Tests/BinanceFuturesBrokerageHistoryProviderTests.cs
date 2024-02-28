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
using NUnit.Framework;
using QuantConnect.Tests;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Brokerages.Binance.Tests
{
    [TestFixture]
    public class BinanceFuturesBrokerageHistoryProviderTests
    {
        private Brokerage _brokerage;

        [OneTimeSetUp]
        public void Setup()
        {
            _brokerage = CreateBrokerage();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _brokerage?.Disconnect();
            _brokerage?.Dispose();
        }

        [Test]
        [TestCaseSource(nameof(ValidHistory))]
        [TestCaseSource(nameof(InvalidHistory))]
        public virtual void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, bool unsupported)
        {
            BinanceBrokerageHistoryProviderTests.BaseHistoryTest(symbol, resolution, period, tickType, unsupported, _brokerage);
        }

        private static TestCaseData[] ValidHistory
        {
            get
            {
                return new[]
                {
                    // valid
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Minute, Time.OneHour, TickType.Trade, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Hour, Time.OneDay, TickType.Trade, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, false),
                };
            }
        }

        private static TestCaseData[] InvalidHistory
        {
            get
            {
                return new[]
                {
                    // invalid period
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Daily, TimeSpan.FromDays(-15), TickType.Trade, true),
                    // invalid resolution
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Tick, TimeSpan.FromSeconds(15), TickType.Trade, true),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Second, Time.OneMinute, TickType.Trade, true),
                    //invalid tick type
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Minute, Time.OneHour, TickType.Quote, true),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance), Resolution.Minute, Time.OneHour, TickType.OpenInterest, true),
                    // invalid symbol
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.CryptoFuture, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                    // invalid security type
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                };
            }
        }

        protected virtual Brokerage CreateBrokerage()
        {
            var apiKey = Config.Get("binance-api-key");
            var apiSecret = Config.Get("binance-api-secret");
            var apiUrl = Config.Get(BinanceFuturesBrokerageFactory.ApiUrlKeyName, "https://fapi.binance.com");
            var websocketUrl = Config.Get(BinanceFuturesBrokerageFactory.WebSocketUrlKeyName, "wss://fstream.binance.com/ws");

            return new BinanceFuturesBrokerage(
                apiKey,
                apiSecret,
                apiUrl,
                websocketUrl,
                null,
                new AggregationManager(),
                null
            );
        }
    }
}
