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

using NUnit.Framework;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Brokerages.Binance.Tests
{
    public partial class BinanceBrokerageTests
    {
        private static readonly OrderTestParameters ContingentLimit = new LimitOrderTestParameters(StaticSymbol, HighPrice, LowPrice);
        private static readonly OrderTestParameters ContingentStopLimit = new StopLimitOrderTestParameters(StaticSymbol, HighPrice, LowPrice);

        /// <summary>
        /// Spot order lists (OCO, OTO, OTOCO), resting: the prices are far from the market. The working order has to be a limit order
        /// </summary>
        private static TestCaseData[] RestingContingentOrders => new[]
        {
            new TestCaseData(ContingentOrderTestParameters.OneCancelsOther(ContingentLimit, ContingentStopLimit)),
            new TestCaseData(ContingentOrderTestParameters.OneTriggersOther(ContingentLimit, ContingentLimit)),
            new TestCaseData(ContingentOrderTestParameters.Bracket(ContingentLimit, ContingentLimit, ContingentStopLimit))
        };

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(RestingContingentOrders))]
        public override void ContingentOrdersCancel(ContingentOrderTestParameters parameters)
        {
            base.ContingentOrdersCancel(parameters);
        }
    }
}
