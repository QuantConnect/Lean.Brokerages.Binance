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
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public class BinanceCoinFuturesBrokerageHistoryProviderTests
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
        public virtual void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            BinanceBrokerageHistoryProviderTests.BaseHistoryTest(symbol, resolution, period, throwsException, _brokerage);
        }

        [Test]
        [TestCaseSource(nameof(NoHistory))]
        public virtual void GetEmptyHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType)
        {
            BinanceBrokerageHistoryProviderTests.BaseEmptyHistoryTest(symbol, resolution, period, tickType, _brokerage);
        }

        private static TestCaseData[] ValidHistory
        {
            get
            {
                return new[]
                {
                    // valid
                    new TestCaseData(Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance), Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance), Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), false),
                };
            }
        }

        private static TestCaseData[] NoHistory
        {
            get
            {
                return new[]
                {
                    new TestCaseData(Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance), Resolution.Tick, TimeSpan.FromSeconds(15), TickType.Trade),
                    new TestCaseData(Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance), Resolution.Second, Time.OneMinute, TickType.Trade),
                    new TestCaseData(Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance), Resolution.Minute, Time.OneHour, TickType.Quote),
                };
            }
        }

        private static TestCaseData[] InvalidHistory
        {
            get
            {
                return new[]
                {
                    // invalid period, no error, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(-15), false),

                    // invalid symbol, throws "System.ArgumentException : Unknown symbol: XYZ"
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.CryptoFuture, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Equity"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), false),
                };
            }
        }

        protected virtual Brokerage CreateBrokerage()
        {
            var apiKey = Config.Get("binance-api-key");
            var apiSecret = Config.Get("binance-api-secret");
            var apiUrl = Config.Get(BinanceCoinFuturesBrokerageFactory.ApiUrlKeyName, "https://dapi.binance.com");
            var websocketUrl = Config.Get(BinanceCoinFuturesBrokerageFactory.WebSocketUrlKeyName, "wss://dstream.binance.com/ws");

            return new BinanceCoinFuturesBrokerage(
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
