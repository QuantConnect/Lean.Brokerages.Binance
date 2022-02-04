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

namespace QuantConnect.BinanceBrokerage.Messages
{
#pragma warning disable 1591
    public class BalanceEntry
    {
        public string Asset { get; set; }
        public decimal Free { get; set; }
        public decimal Locked { get; set; }
        public virtual decimal Amount { get; }
    }

    public class SpotBalance : BalanceEntry
    {
        public override decimal Amount => Free + Locked;
    }

    public class MarginBalance : BalanceEntry
    {
        public decimal Borrowed { get; set; }
        public decimal NetAsset { get; set; }
        public override decimal Amount => NetAsset;
    }
#pragma warning restore 1591
}
