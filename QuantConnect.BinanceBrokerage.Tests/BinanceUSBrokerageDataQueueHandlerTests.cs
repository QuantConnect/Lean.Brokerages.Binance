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
using QuantConnect.Tests;

namespace QuantConnect.BinanceBrokerage.Tests
{
    [TestFixture]
    public partial class BinanceUSBrokerageTests
    {
        private static TestCaseData[] TestParameters
        {
            get
            {
                TestGlobals.Initialize();
                var symbol = Symbol.Create("FTMUSDT", SecurityType.Crypto, Market.BinanceUS);

                return new[]
                {
                    // valid parameters, for example
                    new TestCaseData(symbol, Resolution.Tick, false),
                    new TestCaseData(symbol, Resolution.Minute, false),
                    new TestCaseData(symbol, Resolution.Second, false),
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public override void StreamsDataTest(Symbol symbol, Resolution resolution, bool throwsException)
        {
            StreamsData(symbol, resolution, throwsException, Brokerage);
        }
    }
}
