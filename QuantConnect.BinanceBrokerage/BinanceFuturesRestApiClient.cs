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

using Newtonsoft.Json;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.BinanceBrokerage.Converters;
using System.Collections.Generic;
using QuantConnect.BinanceBrokerage.Messages;
using RestSharp;
using System.Net;
using System;
using System.Linq;

namespace QuantConnect.BinanceBrokerage
{
    /// <summary>
    /// Binance USDT Futures REST API implementation
    /// </summary>
    public class BinanceFuturesRestApiClient : BinanceBaseRestApiClient
    {
        private const string _prefix = "/fapi/v1";

        protected override JsonConverter CreateAccountConverter() => new FuturesAccountConverter();

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

        /// <summary>
        /// The user data stream endpoint
        /// </summary>
        protected override string UserDataStreamEndpoint => $"{WsPrefix}/listenKey";

        protected override string GetBaseDataEndpoint() => ApiPrefix;

        /// <summary>
        /// Create a new instance
        /// </summary>
        public BinanceFuturesRestApiClient(
            ISymbolMapper symbolMapper,
            ISecurityProvider securityProvider,
            string apiKey,
            string apiSecret,
            string restApiUrl
            )
            : base(symbolMapper, securityProvider, apiKey, apiSecret, restApiUrl)
        {
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns>The list of all account holdings</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var queryString = $"timestamp={GetNonce()}";
            var endpoint = $"{ApiPrefix}/account?{queryString}&signature={AuthenticationToken(queryString)}";
            var request = new RestRequest(endpoint, Method.GET);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            return JsonConvert
                .DeserializeObject<FuturesAccountInformation>(response.Content, CreateAccountConverter())
                .Positions
                .Where(p => p.PositionAmt != 0)
                .Select(x => new Holding
                {
                    Symbol = SymbolMapper.GetLeanSymbol(x.Symbol, SecurityType.CryptoFuture, Market.Binance),
                    AveragePrice = x.EntryPrice,
                    Quantity = x.PositionAmt,
                })
                .ToList();
        }
    }
}
