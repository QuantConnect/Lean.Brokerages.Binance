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
        // resting prices within the allowed range around the average price (PERCENT_PRICE_BY_SIDE), for an ETHBTC price around 0.032
        private static readonly OrderTestParameters ContingentLimit = new LimitOrderTestParameters(StaticSymbol, 0.06m, 0.02m);
        // a buy stop limit order above the market and a sell stop limit order below it
        private static readonly OrderTestParameters ContingentBuyStopLimit = new StopLimitOrderTestParameters(StaticSymbol, 0.037m, 0.0365m);
        private static readonly OrderTestParameters ContingentSellStopLimit = new StopLimitOrderTestParameters(StaticSymbol, 0.0255m, 0.026m);
        // a buy limit order above the market, which fills right away
        private static readonly OrderTestParameters ContingentMarketableLimit = new LimitOrderTestParameters(StaticSymbol, 0.06m, 0.035m);

        /// <summary>
        /// Spot order lists (OCO, OTO, OTOCO), resting: the prices are far from the market. The working order has to be a limit order
        /// </summary>
        private static TestCaseData[] RestingContingentOrders => new[]
        {
            new TestCaseData(ContingentOrderTestParameters.OneCancelsOther(ContingentLimit, ContingentBuyStopLimit)),
            new TestCaseData(ContingentOrderTestParameters.OneTriggersOther(ContingentLimit, ContingentLimit)),
            new TestCaseData(ContingentOrderTestParameters.Bracket(ContingentLimit, ContingentLimit, ContingentSellStopLimit))
        };

        /// <summary>
        /// Spot order lists where the working limit order fills right away
        /// </summary>
        private static TestCaseData[] TriggeredContingentOrders => new[]
        {
            new TestCaseData(ContingentOrderTestParameters.OneTriggersOther(ContingentMarketableLimit, ContingentLimit)),
            new TestCaseData(ContingentOrderTestParameters.Bracket(ContingentMarketableLimit, ContingentLimit, ContingentSellStopLimit))
        };

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(RestingContingentOrders))]
        public override void ContingentOrdersCancel(ContingentOrderTestParameters parameters)
        {
            base.ContingentOrdersCancel(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(RestingContingentOrders))]
        public override void ContingentOrdersGetOpenOrders(ContingentOrderTestParameters parameters)
        {
            base.ContingentOrdersGetOpenOrders(parameters);
        }

        [Explicit("This test requires a configured and testable Binance practice account")]
        [Test, TestCaseSource(nameof(TriggeredContingentOrders))]
        public override void ContingentOrdersTrigger(ContingentOrderTestParameters parameters)
        {
            base.ContingentOrdersTrigger(parameters);
        }
    }
}
