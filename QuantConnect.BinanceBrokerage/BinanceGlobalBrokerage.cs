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
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages.Binance.Messages;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Create instance of Binance brokerage used for connecting to Binance exchange and processing live data and orders.
    /// </summary>
    /// <remarks>
    /// Cloud: GlobalBinance, Spot
    /// </remarks>
    [BrokerageFactory(typeof(BinanceBrokerageFactory))]
    public sealed class BinanceGlobalBrokerage : BinanceBrokerage
    {
        /// <summary>
        /// Initializes a new instance of the BinanceGlobalBrokerage class.
        /// </summary>
        public BinanceGlobalBrokerage() : base()
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
        public BinanceGlobalBrokerage(string apiKey, string apiSecret, string restApiUrl, string webSocketBaseUrl, IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
            : base(apiKey, apiSecret, restApiUrl, webSocketBaseUrl, algorithm, aggregator, job, Market.Binance)
        {
            _webSocketBaseUrl = "wss://ws-api.binance.com:443/ws-api/v3";
        }

        /// <summary>
        /// Override Connect to initialize WebSocket with Binance Global base url and start reconnect timer for maintaining the connection.
        /// </summary>
        private protected override void Connect(string _)
        {
            Log.Trace("BinanceGlobalBrokerage.Connect(): Connecting...");

            _reconnectTimer.Start();
            WebSocket.Initialize($"{_webSocketBaseUrl}");
            ConnectSync();
        }

        /// <summary>
        /// Override WebSocketOpen to send signature message for subscribing to private user data stream after connection is established.
        /// </summary>
        private protected override void WebSocketOpen(object _, EventArgs eventArgs)
        {
            WebSocket.Send(new SubscribeSignature(ApiKey, ApiSecret).ToJson());
        }
    }
}
