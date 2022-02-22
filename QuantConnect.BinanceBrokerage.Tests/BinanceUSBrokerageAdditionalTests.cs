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
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public class BinanceUSBrokerageAdditionalTests : BinanceBrokerageAdditionalTests
    {
        protected override string BrokerageClassName => nameof(BinanceUSBrokerage);

        protected override Brokerage CreateBrokerage(IAlgorithm algorithm)
        {
            var apiKey = Config.Get("binanceus-api-key");
            var apiSecret = Config.Get("binanceus-api-secret");
            var apiUrl = Config.Get("binanceus-api-url", "https://api.binance.us");
            var websocketUrl = Config.Get("binanceus-websocket-url", "wss://stream.binance.us:9443/ws");

            return new BinanceBrokerage(
                apiKey,
                apiSecret,
                apiUrl,
                websocketUrl,
                algorithm,
                new AggregationManager(),
                null
            );
        }

        [Test]
        public void ThrowInTestnetApiEndpoint()
        {
            Assert.Throws<InvalidOperationException>(() => new BinanceUSBrokerage(
                null,
                null,
                "https://testnet.binance.vision",
                "wss://testnet.binance.vision/ws",
                null,
                null,
                null));
        }
    }
}
