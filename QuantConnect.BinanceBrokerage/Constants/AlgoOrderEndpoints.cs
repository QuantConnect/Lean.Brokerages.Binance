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
    /// Provides constant endpoint paths for Binance algorithmic order operations.
    /// </summary>
    public sealed class AlgoOrderEndpoints
    {
        /// <summary>
        /// Endpoint for placing or managing algorithmic orders.
        /// </summary>
        /// <remarks>Uses in <see cref="BinanceBrokerage.PlaceOrder"/> and <see cref="BinanceBrokerage.CancelOrder"/></remarks>
        public const string Trade = "algoOrder";

        /// <summary>
        /// Endpoint for retrieving open algorithmic orders.
        /// </summary>
        /// <remarks>Uses in <see cref="BinanceBrokerage.GetOpenOrders"/></remarks>
        public const string OpenOrders = "openAlgoOrders";
    }
}
