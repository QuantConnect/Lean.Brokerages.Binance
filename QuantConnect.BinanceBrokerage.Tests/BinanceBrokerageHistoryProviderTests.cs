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
using QuantConnect.Lean.Engine.HistoricalData;
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
            BaseHistoryTest(symbol, resolution, period, throwsException, _brokerage);
        }

        [Test]
        [TestCaseSource(nameof(NoHistory))]
        public virtual void GetEmptyHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType)
        {
            BaseEmptyHistoryTest(symbol, resolution, period, tickType, _brokerage);
        }

        public static void BaseEmptyHistoryTest(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, Brokerage brokerage)
        {
            TestDelegate test = () =>
            {
                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager()));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
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
                                       tickType)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc).ToList();

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
                Assert.AreEqual(0, historyProvider.DataPointCount);
            };

            Assert.DoesNotThrow(test);
        }

        public static void BaseHistoryTest(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException, Brokerage brokerage)
        {
            TestDelegate test = () =>
            {
                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager()));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
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
                                       TickType.Trade)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick)
                    {
                        foreach (var tick in slice.Ticks[symbol])
                        {
                            Log.Trace("{0}: {1} - {2} / {3}", tick.Time.ToStringInvariant("yyyy-MM-dd HH:mm:ss.fff"), tick.Symbol, tick.BidPrice, tick.AskPrice);
                        }
                    }
                    else
                    {
                        var bar = slice.Bars[symbol];

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
                    }
                }

                Assert.Greater(historyProvider.DataPointCount, 0);
                Log.Debug("Data points retrieved: " + historyProvider.DataPointCount);
            };

            if (throwsException)
            {
                Assert.Throws<ArgumentException>(test);
            }
            else
            {
                Assert.DoesNotThrow(test);
            }
        }

        private static TestCaseData[] ValidHistory
        {
            get
            {
                return new[]
                {
                    // valid
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), false),
                };
            }
        }

        private static TestCaseData[] NoHistory
        {
            get
            {
                return new[]
                {
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Tick, TimeSpan.FromSeconds(15), TickType.Trade),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Second, Time.OneMinute, TickType.Trade),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, Time.OneHour, TickType.Quote),
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
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Equity"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), false),
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
