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

namespace QuantConnect.BinanceBrokerage.Messages
{
#pragma warning disable 1591

    public class BestBidAskQuote
    {
        [JsonProperty("u")]
        public long OrderBookUpdateId { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("b")]
        public decimal BestBidPrice { get; set; }

        [JsonProperty("B")]
        public decimal BestBidSize { get; set; }

        [JsonProperty("a")]
        public decimal BestAskPrice { get; set; }

        [JsonProperty("A")]
        public decimal BestAskSize { get; set; }
    }

#pragma warning restore 1591
}
