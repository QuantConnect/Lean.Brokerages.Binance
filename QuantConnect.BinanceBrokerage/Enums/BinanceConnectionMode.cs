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

namespace QuantConnect.Brokerages.Binance.Enums
{
    /// <summary>
    /// Identifies how the user data WebSocket stream is authenticated and maintained.
    /// </summary>
    public enum BinanceConnectionMode
    {
        /// <summary>
        /// Standard Spot (US), Futures, and Coin-Futures:
        /// a listen key is obtained via REST and appended to the WS URL,
        /// keep-alive ping sent every 30 minutes.
        /// </summary>
        StandardListenKey,

        /// <summary>
        /// Global Spot WS-API v3:
        /// uses a separate order endpoint; a <see cref="Messages.SubscribeSignature"/> message
        /// is sent on open instead of a listen key; keep-alive ping every 30 minutes.
        /// </summary>
        WsApiSignature,

        /// <summary>
        /// Cross-Margin account:
        /// a listen key is obtained via REST then delivered to the stream as a
        /// <see cref="Messages.SubscribeListenToken"/> message on open;
        /// keep-alive re-creates the token every 23.5 hours.
        /// </summary>
        CrossMarginToken,
    }
}
