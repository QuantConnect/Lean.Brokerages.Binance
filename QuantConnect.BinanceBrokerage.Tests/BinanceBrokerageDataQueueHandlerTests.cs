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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using System;
using System.Threading;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public partial class BinanceBrokerageTests
    {
        private static TestCaseData[] TestParameters
        {
            get
            {
                var FTM_USDT = Symbol.Create("FTMUSDT", SecurityType.Crypto, Market.Binance);

                return new[]
                {
                    // valid parameters, for example
                    new TestCaseData(FTM_USDT, Resolution.Tick, false),
                    new TestCaseData(FTM_USDT, Resolution.Minute, false),
                    new TestCaseData(FTM_USDT, Resolution.Second, false),
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public virtual void StreamsDataTest(Symbol symbol, Resolution resolution, bool throwsException)
        {
            StreamsData(symbol, resolution, throwsException, Brokerage);
        }

        public static void StreamsData(Symbol symbol, Resolution resolution, bool throwsException, IBrokerage brokerageInstance)
        {
            var startTime = DateTime.UtcNow;
            var cancelationToken = new CancellationTokenSource();
            var brokerage = (BinanceBrokerage)brokerageInstance;

            SubscriptionDataConfig[] configs;
            if (resolution == Resolution.Tick)
            {
                var tradeConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                    tickType: TickType.Trade);
                var quoteConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                    tickType: TickType.Quote);
                configs = new[] { tradeConfig, quoteConfig };
            }
            else
            {
                configs = new[]
                {
                    GetSubscriptionDataConfig<QuoteBar>(symbol, resolution),
                    GetSubscriptionDataConfig<TradeBar>(symbol, resolution)
                };
            }

            var trade = new ManualResetEvent(false);
            var quote = new ManualResetEvent(false);
            foreach (var config in configs)
            {
                ProcessFeed(brokerage.Subscribe(config, (s, e) =>
                    {
                    }),
                    cancelationToken,
                    (baseData) =>
                    {
                        if (baseData != null)
                        {
                            Assert.GreaterOrEqual(baseData.EndTime.Ticks, startTime.Ticks);

                            if((baseData as Tick)?.TickType == TickType.Quote || baseData is QuoteBar)
                            {
                                quote.Set();
                            }
                            else if ((baseData as Tick)?.TickType == TickType.Trade || baseData is TradeBar)
                            {
                                trade.Set();
                            }
                            Log.Debug("{baseData}");
                        }
                    });
            }

            Assert.IsTrue(trade.WaitOne(resolution.ToTimeSpan() + TimeSpan.FromSeconds(30)));
            Assert.IsTrue(quote.WaitOne(resolution.ToTimeSpan() + TimeSpan.FromSeconds(30)));

            foreach (var config in configs)
            {
                brokerage.Unsubscribe(config);
            }

            Thread.Sleep(2000);

            cancelationToken.Cancel();
        }
    }
}
