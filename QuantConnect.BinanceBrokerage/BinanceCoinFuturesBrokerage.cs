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
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Brokerages.Binance.Constants;
using System;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance Coin Futures brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(BinanceCoinFuturesBrokerageFactory))]
    public class BinanceCoinFuturesBrokerage : BinanceBrokerage
    {
        /// <summary>
        /// Gets the trade channel name used for streaming trade information in the overridden context.
        /// </summary>
        /// <remarks>
        /// The <see cref="TradeChannelName"/> property represents the specific trade channel name utilized for streaming
        /// trade data in the overridden context. In this specific implementation, it returns the constant value
        /// <see cref="TradeChannels.FutureTradeChannelName"/>, indicating the use of Future Trade Streams.
        /// </remarks>
        /// <value>
        /// The value is the channel name for Future Trade Streams: <c>aggTrade</c>.
        /// </value>
        protected override string TradeChannelName => TradeChannels.FutureTradeChannelName;

        public BinanceCoinFuturesBrokerage() : base(Market.Binance)
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
        public BinanceCoinFuturesBrokerage(string apiKey, string apiSecret, string restApiUrl, string webSocketBaseUrl, IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
            : base(apiKey, apiSecret, restApiUrl, webSocketBaseUrl, algorithm, aggregator, job, Market.Binance)
        {
        }

        protected override void SetJobInit(LiveNodePacket job, IDataAggregator aggregator)
        {
            Initialize(
                job.BrokerageData[BinanceCoinFuturesBrokerageFactory.WebSocketUrlKeyName],
                job.BrokerageData[BinanceCoinFuturesBrokerageFactory.ApiUrlKeyName],
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

            return quoteCurrency.Equals("USD", System.StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get's the appropiate API client to use
        /// </summary>
        protected override BinanceBaseRestApiClient GetApiClient(ISymbolMapper symbolMapper, ISecurityProvider securityProvider,
            string restApiUrl, string apiKey, string apiSecret, DeploymentTarget? deploymentTarget)
        {
            restApiUrl ??= Config.Get(BinanceCoinFuturesBrokerageFactory.ApiUrlKeyName, "https://dapi.binance.com");
            RateGate rateGate = null;
            if (deploymentTarget == DeploymentTarget.CloudPlatform)
            {
                rateGate = new RateGate(10, TimeSpan.FromSeconds(1));
            }
            return new BinanceCoinFuturesRestApiClient(symbolMapper, securityProvider, apiKey, apiSecret, restApiUrl);
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
