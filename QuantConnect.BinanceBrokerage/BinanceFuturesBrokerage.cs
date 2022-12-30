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

using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;

namespace QuantConnect.BinanceBrokerage
{
    /// <summary>
    /// Binance USDT Futures brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(BinanceFuturesBrokerageFactory))]
    public class BinanceFuturesBrokerage : BinanceBrokerage
    {
        public BinanceFuturesBrokerage() : base(Market.Binance)
        {
        }

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="restApiUrl">The rest api url</param>
        /// <param name="webSocketBaseUrl">The web socket base url</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        public BinanceFuturesBrokerage(string apiKey, string apiSecret, string restApiUrl, string webSocketBaseUrl, IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
            : base(apiKey, apiSecret, restApiUrl, webSocketBaseUrl, algorithm, aggregator, job, Market.Binance)
        {
        }

        protected override void SetJobInit(LiveNodePacket job, IDataAggregator aggregator)
        {
            Initialize(
                job.BrokerageData[BinanceFuturesBrokerageFactory.WebSocketUrlKeyName],
                job.BrokerageData[BinanceFuturesBrokerageFactory.ApiUrlKeyName],
                job.BrokerageData["binance-api-key"],
                job.BrokerageData["binance-api-secret"],
                null,
                aggregator,
                job,
                Market.Binance
            );
        }

        /// <summary>
        /// Checks if this brokerage supports the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>returns true if brokerage supports the specified symbol; otherwise false</returns>
        protected override bool CanSubscribe(Symbol symbol)
        {
            if (!base.CanSubscribe(symbol))
            {
                return false;
            }
            CurrencyPairUtil.DecomposeCurrencyPair(symbol, out var _, out var quoteCurrency);

            return quoteCurrency.Equals("USDT", System.StringComparison.InvariantCultureIgnoreCase)
                || quoteCurrency.Equals("BUSD", System.StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get's the appropiate API client to use
        /// </summary>
        protected override BinanceBaseRestApiClient GetApiClient(ISymbolMapper symbolMapper, ISecurityProvider securityProvider,
            string restApiUrl, string apiKey, string apiSecret)
        {
            restApiUrl ??= Config.Get(BinanceFuturesBrokerageFactory.ApiUrlKeyName, "https://fapi.binance.com");

            return new BinanceFuturesRestApiClient(symbolMapper, securityProvider, apiKey, apiSecret, restApiUrl);
        }

        /// <summary>
        /// Get's the supported security type by the brokerage
        /// </summary>
        protected override SecurityType GetSupportedSecurityType()
        {
            return SecurityType.CryptoFuture;
        }
    }
}
