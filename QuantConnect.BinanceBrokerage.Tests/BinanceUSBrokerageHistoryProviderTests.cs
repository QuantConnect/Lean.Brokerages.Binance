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
    public class BinanceUSBrokerageHistoryProviderTests : BinanceBrokerageHistoryProviderTests
    {
        [Test]
        [TestCaseSource(nameof(ValidHistory))]
        [TestCaseSource(nameof(InvalidHistory))]
        public override void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            base.GetsHistory(symbol, resolution, period, throwsException);
        }

        [Test]
        [TestCaseSource(nameof(NoHistory))]
        public override void GetEmptyHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType)
        {
            base.GetEmptyHistory(symbol, resolution, period, tickType);
        }

        private static TestCaseData[] ValidHistory
        {
            get
            {
                return new[]
                {
                    // valid
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Daily, TimeSpan.FromDays(15), false),
                };
            }
        }

        private static TestCaseData[] NoHistory
        {
            get
            {
                return new[]
                {
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Tick, TimeSpan.FromSeconds(15), TickType.Trade),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Second, Time.OneMinute, TickType.Trade),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Minute, Time.OneHour, TickType.Quote),
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
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(-15), true),

                    // invalid symbol, throws "System.ArgumentException : Unknown symbol: XYZ"
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Crypto, Market.BinanceUS), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Equity"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), true),
                };
            }
        }

        protected override Brokerage CreateBrokerage()
        {
            var apiKey = Config.Get("binanceus-api-key");
            var apiSecret = Config.Get("binanceus-api-secret");
            var apiUrl = Config.Get("binanceus-api-url", "https://api.binance.us");
            var websocketUrl = Config.Get("binanceus-websocket-url", "wss://stream.binance.us:9443/ws");

            return new BinanceUSBrokerage(
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
