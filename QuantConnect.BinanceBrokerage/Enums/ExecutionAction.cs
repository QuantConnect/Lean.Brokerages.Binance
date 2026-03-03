/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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
    /// Describes what the brokerage should do after parsing a WebSocket execution event.
    /// </summary>
    public enum ExecutionAction
    {
        /// <summary>
        /// No action — routine status update, duplicate NEW, or a stop-trigger-consumed EXPIRED.
        /// </summary>
        None,

        /// <summary>
        /// A trade or real-expiry event: call <see cref="BinanceBrokerage.OnFillOrder"/>.
        /// </summary>
        Fill,

        /// <summary>
        /// An order was canceled externally or by REST: call <see cref="BinanceBrokerage.OnFillOrder"/>.
        /// </summary>
        Cancel,

        /// <summary>
        /// A NEW order that Lean does not yet know about (placed externally, e.g. Binance Web UI):
        /// call <see cref="BinanceBrokerage.OnNewBrokerageOrderNotification"/>.
        /// </summary>
        NewOrder,
    }
}
