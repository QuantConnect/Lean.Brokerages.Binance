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

using QuantConnect.Brokerages.Binance.Constants;
using QuantConnect.Brokerages.Binance.Messages;
using QuantConnect.Data;
using QuantConnect.Util;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Subscription manager for Binance USDS-margined futures that fans each symbol
    /// subscription across two separate WebSocket endpoint families:
    /// <list type="bullet">
    ///   <item><c>{host}/market/ws</c> — aggTrade stream</item>
    ///   <item><c>{host}/public/ws</c> — bookTicker stream</item>
    /// </list>
    /// </summary>
    public sealed class BinanceFuturesSubscriptionManager : DataQueueHandlerSubscriptionManager
    {
        private readonly BrokerageMultiWebSocketSubscriptionManager _tradePool;
        private readonly BrokerageMultiWebSocketSubscriptionManager _quotePool;
        private readonly ISymbolMapper _symbolMapper;
        private readonly RateGate _rateLimiter;
        private readonly Func<long> _getNextRequestId;

        /// <param name="privateWsUrl">
        /// The private user-data WebSocket URL, e.g. <c>wss://fstream.binance.com/private/ws</c>.
        /// The trade and quote pool URLs are derived by replacing <c>/private/</c> with
        /// <c>/market/</c> and <c>/public/</c> respectively.
        /// </param>
        public BinanceFuturesSubscriptionManager(
            string privateWsUrl,
            int maxSymbolsPerConnection,
            int maxWebSocketConnections,
            Dictionary<Symbol, int> symbolWeights,
            Action<WebSocketMessage> onDataMessage,
            ISymbolMapper symbolMapper,
            RateGate rateLimiter,
            Func<long> getNextRequestId)
        {
            _symbolMapper = symbolMapper;
            _rateLimiter = rateLimiter;
            _getNextRequestId = getNextRequestId;

            var tradeUrl = privateWsUrl.Replace("/private/", "/market/");
            var quoteUrl = privateWsUrl.Replace("/private/", "/public/");

            _tradePool = new BrokerageMultiWebSocketSubscriptionManager(
                tradeUrl,
                maxSymbolsPerConnection,
                maxWebSocketConnections,
                symbolWeights,
                () => new BinanceWebSocketWrapper(null),
                SubscribeTrade,
                UnsubscribeTrade,
                onDataMessage,
                new TimeSpan(23, 45, 0));

            _quotePool = new BrokerageMultiWebSocketSubscriptionManager(
                quoteUrl,
                maxSymbolsPerConnection,
                maxWebSocketConnections,
                symbolWeights,
                () => new BinanceWebSocketWrapper(null),
                SubscribeQuote,
                UnsubscribeQuote,
                onDataMessage,
                new TimeSpan(23, 45, 0));
        }

        public override void Subscribe(SubscriptionDataConfig dataConfig)
        {
            _tradePool.Subscribe(dataConfig);
            _quotePool.Subscribe(dataConfig);
        }

        public override void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _tradePool.Unsubscribe(dataConfig);
            _quotePool.Unsubscribe(dataConfig);
        }

        public override void Dispose()
        {
            _tradePool.DisposeSafely();
            _quotePool.DisposeSafely();
        }

        private bool SubscribeTrade(IWebSocket ws, Symbol symbol)
        {
            return Send(ws, symbol, (bs, requestId) => new TradeChannelSubscribeRequest(requestId, bs, TradeChannels.FutureTradeChannelName));
        }

        private bool UnsubscribeTrade(IWebSocket ws, Symbol symbol)
        {
            return Send(ws, symbol, (bs, requestId) => new TradeChannelUnsubscribeRequest(requestId, bs, TradeChannels.FutureTradeChannelName));
        }

        private bool SubscribeQuote(IWebSocket ws, Symbol symbol)
        {
            return Send(ws, symbol, (bs, requestId) => new QuoteChannelSubscribeRequest(requestId, bs));
        }

        private bool UnsubscribeQuote(IWebSocket ws, Symbol symbol)
        {
            return Send(ws, symbol, (bs, requestId) => new QuoteChannelUnsubscribeRequest(requestId, bs));
        }

        private bool Send<T>(IWebSocket ws, Symbol symbol, Func<string, long, T> requestMsg) where T : BaseChannelRequest
        {
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var msg = requestMsg(brokerageSymbol, _getNextRequestId());
            _rateLimiter.WaitToProceed();
            ws.Send(msg.ToJson());
            return true;
        }

        #region Not used since we fan out to separate WebSocket endpoints for trades vs quotes
        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            return true;
        }

        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            return true;
        }

        protected override string ChannelNameFromTickType(TickType tickType)
        {
            return Channel.Single;
        }
        #endregion
    }
}
