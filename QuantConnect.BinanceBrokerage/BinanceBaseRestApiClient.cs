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
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Binance.Messages;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance REST API base implementation
    /// </summary>
    public abstract class BinanceBaseRestApiClient : IDisposable
    {
        private readonly ISecurityProvider _securityProvider;
        private readonly IRestClient _restClient;
        private readonly object _listenKeyLocker = new();

        /// <summary>
        /// The rate limiter used to control the frequency of REST API requests.
        /// </summary>
        private readonly RateGate _restRateLimiter;

        /// <summary>
        /// Provides a cancellation mechanism for ongoing HTTP requests.
        /// Used to signal request termination when needed, such as during shutdown
        /// or when exceeding retry limits.
        /// </summary>
        private readonly CancellationTokenSource _cancellationToken = new();

        /// <summary>
        /// The symbol mapper instance
        /// </summary>
        protected ISymbolMapper SymbolMapper { get; }

        /// <summary>
        /// The Api prefix
        /// </summary>
        /// <remarks>Depends on SPOT,MARGIN, Futures trading</remarks>
        protected virtual string ApiPrefix { get; }

        /// <summary>
        /// The websocket prefix
        /// </summary>
        /// <remarks>Depends on SPOT,MARGIN, Futures trading</remarks>
        protected virtual string WsPrefix { get; }

        /// <summary>
        /// The user data stream endpoint
        /// </summary>
        protected virtual string UserDataStreamEndpoint => $"{WsPrefix}/userDataStream";

        /// <summary>
        /// Ticker Price Change Statistics Endpoint
        /// </summary>
        protected virtual string TickerPriceChangeStatisticsEndpoint => $"{ApiPrefix}/ticker/24hr";

        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        public event EventHandler<BinanceOrderSubmitEventArgs> OrderSubmit;

        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        public event EventHandler<OrderEvent> OrderStatusChanged;

        /// <summary>
        /// Event that fires when an error is encountered in the brokerage
        /// </summary>
        public event EventHandler<BrokerageMessageEvent> Message;

        /// <summary>
        /// Key Header
        /// </summary>
        public readonly string KeyHeader = "X-MBX-APIKEY";

        /// <summary>
        /// The api secret
        /// </summary>
        protected string ApiSecret;

        /// <summary>
        /// The api key
        /// </summary>
        protected string ApiKey;

        /// <summary>
        /// Represents UserData Session listen key
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinanceBaseRestApiClient"/> class.
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="apiKey">The Binance API key.</param>
        /// <param name="apiSecret">The Binance API secret.</param>
        /// <param name="restApiUrl">The Binance REST API URL.</param>
        /// <param name="restRateLimiter">The rate limiter for REST API requests. Defaults to 10 requests per second if not provided.</param>
        public BinanceBaseRestApiClient(
            ISymbolMapper symbolMapper,
            ISecurityProvider securityProvider,
            string apiKey,
            string apiSecret,
            string restApiUrl,
            RateGate restRateLimiter)
        {
            SymbolMapper = symbolMapper;
            _securityProvider = securityProvider;
            _restClient = new RestClient(restApiUrl);
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            _restRateLimiter = restRateLimiter;
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns>The list of all account holdings</returns>
        public virtual List<Holding> GetAccountHoldings()
        {
            return new List<Holding>();
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public virtual BalanceEntry[] GetCashBalance()
        {
            return GetCashBalance(ApiPrefix);
        }

        /// <summary>
        /// Resolves the REST endpoint used to place an order for the given order type.
        /// </summary>
        /// <param name="orderType">The type of order being placed.</param>
        /// <returns>
        /// The endpoint path used for placing the order.
        /// </returns>
        protected virtual string ResolveOrderEndpoint(OrderType orderType)
        {
            return "order";
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        protected BalanceEntry[] GetCashBalance(string apiPrefix)
        {
            var request = new RestRequest($"{apiPrefix}/account", Method.GET);

            var response = ExecuteRestRequestWithSignature(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            return JsonConvert
                .DeserializeObject<AccountInformation>(response.Content, CreateAccountConverter())
                .Balances
                .Where(s => s.Amount != 0)
                .ToArray();
        }

        /// <summary>
        /// Deserialize Binance account information
        /// </summary>
        /// <param name="content">API response content</param>
        /// <returns>Cash or Margin Account</returns>
        protected abstract JsonConverter CreateAccountConverter();

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Messages.OpenOrder> GetOpenOrders()
        {
            var request = new RestRequest($"{ApiPrefix}/openOrders", Method.GET);

            var response = ExecuteRestRequestWithSignature(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            return JsonConvert.DeserializeObject<Messages.OpenOrder[]>(response.Content);
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public bool PlaceOrder(Order order)
        {
            var body = CreateOrderBody(order);

            var request = new RestRequest($"{ApiPrefix}/{ResolveOrderEndpoint(order.Type)}", Method.POST);

            var response = ExecuteRestRequestWithSignature(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = JsonConvert.DeserializeObject<Messages.NewOrder>(response.Content);

                if (string.IsNullOrEmpty(raw?.Id))
                {
                    var errorMessage = $"Error parsing response from place order: {response.Content}";
                    OnOrderEvent(new OrderEvent(
                        order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Binance Order Event")
                    { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (int)response.StatusCode, errorMessage));

                    return true;
                }

                OnOrderSubmit(raw, order);
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Content}";
            OnOrderEvent(new OrderEvent(
                order,
                DateTime.UtcNow,
                OrderFee.Zero,
                response.Content)
            { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            return true;
        }

        /// <summary>
        /// Create account new order body payload
        /// </summary>
        /// <param name="order">Lean order</param>
        protected virtual IDictionary<string, object> CreateOrderBody(Order order)
        {
            // supported time in force values {GTC, IOC, FOK}
            // use GTC as LEAN doesn't support others yet
            var body = new Dictionary<string, object>
            {
                { "symbol", SymbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "side", ConvertOrderDirection(order.Direction) }
            };

            switch (order)
            {
                case LimitOrder limitOrder:
                    body["type"] = (order.Properties as BinanceOrderProperties)?.PostOnly == true
                        ? "LIMIT_MAKER"
                        : "LIMIT";
                    body["price"] = limitOrder.LimitPrice.ToString(CultureInfo.InvariantCulture);
                    // timeInForce is not required for LIMIT_MAKER
                    if (Equals(body["type"], "LIMIT"))
                        body["timeInForce"] = "GTC";
                    break;
                case MarketOrder:
                    body["type"] = "MARKET";
                    break;
                case StopLimitOrder stopLimitOrder:
                    var tickerPrice = GetTickerPrice(order);
                    var stopPrice = stopLimitOrder.StopPrice;
                    if (order.Direction == OrderDirection.Sell)
                    {
                        if (order.SecurityType == SecurityType.CryptoFuture)
                        {
                            body["type"] = stopPrice <= tickerPrice ? "STOP" : "TAKE_PROFIT";
                        }
                        else
                        {
                            body["type"] = stopPrice <= tickerPrice ? "STOP_LOSS_LIMIT" : "TAKE_PROFIT_LIMIT";
                        }
                    }
                    else if (order.SecurityType == SecurityType.CryptoFuture)
                    {
                        body["type"] = stopPrice <= tickerPrice ? "TAKE_PROFIT" : "STOP";
                    }
                    else
                    {
                        body["type"] = stopPrice <= tickerPrice ? "TAKE_PROFIT_LIMIT" : "STOP_LOSS_LIMIT";
                    }

                    body["timeInForce"] = "GTC";
                    body["stopPrice"] = stopPrice.ToStringInvariant();
                    body["price"] = stopLimitOrder.LimitPrice.ToStringInvariant();
                    break;
                case StopMarketOrder stopMarketOrder:
                    if (order.SecurityType == SecurityType.Crypto)
                    {
                        throw new NotSupportedException($"{nameof(BinanceBaseRestApiClient)}.{nameof(CreateOrderBody)}: Unsupported order type: {order.Type} for {SecurityType.Crypto}");
                    }
                    body["type"] = "STOP_MARKET";
                    body["stopPrice"] = stopMarketOrder.StopPrice.ToStringInvariant();

                    break;
                default:
                    throw new NotSupportedException($"{nameof(BinanceBaseRestApiClient)}.{nameof(CreateOrderBody)}: Unsupported order type: {order.Type}");
            }

            return body;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public bool CancelOrder(Order order)
        {
            var success = new List<bool>();
            IDictionary<string, object> body = new Dictionary<string, object>()
            {
                { "symbol", SymbolMapper.GetBrokerageSymbol(order.Symbol) }
            };
            foreach (var id in order.BrokerId)
            {
                body["orderId"] = id;
                var request = new RestRequest($"{ApiPrefix}/order", Method.DELETE);

                var response = ExecuteRestRequestWithSignature(request, body);
                success.Add(response.StatusCode == HttpStatusCode.OK);
            }

            var canceled = false;
            if (success.All(a => a))
            {
                OnOrderEvent(new OrderEvent(order,
                    DateTime.UtcNow,
                    OrderFee.Zero,
                    "Binance Order Event")
                { Status = OrderStatus.Canceled });

                canceled = true;
            }
            return canceled;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public IEnumerable<Messages.Kline> GetHistory(Data.HistoryRequest request)
        {
            var resolution = ConvertResolution(request.Resolution);
            var resolutionInMs = (long)request.Resolution.ToTimeSpan().TotalMilliseconds;
            var symbol = SymbolMapper.GetBrokerageSymbol(request.Symbol);
            var startMs = (long)Time.DateTimeToUnixTimeStamp(request.StartTimeUtc) * 1000;
            var endMs = (long)Time.DateTimeToUnixTimeStamp(request.EndTimeUtc) * 1000;

            var endpoint = $"{GetBaseDataEndpoint()}/klines?symbol={symbol}&interval={resolution}&limit=1000";
            if (_restClient?.BaseUrl?.Host.Equals("testnet.binance.vision") == true)
            {
                // we always use the global endpoint for history requests, as not binance testnet available
                endpoint = "https://api.binance.com" + endpoint;
            }

            while (endMs - startMs >= resolutionInMs)
            {
                var timeframe = $"&startTime={startMs}&endTime={endMs}";

                var restRequest = new RestRequest(endpoint + timeframe, Method.GET);
                var response = ExecuteRestRequest(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"BinanceBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                }

                var klines = JsonConvert.DeserializeObject<object[][]>(response.Content)
                    .Select(entries => new Messages.Kline(entries))
                    .ToList();

                if (klines.Count > 0)
                {
                    var lastValue = klines[klines.Count - 1];
                    if (Log.DebuggingEnabled)
                    {
                        var windowStartTime = Time.UnixMillisecondTimeStampToDateTime(klines[0].OpenTime);
                        var windowEndTime = Time.UnixMillisecondTimeStampToDateTime(lastValue.OpenTime + resolutionInMs);
                        Log.Debug($"BinanceRestApiClient.GetHistory(): Received [{symbol}] data for timeperiod from {windowStartTime.ToStringInvariant()} to {windowEndTime.ToStringInvariant()}..");
                    }
                    startMs = lastValue.OpenTime + resolutionInMs;

                    foreach (var kline in klines)
                    {
                        yield return kline;
                    }
                }
                else
                {
                    // if there is no data just break
                    break;
                }
            }
        }

        /// <summary>
        /// Ticker Price Change Statistics
        /// </summary>
        public PriceChangeStatistics[] GetTickerPriceChangeStatistics()
        {
            var endpoint = TickerPriceChangeStatisticsEndpoint;
            var request = new RestRequest(endpoint, Method.GET);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            return JsonConvert.DeserializeObject<PriceChangeStatistics[]>(response.Content);
        }

        /// <summary>
        /// Check User Data stream listen key is alive
        /// </summary>
        /// <returns></returns>
        public bool SessionKeepAlive()
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                throw new Exception("BinanceBrokerage:UserStream. listenKey wasn't allocated or has been refused.");
            }

            var ping = new RestRequest(UserDataStreamEndpoint, Method.PUT);
            ping.AddHeader(KeyHeader, ApiKey);
            ping.AddParameter(
                "application/x-www-form-urlencoded",
                Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                ParameterType.RequestBody
            );

            var pong = ExecuteRestRequest(ping);
            return pong.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Stops the session
        /// </summary>
        public void StopSession()
        {
            if (!string.IsNullOrEmpty(SessionId))
            {
                var request = new RestRequest(UserDataStreamEndpoint, Method.DELETE);
                request.AddHeader(KeyHeader, ApiKey);
                request.AddParameter(
                    "application/x-www-form-urlencoded",
                    Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                    ParameterType.RequestBody
                );
                if (ExecuteRestRequest(request).StatusCode == HttpStatusCode.OK)
                {
                    SessionId = null;
                }
            }
        }

        /// <summary>
        /// Provides the current tickers price
        /// </summary>
        /// <returns></returns>
        public PriceTicker[] GetTickers()
        {
            var endpoint = $"{GetBaseDataEndpoint()}/ticker/price";
            var req = new RestRequest(endpoint, Method.GET);
            var response = ExecuteRestRequest(req);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            return JsonConvert.DeserializeObject<PriceTicker[]>(response.Content);
        }

        /// <summary>
        /// Start user data stream
        /// </summary>
        public void CreateListenKey()
        {
            var request = new RestRequest(UserDataStreamEndpoint, Method.POST);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.StartSession: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var content = JObject.Parse(response.Content);
            lock (_listenKeyLocker)
            {
                SessionId = content.Value<string>("listenKey");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _cancellationToken.DisposeSafely();
        }

        protected virtual string GetBaseDataEndpoint()
        {
            return "/api/v3";
        }

        /// <summary>
        /// Executes a REST request with a signature if required, handling rate limits and retries on HTTP 429 responses.
        /// </summary>
        /// <param name="request">The REST request to execute.</param>
        /// <param name="body">Optional request body parameters.</param>
        /// <returns>The response from the REST API.</returns>
        protected IRestResponse ExecuteRestRequestWithSignature(IRestRequest request, IDictionary<string, object> body = default)
        {
            return ExecuteRestRequestInternal(request, body, true);
        }

        /// <summary>
        /// Executes a REST request while handling rate limits and retries on HTTP 429 responses.
        /// </summary>
        /// <param name="request">The REST request to execute.</param>
        /// <returns>The response from the REST API.</returns>
        protected IRestResponse ExecuteRestRequest(IRestRequest request)
        {
            return ExecuteRestRequestInternal(request, null, false);
        }

        /// <summary>
        /// Executes a REST request internally with optional signature authentication.
        /// Handles rate limiting and retries failed requests due to HTTP 429 responses.
        /// </summary>
        /// <param name="request">The REST request to execute.</param>
        /// <param name="body">Optional request body parameters.</param>
        /// <param name="useSignature">Indicates whether to include a signature in the request.</param>
        /// <returns>The response from the REST API.</returns>
        private IRestResponse ExecuteRestRequestInternal(IRestRequest request, IDictionary<string, object> body, bool useSignature)
        {
            const int maxAttempts = 10;
            var attempts = 0;
            IRestResponse response;

            do
            {
                if (!_restRateLimiter.WaitToProceed(TimeSpan.Zero))
                {
                    Log.Trace("Brokerage.OnMessage(): " + new BrokerageMessageEvent(BrokerageMessageType.Warning, "RateLimit",
                        "The API request has been rate limited. To avoid this message, please reduce the frequency of API calls."));

                    _restRateLimiter.WaitToProceed();
                }

                if (useSignature)
                {
                    request.AddOrUpdateHeader(KeyHeader, ApiKey);

                    if (body != null)
                    {
                        // Remove the old 'signature' before generating a new authenticated token
                        body.Remove("signature");

                        body["timestamp"] = GetNonce();
                        body["signature"] = AuthenticationToken(body.ToQueryString());

                        request.AddOrUpdateParameter("application/x-www-form-urlencoded", Encoding.UTF8.GetBytes(body.ToQueryString()), ParameterType.RequestBody);
                    }
                    else
                    {
                        var nonce = GetNonce();
                        var queryString = $"timestamp={nonce}";

                        request.AddOrUpdateParameter("timestamp", nonce.ToStringInvariant(), ParameterType.QueryString);
                        request.AddOrUpdateParameter("signature", AuthenticationToken(queryString), ParameterType.QueryString);
                    }
                }

                response = _restClient.Execute(request);

                // Retry on HTTP 429 (Too Many Requests), with exponential backoff using WaitOne as an additional safeguard.
            } while (++attempts < maxAttempts && (int)response.StatusCode == 429 && !_cancellationToken.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1 * attempts)));

            return response;
        }

        private decimal GetTickerPrice(Order order)
        {
            var security = _securityProvider.GetSecurity(order.Symbol);
            var tickerPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
            if (tickerPrice == 0)
            {
                var brokerageSymbol = SymbolMapper.GetBrokerageSymbol(order.Symbol);
                var tickers = GetTickers();
                var ticker = tickers.FirstOrDefault(t => t.Symbol == brokerageSymbol);
                if (ticker == null)
                {
                    throw new KeyNotFoundException($"BinanceBrokerage: Unable to resolve currency conversion pair: {order.Symbol}");
                }
                tickerPrice = ticker.Price;
            }
            return tickerPrice;
        }

        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        /// <returns></returns>
        protected long GetNonce()
        {
            return (long)Time.DateTimeToUnixTimeStampMilliseconds(DateTime.UtcNow);
        }

        /// <summary>
        /// Creates a signature for signed endpoints
        /// </summary>
        /// <param name="payload">the body of the request</param>
        /// <returns>a token representing the request params</returns>
        protected string AuthenticationToken(string payload)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)).ToHexString();
            }
        }

        private static string ConvertOrderDirection(OrderDirection orderDirection)
        {
            if (orderDirection == OrderDirection.Buy || orderDirection == OrderDirection.Sell)
            {
                return orderDirection.ToString().LazyToUpper();
            }

            throw new NotSupportedException($"BinanceBrokerage.ConvertOrderDirection: Unsupported order direction: {orderDirection}");
        }


        private readonly Dictionary<Resolution, string> _knownResolutions = new Dictionary<Resolution, string>()
        {
            { Resolution.Minute, "1m" },
            { Resolution.Hour,   "1h" },
            { Resolution.Daily,  "1d" }
        };

        private string ConvertResolution(Resolution resolution)
        {
            if (_knownResolutions.ContainsKey(resolution))
            {
                return _knownResolutions[resolution];
            }
            else
            {
                throw new ArgumentException($"BinanceBrokerage.ConvertResolution: Unsupported resolution type: {resolution}");
            }
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="newOrder">The brokerage order submit result</param>
        /// <param name="order">The lean order</param>
        private void OnOrderSubmit(Messages.NewOrder newOrder, Order order)
        {
            try
            {
                OrderSubmit?.Invoke(
                    this,
                    new BinanceOrderSubmitEventArgs(newOrder.Id, order));

                // Generate submitted event
                OnOrderEvent(new OrderEvent(
                    order,
                    Time.UnixMillisecondTimeStampToDateTime(newOrder.TransactionTime),
                    OrderFee.Zero,
                    "Binance Order Event")
                { Status = OrderStatus.Submitted }
                );
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        private void OnOrderEvent(OrderEvent e)
        {
            try
            {
                Log.Debug("Brokerage.OnOrderEvent(): " + e);

                OrderStatusChanged?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        protected virtual void OnMessage(BrokerageMessageEvent e)
        {
            try
            {
                if (e.Type == BrokerageMessageType.Error)
                {
                    Log.Error("Brokerage.OnMessage(): " + e);
                }
                else
                {
                    Log.Trace("Brokerage.OnMessage(): " + e);
                }

                Message?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
