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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.ToolBox;
using System.Collections.Generic;
using QuantConnect.BinanceBrokerage.ToolBox.Models;

namespace QuantConnect.BinanceBrokerage.ToolBox
{
    /// <summary>
    /// Binance implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class BinanceExchangeInfoDownloader : IExchangeInfoDownloader
    {
        private const string _binanceFutureCryptoApiEndpoint = "https://fapi.binance.com/fapi";
        private const string _binanceFutureCoinApiEndpoint = "https://dapi.binance.com/dapi";
        private const string _binanceApiEndpoint = "https://api.binance.com";
        private const string _binanceUsApiEndpoint = "https://api.binance.us";
        private readonly string _restApiHost;

        /// <summary>
        /// Market name
        /// </summary>
        public string Market { get; }


        public BinanceExchangeInfoDownloader(string market)
        {
            Market = market;
            _restApiHost = market.Equals(QuantConnect.Market.Binance, StringComparison.OrdinalIgnoreCase)
                ? _binanceApiEndpoint
                : _binanceUsApiEndpoint;
        }

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            var data = Extensions.DownloadData($"{_restApiHost}/api/v3/exchangeInfo");

            foreach (var symbol in JsonConvert.DeserializeObject<ExchangeInfo>(data).Symbols)
            {
                if (!symbol.IsSpotTradingAllowed && !symbol.IsMarginTradingAllowed)
                {
                    // exclude derivatives
                    continue;
                }

                var priceFilter = symbol.Filters
                    .First(f => f.GetValue("filterType").ToString() == "PRICE_FILTER")
                    .GetValue("tickSize").ToObject<decimal>().NormalizeToStr();

                var lotFilter = symbol.Filters
                    .First(f => f.GetValue("filterType").ToString() == "LOT_SIZE");
                var stepSize = lotFilter.GetValue("stepSize").ToObject<decimal>().NormalizeToStr();

                var minNotional = symbol.Filters
                    .First(f => f.GetValue("filterType").ToString() == "MIN_NOTIONAL" || f.GetValue("filterType").ToString() == "NOTIONAL");
                var minOrderSize = minNotional.GetValue("minNotional").ToObject<decimal>().NormalizeToStr();

                yield return $"{Market.ToLowerInvariant()},{symbol.Name},crypto,{symbol.Name},{symbol.QuoteAsset},1,{priceFilter},{stepSize},{symbol.Name},{minOrderSize}";
            }

            if(Market == QuantConnect.Market.Binance)
            {
                foreach (var endpoint in new[] { _binanceFutureCryptoApiEndpoint, _binanceFutureCoinApiEndpoint })
                {
                    var futureData = Extensions.DownloadData($"{endpoint}/v1/exchangeInfo");

                    foreach (var symbol in JsonConvert.DeserializeObject<ExchangeInfo>(futureData).Symbols)
                    {
                        if (!symbol.ContractType.Equals("PERPETUAL", StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        var priceFilter = symbol.Filters
                            .First(f => f.GetValue("filterType").ToString() == "PRICE_FILTER")
                            .GetValue("tickSize").ToObject<decimal>().NormalizeToStr();

                        var lotFilter = symbol.Filters
                            .First(f => f.GetValue("filterType").ToString() == "LOT_SIZE");
                        var stepSize = lotFilter.GetValue("stepSize").ToObject<decimal>().NormalizeToStr();

                        var contractSize = 1;
                        if (symbol.ContractSize != 0)
                        {
                            // Coin-Margined futures have a contract multiplier of 100 or 10
                            // USDT margined futures do not have a contract multiplier
                            contractSize = symbol.ContractSize;
                        }

                        yield return $"{Market.ToLowerInvariant()},{symbol.Name.RemoveFromEnd("_PERP")},cryptofuture,{symbol.Name},{symbol.QuoteAsset},{contractSize},{priceFilter},{stepSize},{symbol.Name}";
                    }
                }
            }
        }
    }
}
