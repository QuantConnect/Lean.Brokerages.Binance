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
    /// Factory method to create Binance.US brokerage
    /// </summary>
    public class BinanceUSBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public BinanceUSBrokerageFactory() : base(typeof(BinanceUSBrokerage))
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
        public override Dictionary<string, string> BrokerageData => new()
        {
            { "binanceus-api-key", Config.Get("binanceus-api-key")},
            { "binanceus-api-secret", Config.Get("binanceus-api-secret")},
            // paper trading available using https://testnet.binance.vision
            { "binanceus-api-url", Config.Get("binanceus-api-url", "https://api.binance.us")},
            // paper trading available using wss://testnet.binance.vision/ws
            { "binanceus-websocket-url", Config.Get("binanceus-websocket-url", "wss://stream.binance.us:9443/ws")},

            // load holdings if available
            { "live-holdings", Config.Get("live-holdings")},
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new BinanceUSBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
            var apiKey = Read<string>(job.BrokerageData, "binanceus-api-secret", errors);
            var apiSecret = Read<string>(job.BrokerageData, "binanceus-api-key", errors);
            var apiUrl = Read<string>(job.BrokerageData, "binanceus-api-url", errors);
            var wsUrl = Read<string>(job.BrokerageData, "binanceus-websocket-url", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new ArgumentException(string.Join(Environment.NewLine, errors));
            }
            
            var brokerage = new BinanceUSBrokerage(
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
