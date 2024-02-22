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

using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests;
using System;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public class BinanceUSBrokerageHistoryProviderTests : BinanceBrokerageHistoryProviderTests
    {
        [Test]
        [TestCaseSource(nameof(ValidHistory))]
        [TestCaseSource(nameof(InvalidHistory))]
        public override void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, bool unsupported)
        {
            base.GetsHistory(symbol, resolution, period, tickType, unsupported);
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
                    // invalid period, no error, empty result
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Daily, TimeSpan.FromDays(-15), TickType.Trade),
                };
            }
        }

        private static TestCaseData[] InvalidHistory
        {
            get
            {
                return new[]
                {
                    // invalid market
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, TimeSpan.FromSeconds(15), TickType.Trade, true),
                    // invalid resolution
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Tick, TimeSpan.FromSeconds(15), TickType.Trade, true),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Second, Time.OneMinute, TickType.Trade, true),
                    // invalid tick type
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Minute, Time.OneHour, TickType.Quote, true),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.BinanceUS), Resolution.Minute, Time.OneHour, TickType.OpenInterest, true),
                    // invalid symbol
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Crypto, Market.BinanceUS), Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
                    // invalid security type
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), TickType.Trade, true),
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
