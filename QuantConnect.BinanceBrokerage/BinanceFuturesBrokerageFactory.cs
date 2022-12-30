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
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.BinanceBrokerage
{
    /// <summary>
    /// Factory method to create USDT Futures Binance Websockets brokerage
    /// </summary>
    public class BinanceFuturesBrokerageFactory : BrokerageFactory
    {
        public static readonly string ApiUrlKeyName = "binance-fapi-url";
        public static readonly string WebSocketUrlKeyName = "binance-fwebsocket-url";

        /// <summary>
        /// Factory constructor
        /// </summary>
        public BinanceFuturesBrokerageFactory() : base(typeof(BinanceFuturesBrokerage))
        {
        }

        /// <summary>
        /// Not required
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// provides brokerage connection data
        /// </summary>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "binance-api-key", Config.Get("binance-api-key")},
            { "binance-api-secret", Config.Get("binance-api-secret")},

            // paper trading available using https://testnet.binancefuture.com
            { ApiUrlKeyName, Config.Get(ApiUrlKeyName, "https://fapi.binance.com")},
            // paper trading available using wss://stream.binancefuture.com/ws
            { WebSocketUrlKeyName, Config.Get(WebSocketUrlKeyName, "wss://fstream.binance.com/ws")},
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new BinanceFuturesBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
            var apiKey = Read<string>(job.BrokerageData, "binance-api-key", errors);
            var apiSecret = Read<string>(job.BrokerageData, "binance-api-secret", errors);
            var apiUrl = Read<string>(job.BrokerageData, ApiUrlKeyName, errors);
            var wsUrl = Read<string>(job.BrokerageData, WebSocketUrlKeyName, errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new ArgumentException(string.Join(Environment.NewLine, errors));
            }

            var brokerage = new BinanceFuturesBrokerage(
                apiKey,
                apiSecret,
                apiUrl,
                wsUrl,
                algorithm,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"), forceTypeNameOnExisting: false),
                job);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
