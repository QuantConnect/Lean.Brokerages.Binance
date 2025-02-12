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

using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages.Binance.Messages;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance Coin Futures REST API implementation
    /// </summary>
    public class BinanceCoinFuturesRestApiClient : BinanceFuturesRestApiClient
    {
        private const string _prefix = "/dapi/v1";

        /// <summary>
        /// The Api prefix
        /// </summary>
        /// <remarks>Depends on SPOT,MARGIN, Futures trading</remarks>
        protected override string ApiPrefix => _prefix;

        /// <summary>
        /// The websocket prefix
        /// </summary>
        /// <remarks>Depends on SPOT,MARGIN, Futures trading</remarks>
        protected override string WsPrefix => _prefix;

        public BinanceCoinFuturesRestApiClient(
            ISymbolMapper symbolMapper,
            ISecurityProvider securityProvider,
            string apiKey,
            string apiSecret,
            string restApiUrl,
            RateGate restRateLimiter = null
            )
            : base(symbolMapper, securityProvider, apiKey, apiSecret, restApiUrl, restRateLimiter)
        {
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns>The list of all account holdings</returns>
        public override List<Holding> GetAccountHoldings()
        {
            return GetAccountHoldings(_prefix);
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override BalanceEntry[] GetCashBalance()
        {
            return GetCashBalance(_prefix);
        }
    }
}
