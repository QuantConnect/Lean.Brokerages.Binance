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
using System;
using RestSharp;
using System.Text;
using NUnit.Framework;
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Brokerages.Binance.Tests
{
    [TestFixture]
    public class BinanceBrokerageAdditionalTests
    {
        protected virtual string BrokerageClassName => nameof(BinanceBrokerage);

        [Test]
        public void ParameterlessConstructorComposerUsage()
        {
            var brokerage = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(BrokerageClassName);
            Assert.IsNotNull(brokerage);
            Assert.True(brokerage.IsConnected);
        }

        [TestCase(typeof(BinanceCrossMarginRestApiClient),"https://api.binance.com")]
        [TestCase(typeof(BinanceFuturesRestApiClient), "https://fapi.binance.com")]
        [TestCase(typeof(BinanceCoinFuturesRestApiClient), "https://dapi.binance.com")]
        public void TickerPriceChangeStatistics(Type restApiClient, string apiUrl)
        {
            var restClient = (BinanceBaseRestApiClient)Activator.CreateInstance(restApiClient, null, null, string.Empty, string.Empty, apiUrl);
            var response = restClient.GetTickerPriceChangeStatistics();

            Assert.AreNotEqual(0, response.Length);
        }

        [Test]
        public void ConnectedIfNoAlgorithm()
        {
            using var brokerage = CreateBrokerage(null);
            Assert.True(brokerage.IsConnected);
        }

        [Test]
        public void ConnectedIfAlgorithmIsNotNullAndClientNotCreated()
        {
            using var brokerage = CreateBrokerage(Mock.Of<IAlgorithm>());
            Assert.True(brokerage.IsConnected);
        }

        [Test]
        public void ConnectToUserDataStreamIfAlgorithmNotNullAndApiIsCreated()
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork));
            var algorithmSettings = new AlgorithmSettings();
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new BinanceBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions, algorithmSettings));

            using var brokerage = CreateBrokerage(algorithm.Object);

            Assert.True(brokerage.IsConnected);

            var _ = brokerage.GetCashBalance();

            Assert.True(brokerage.IsConnected);

            brokerage.Disconnect();

            Assert.False(brokerage.IsConnected);
        }

        protected virtual Brokerage CreateBrokerage(IAlgorithm algorithm)
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
                algorithm,
                new AggregationManager(),
                null
            );
        }

        [Test]
        public void GetRateLimitsByEachBinanceType()
        {
            var restClient = new RestClient();
            var exchangeInfoRequest = new RestRequest("exchangeInfo", RestSharp.Method.GET);

            var binanceUrls = new (string name, string baseUrl)[]
            {
                new("BinanceUS", "https://api.binance.us/api/v3/"),
                new("Binance", "https://api.binance.com/api/v3/"),
                new("BinanceCoinFutures", "https://dapi.binance.com/dapi/v1/"),
                new("BinanceFutures", "https://fapi.binance.com/fapi/v1/")
            };

            var stringBuilder = new StringBuilder();
            foreach (var (name, baseUrl) in binanceUrls)
            {
                restClient.BaseUrl = new Uri(baseUrl);
                var response = restClient.Execute(exchangeInfoRequest);

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    stringBuilder.AppendLine($"Access to {name} is restricted (Forbidden). Skipping...");
                    continue;
                }

                Assert.IsNotNull(response);
                var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfo>(response.Content);

                stringBuilder.AppendLine($"Rate Limits for {name}:");
                foreach (var rateLimit in exchangeInfo.RateLimits)
                {
                    stringBuilder.AppendLine($"  - {rateLimit}");
                }
                stringBuilder.AppendLine();
            }

            Logging.Log.Trace(stringBuilder.ToString());
        }

        public readonly record struct ExchangeInfo(IReadOnlyCollection<RateLimit> RateLimits);
        public readonly record struct RateLimit(string RateLimitType, string Interval, int IntervalNum, int Limit);
    }
}
