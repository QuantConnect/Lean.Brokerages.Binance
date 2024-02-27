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

using NodaTime;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Tests;
using System;
using System.Linq;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public class BinanceBrokerageHistoryProviderTests
    {
        private Brokerage _brokerage;

        [OneTimeSetUp]
        public void Setup()
        {
            TestGlobals.Initialize();
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
            BaseHistoryTest(symbol, resolution, period, tickType, unsupported, _brokerage);
        }

        public static void BaseHistoryTest(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, bool unsupported, Brokerage brokerage)
        {
            var now = DateTime.UtcNow;
            var request = new HistoryRequest(now.Add(-period),
                now,
                typeof(TradeBar),
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                DateTimeZone.Utc,
                Resolution.Minute,
                false,
                false,
                DataNormalizationMode.Adjusted,
                tickType);

            var history = brokerage.GetHistory(request)?.ToList();

            if (unsupported)
            {
                Assert.IsNull(history);
            }
            else
            {
                Assert.IsNotNull(history);

                foreach (var bar in history.Cast<TradeBar>())
                {
                    Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
                }

                Assert.Greater(history.Count, 0);
                Log.Debug("Data points retrieved: " + history.Count);
            }
        }

        private static TestCaseData[] ValidHistory
        {
            get
            {
                return new[]
                {
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, Time.OneHour, TickType.Trade, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Hour, Time.OneDay, TickType.Trade, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, false),
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
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(-15), TickType.Trade, true),
                    // invalid symbol
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                    // invalid security type
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                    new TestCaseData(Symbols.USDJPY, Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                    // invalid market
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                    // invalid resolution
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Tick, TimeSpan.FromSeconds(15), TickType.Trade, true),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Second, Time.OneMinute, TickType.Trade, true),
                    // Invalid tick type
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, Time.OneHour, TickType.Quote, true),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, Time.OneHour, TickType.OpenInterest, true),
                };
            }
        }

        protected virtual Brokerage CreateBrokerage()
        {
            var apiKey = Config.Get("binance-api-key");
            var apiSecret = Config.Get("binance-api-secret");
            var apiUrl = Config.Get("binance-api-url", "https://api.binance.com");
            var websocketUrl = Config.Get("binance-websocket-url", "wss://stream.binance.com:9443/ws");

            return new BinanceBrokerage(
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
