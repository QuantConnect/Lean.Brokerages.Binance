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
using QuantConnect.Logging;
using QuantConnect.BinanceBrokerage.ToolBox;
using System;
using System.Linq;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public class BinanceBrokerageExchangeInfoTests
    {
        [Test]
        [TestCase(Market.Binance)]
        [TestCase(Market.BinanceUS)]
        public void GetsExchangeInfo(string marketName)
        {
            var downloader = new BinanceExchangeInfoDownloader(marketName);
            var tickers = downloader.Get().ToList();

            Assert.IsTrue(tickers.Any());

            foreach (var t in tickers)
            {
                Assert.IsTrue(t.StartsWith(marketName, StringComparison.OrdinalIgnoreCase));
            }

            Log.Trace("Tickers retrieved: " + tickers.Count);
        }
    }
}
