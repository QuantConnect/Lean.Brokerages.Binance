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

namespace QuantConnect.Brokerages.Binance.Messages
{
    /// <summary>
    /// Base class for WebSocket market-data channel subscription request messages.
    /// </summary>
    public abstract class BaseChannelRequest
    {
        [JsonProperty("method")]
        public abstract string Method { get; }

        [JsonProperty("params")]
        public string[] Params { get; }

        [JsonProperty("id")]
        public long Id { get; }

        protected BaseChannelRequest(long id, string[] @params)
        {
            Id = id;
            Params = @params;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        protected static string Stream(string brokerageSymbol, string channel)
        {
            return $"{brokerageSymbol.ToLowerInvariant()}@{channel}";
        }
    }

    public abstract class BaseSubscribeChannelRequest : BaseChannelRequest
    {
        public override string Method => "SUBSCRIBE";

        protected BaseSubscribeChannelRequest(long id, string[] @params)
            : base(id, @params)
        { }
    }

    public abstract class BaseUnSubscribeChannelRequest : BaseChannelRequest
    {
        public override string Method => "UNSUBSCRIBE";

        protected BaseUnSubscribeChannelRequest(long id, string[] @params)
            : base(id, @params)
        { }
    }

    /// <summary>Subscribes the aggTrade channel.</summary>
    public sealed class TradeChannelSubscribeRequest : BaseSubscribeChannelRequest
    {
        public TradeChannelSubscribeRequest(long id, string brokerageSymbol, string tradeChannelName)
            : base(id, [Stream(brokerageSymbol, tradeChannelName)])
        { }
    }

    /// <summary>Unsubscribes the aggTrade channel.</summary>
    public sealed class TradeChannelUnsubscribeRequest : BaseUnSubscribeChannelRequest
    {

        public TradeChannelUnsubscribeRequest(long id, string brokerageSymbol, string tradeChannelName)
            : base(id, [Stream(brokerageSymbol, tradeChannelName)])
        { }
    }

    /// <summary>Subscribes the bookTicker channel.</summary>
    public sealed class QuoteChannelSubscribeRequest : BaseSubscribeChannelRequest
    {
        public QuoteChannelSubscribeRequest(long id, string brokerageSymbol)
            : base(id, [Stream(brokerageSymbol, "bookTicker")])
        { }
    }

    /// <summary>Unsubscribes the bookTicker channel.</summary>
    public sealed class QuoteChannelUnsubscribeRequest : BaseUnSubscribeChannelRequest
    {
        public QuoteChannelUnsubscribeRequest(long id, string brokerageSymbol)
            : base(id, [Stream(brokerageSymbol, "bookTicker")])
        { }
    }

    /// <summary>Subscribes both the trade and bookTicker channels (spot default).</summary>
    public sealed class TradeAndQuoteChannelSubscribeRequest : BaseSubscribeChannelRequest
    {
        public TradeAndQuoteChannelSubscribeRequest(long id, string brokerageSymbol, string tradeChannelName)
            : base(id, [Stream(brokerageSymbol, tradeChannelName), Stream(brokerageSymbol, "bookTicker")])
        { }
    }

    /// <summary>Unsubscribes both the trade and bookTicker channels (spot default).</summary>
    public sealed class TradeAndQuoteChannelUnsubscribeRequest : BaseUnSubscribeChannelRequest
    {
        public TradeAndQuoteChannelUnsubscribeRequest(long id, string brokerageSymbol, string tradeChannelName)
            : base(id, [Stream(brokerageSymbol, tradeChannelName), Stream(brokerageSymbol, "bookTicker")])
        { }
    }
}