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

using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Binance.Messages;

namespace QuantConnect.Brokerages.Binance.Converters
{
    /// <summary>
    /// Converts order responses into <see cref="AlgoOrder"/> or <see cref="NewOrder"/>.
    /// </summary>
    public sealed class NewOrderResponseConverter : OrderResponseConverterBase
    {
        protected override Order CreateOrder(JToken token)
        {
            return token["algoId"] != null ? new AlgoOrder() : new NewOrder();
        }
    }
}
