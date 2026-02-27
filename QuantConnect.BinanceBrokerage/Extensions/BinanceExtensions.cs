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
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using QuantConnect.Brokerages.Binance.Messages;

namespace QuantConnect.Brokerages.Binance.Extensions;

/// <summary>
/// Extension methods specific to using the Binance API
/// </summary>
public static class BinanceExtensions
{
    /// <summary>
    /// Renames a key in the dictionary.
    /// </summary>
    public static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey fromKey, TKey toKey)
    {
        dictionary[toKey] = dictionary[fromKey];
        dictionary.Remove(fromKey);
    }


    /// <summary>
    /// Timestamp in milliseconds
    /// </summary>
    /// <returns></returns>
    public static long GetNonce()
    {
        return (long)Time.DateTimeToUnixTimeStampMilliseconds(DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a signature for signed endpoints
    /// </summary>
    /// <param name="apiSecret">the api secret key for the request</param>
    /// <param name="payload">the body of the request</param>
    /// <returns>a token representing the request params</returns>
    public static string GetAuthenticationToken(string apiSecret, string payload)
    {
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(apiSecret), Encoding.UTF8.GetBytes(payload)).ToHexString();
    }

    /// <summary>
    /// Maps a WebSocket <see cref="Execution"/> event to a Binance <see cref="OpenOrder"/> DTO,
    /// using the order-level fields (<see cref="Execution.Price"/>, <see cref="Execution.StopPrice"/>,
    /// <see cref="Execution.OrderType"/>) rather than the trade-level fields.
    /// </summary>
    public static OpenOrder MapExecutionToOpenOrder(this Execution execution)
    {
        return new OpenOrder
        {
            Id = execution.OrderId,
            Symbol = execution.Symbol,
            Price = execution.Price,
            // Spot sends stop price as "P"; Futures sends it as "sp"
            StopPrice = execution.StopPrice != 0 ? execution.StopPrice : execution.FuturesStopPrice,
            OriginalAmount = execution.OriginalAmount,
            ExecutedAmount = execution.LastExecutedQuantity,
            Status = execution.OrderStatus,
            Type = execution.OrderType,
            Side = execution.Side,
            Time = execution.TransactionTime
        };
    }
}
