/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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

namespace QuantConnect.Brokerages.Binance.Enums
{
    /// <summary>
    /// Maps the numeric <c>er</c> field in a Binance Futures
    /// <c>ORDER_TRADE_UPDATE</c> WebSocket event to a human-readable expiry reason.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>er</c> field is only present in Futures user-data stream events.
    /// A value of <see cref="None"/> (0) means no error occurred — for <c>x:EXPIRED</c>
    /// events on STOP / STOP_MARKET orders this indicates the stop trigger was consumed
    /// normally and a child order will follow in a separate <c>x:NEW</c> event.
    /// Any non-zero value means the order genuinely failed.
    /// </para>
    /// <para>
    /// Full specification:
    /// <see href="https://developers.binance.com/docs/derivatives/usds-margined-futures/user-data-streams/Event-Order-Update">
    /// Binance Futures — Event: Order Update
    /// </see>.
    /// </para>
    /// </remarks>
    public enum FuturesExpiredReason
    {
        /// <summary>
        /// No error. For STOP/STOP_MARKET orders this means the stop price was hit and
        /// the trigger order is consumed; a child limit/market order will arrive separately.
        /// </summary>
        None = 0,

        /// <summary>
        /// Order expired to prevent users from inadvertently trading against themselves.
        /// </summary>
        SelfTradePrevention = 1,

        /// <summary>
        /// IOC (Immediate-Or-Cancel) order could not be filled completely;
        /// the remaining quantity was canceled.
        /// </summary>
        IocCanceled = 2,

        /// <summary>
        /// IOC order could not be filled completely due to self-trade prevention;
        /// the remaining quantity was canceled.
        /// </summary>
        IocCanceledSelfTrade = 3,

        /// <summary>
        /// Order was canceled because it was knocked out by a higher-priority
        /// market (RO) order or reversed positions would have been opened.
        /// </summary>
        KnockedOut = 4,

        /// <summary>
        /// Order expired because the account was liquidated.
        /// </summary>
        Liquidated = 5,

        /// <summary>
        /// GTE (Good-Till-Expiry) order expired because the condition was not satisfied.
        /// </summary>
        GteConditionUnsatisfied = 6,

        /// <summary>
        /// Order was canceled because the symbol was delisted.
        /// </summary>
        SymbolDelisted = 7,

        /// <summary>
        /// The initial stop order expired after the stop price was triggered but
        /// the resulting FOK limit order could not be filled.
        /// </summary>
        StopTriggeredFokRejected = 8,

        /// <summary>
        /// Market order could not be filled completely; the remaining quantity was canceled.
        /// </summary>
        MarketOrderCanceled = 9,
    }
}
