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

namespace QuantConnect.Brokerages.Binance.Constants
{
    /// <summary>
    /// The <see cref="TradeChannels"/> class contains constant values representing trade channels in different markets.
    /// </summary>
    public sealed class TradeChannels
    {
        /// <summary>
        /// The channel name for Spot Trade Streams. Spot Trade Streams push raw trade information, where each trade
        /// involves a unique buyer and seller.
        /// </summary>
        /// <remarks>
        /// Stream Name Format: <c>{symbol}@trade</c>
        /// Update Speed: Real-time
        /// </remarks>
        public const string SpotTradeChannelName = "trade";

        /// <summary>
        /// The channel name for Aggregate Trade Streams. Aggregate Trade Streams push market trade information that is
        /// aggregated for fills with the same price, updating every 100 milliseconds.
        /// </summary>
        /// <remarks>
        /// Stream Name Format: <c>{symbol}@aggTrade</c>
        /// Update Speed: 100ms
        /// </remarks>
        public const string FutureTradeChannelName = "aggTrade";
    }
}
