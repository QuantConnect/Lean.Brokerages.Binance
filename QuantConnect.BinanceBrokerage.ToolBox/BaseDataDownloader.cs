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

using NodaTime;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.BinanceBrokerage.ToolBox
{
    /// <summary>
    /// Binance Downloader class
    /// </summary>
    public abstract class BaseDataDownloader : IDataDownloader, IDisposable
    {
        protected BinanceBrokerage Brokerage { get; init; }
        protected SymbolPropertiesDatabaseSymbolMapper SymbolMapper { get; init; }
        protected virtual string MarketName { get; }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            if (tickType != TickType.Trade)
            {
                return Enumerable.Empty<BaseData>();
            }

            if (resolution == Resolution.Tick || resolution == Resolution.Second)
            {
                throw new ArgumentException($"Resolution not available: {resolution}");
            }

            if (!SymbolMapper.IsKnownLeanSymbol(symbol))
            {
                throw new ArgumentException($"The ticker {symbol.Value} is not available.");
            }

            if (endUtc < startUtc)
            {
                throw new ArgumentException("The end date must be greater or equal than the start date.");
            }

            var historyRequest = new HistoryRequest(
                startUtc,
                endUtc,
                typeof(TradeBar),
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                DateTimeZone.Utc,
                resolution,
                false,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade);

            return Brokerage.GetHistory(historyRequest) ?? Enumerable.Empty<BaseData>();
        }

        /// <summary>
        /// Creates Lean Symbol
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns></returns>
        internal Symbol GetSymbol(string ticker)
        {
            return SymbolMapper.GetLeanSymbol(ticker, SecurityType.Crypto, MarketName);
        }

        public void Dispose()
        {
            Brokerage.Disconnect();
            Brokerage.DisposeSafely();
        }
    }
}
