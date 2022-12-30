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
using System.Linq;
using QuantConnect.ToolBox;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using static QuantConnect.Configuration.ApplicationParser;

namespace QuantConnect.BinanceBrokerage.ToolBox
{
    static class Program
    {
        static void Main(string[] args)
        {
            var optionsObject = ToolboxArgumentParser.ParseArguments(args);
            if (optionsObject.Count == 0)
            {
                PrintMessageAndExit();
            }

            if (!optionsObject.TryGetValue("app", out var targetApp))
            {
                PrintMessageAndExit(1, "ERROR: --app value is required");
            }

            var targetAppName = targetApp?.ToString() ?? throw new ArgumentNullException(nameof(targetApp));
            if (targetAppName.Contains("downloader") || targetAppName.Contains("dl"))
            {
                var fromDate = Parse.DateTimeExact(GetParameterOrExit(optionsObject, "from-date"), "yyyyMMdd-HH:mm:ss");
                var toDate = optionsObject.ContainsKey("to-date")
                    ? Parse.DateTimeExact(optionsObject["to-date"].ToString(), "yyyyMMdd-HH:mm:ss")
                    : DateTime.UtcNow;
                var resolution = optionsObject.ContainsKey("resolution") ? optionsObject["resolution"].ToString() : "";
                var tickers = ToolboxArgumentParser.GetTickers(optionsObject);

                BaseDataDownloader dataDownloader;
                if (targetAppName.Equals("binanceusdownloader") || targetAppName.Equals("mbxusdl"))
                {
                    dataDownloader = new BinanceUSDataDownloader();
                }
                else
                {
                    dataDownloader = new BinanceDataDownloader();
                }
                DownloadData(
                    dataDownloader,
                    tickers,
                    resolution,
                    fromDate,
                    toDate);
                dataDownloader.DisposeSafely();
            }
            else if (targetAppName.Contains("updater") || targetAppName.EndsWith("spu"))
            {
                BinanceExchangeInfoDownloader exchangeInfoDownloader;
                if (targetAppName.Equals("binanceussymbolpropertiesupdater") || targetAppName.Equals("mbxusspu"))
                {
                    exchangeInfoDownloader = new BinanceExchangeInfoDownloader(Market.BinanceUS);
                }
                else
                {
                    exchangeInfoDownloader = new BinanceExchangeInfoDownloader(Market.Binance);
                }

                ExchangeInfoDownloader(exchangeInfoDownloader);
            }
            else
            {
                PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
            }
        }

        /// <summary>
        /// Primary entry point to the program.
        /// </summary>
        public static void DownloadData(BaseDataDownloader downloader, IList<string> tickers, string resolution, DateTime fromDate, DateTime toDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("BinanceDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg BTCUSD");
                Console.WriteLine("--resolution=Minute/Hour/Daily/All");
                Environment.Exit(1);
            }
            try
            {
                var allResolutions = resolution.Equals("all", StringComparison.OrdinalIgnoreCase);
                var castResolution = allResolutions ? Resolution.Minute : (Resolution)Enum.Parse(typeof(Resolution), resolution);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-folder", "../../../Data");

                foreach (var ticker in tickers)
                {
                    // Download the data
                    var symbol = downloader.GetSymbol(ticker);
                    var data = downloader.Get(new DataDownloaderGetParameters(symbol, castResolution, fromDate, toDate));
                    var bars = data.Cast<TradeBar>().ToList();

                    // Save the data (single resolution)
                    var writer = new LeanDataWriter(castResolution, symbol, dataDirectory);
                    writer.Write(bars);

                    if (allResolutions)
                    {
                        // Save the data (other resolutions)
                        foreach (var res in new[] { Resolution.Hour, Resolution.Daily })
                        {
                            var resData = LeanData.AggregateTradeBars(bars, symbol, res.ToTimeSpan());

                            writer = new LeanDataWriter(res, symbol, dataDirectory);
                            writer.Write(resData);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                PrintMessageAndExit(1, $"ERROR: {err.Message}");
            }
        }

        /// <summary>
        /// Endpoint for downloading exchange info
        /// </summary>
        public static void ExchangeInfoDownloader(IExchangeInfoDownloader exchangeInfoDownloader)
        {
            new ExchangeInfoUpdater(exchangeInfoDownloader)
                .Run();
        }
    }
}