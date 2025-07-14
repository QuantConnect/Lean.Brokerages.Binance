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
using QuantConnect.ToolBox;
using QuantConnect.Configuration;
using static QuantConnect.Configuration.ApplicationParser;

namespace QuantConnect.Brokerages.Binance.ToolBox
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

            if (targetAppName.Contains("updater") || targetAppName.EndsWith("spu"))
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
        /// Endpoint for downloading exchange info
        /// </summary>
        public static void ExchangeInfoDownloader(IExchangeInfoDownloader exchangeInfoDownloader)
        {
            new ExchangeInfoUpdater(exchangeInfoDownloader)
                .Run();
        }
    }
}