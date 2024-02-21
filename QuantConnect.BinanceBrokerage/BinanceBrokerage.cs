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
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using QuantConnect.Api;
using QuantConnect.Brokerages;
using RestSharp;
using Timer = System.Timers.Timer;
using QuantConnect.BinanceBrokerage.Constants;

namespace QuantConnect.BinanceBrokerage
{
    /// <summary>
    /// Binance brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(BinanceBrokerageFactory))]
    public partial class BinanceBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        private IAlgorithm _algorithm;
        private SymbolPropertiesDatabaseSymbolMapper _symbolMapper;

        // Binance allows 5 messages per second, but we still get rate limited if we send a lot of messages at that rate
        // By sending 3 messages per second, evenly spaced out, we can keep sending messages without being limited
        private readonly RateGate _webSocketRateLimiter = new RateGate(1, TimeSpan.FromMilliseconds(330));

        private long _lastRequestId;

        private LiveNodePacket _job;
        private string _webSocketBaseUrl;
        private Timer _keepAliveTimer;
        private Timer _reconnectTimer;
        private Lazy<BinanceBaseRestApiClient> _apiClientLazy;

        private BrokerageConcurrentMessageHandler<WebSocketMessage> _messageHandler;

        private const int MaximumSymbolsPerConnection = 512;

        protected BinanceBaseRestApiClient ApiClient => _apiClientLazy?.Value;
        protected string MarketName { get; set; }

        /// <summary>
        /// Gets or sets the trade channel used for streaming trade information.
        /// </summary>
        /// <remarks>
        /// The <see cref="TradeChannelName"/> property represents the specific trade channel utilized for streaming trade data.
        /// It is initialized with the constant value <see cref="TradeChannels.SpotTradeChannelName"/>, indicating the use of
        /// Spot Trade Streams by default.
        /// </remarks>
        /// <value>
        /// The default value is the channel name for Spot Trade Streams: <c>trade</c>.
        /// </value>
        protected virtual string TradeChannelName { get; } = TradeChannels.SpotTradeChannelName;

        /// <summary>
        /// Parameterless constructor for brokerage
        /// </summary>
        public BinanceBrokerage() : this(Market.Binance)
        {
        }

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        public BinanceBrokerage(string marketName) : base(marketName)
        {
            MarketName = marketName;
        }

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="restApiUrl">The rest api url</param>
        /// <param name="webSocketBaseUrl">The web socket base url</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        public BinanceBrokerage(string apiKey, string apiSecret, string restApiUrl, string webSocketBaseUrl, IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
            : this(apiKey, apiSecret, restApiUrl, webSocketBaseUrl, algorithm, aggregator, job, Market.Binance)
        {
        }

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="restApiUrl">The rest api url</param>
        /// <param name="webSocketBaseUrl">The web socket base url</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        /// <param name="marketName">Actual market name</param>
        public BinanceBrokerage(string apiKey, string apiSecret, string restApiUrl, string webSocketBaseUrl, IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job, string marketName)
            : base(marketName)
        {
            Initialize(
                webSocketBaseUrl,
                restApiUrl,
                apiKey,
                apiSecret,
                algorithm,
                aggregator,
                job,
                marketName
            );
        }

        #region IBrokerage

        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// WebSocket is responsible for Binance UserData stream only.
        /// </summary>
        public override bool IsConnected => _apiClientLazy?.IsValueCreated != true || WebSocket?.IsOpen == true;

        /// <summary>
        /// Creates wss connection
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            // cannot reach this code if rest api client is not created
            // WebSocket is  responsible for Binance UserData stream only
            // as a result we don't need to connect user data stream if BinanceBrokerage is used as DQH only
            // or until Algorithm is actually initialized
            ApiClient.CreateListenKey();
            Connect(ApiClient.SessionId);
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            if (WebSocket?.IsOpen != true)
                return;

            _reconnectTimer.Stop();
            WebSocket.Close();
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns>The list of all account holdings</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = ApiClient.GetAccountHoldings();
            if(holdings.Count > 0)
            {
                return holdings;
            }
            return base.GetAccountHoldings(_job?.BrokerageData, _algorithm.Securities.Values);
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var balances = ApiClient.GetCashBalance();
            if (balances == null || !balances.Any())
                return new List<CashAmount>();

            return balances
                .Select(b => new CashAmount(b.Amount, b.Asset.LazyToUpper()))
                .ToList();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var orders = ApiClient.GetOpenOrders();
            List<Order> list = new List<Order>();
            foreach (var item in orders)
            {
                var orderQuantity = item.Quantity;
                var orderLeanSymbol = _symbolMapper.GetLeanSymbol(item.Symbol, GetSupportedSecurityType(), MarketName);
                var orderTime = Time.UnixMillisecondTimeStampToDateTime(item.Time);

                Order order;
                switch (item.Type.LazyToUpper())
                {
                    case "MARKET":
                        order = new MarketOrder(orderLeanSymbol, orderQuantity, orderTime);
                        break;

                    case "LIMIT":
                    case "LIMIT_MAKER":
                        order = new LimitOrder(orderLeanSymbol, orderQuantity, item.Price, orderTime);
                        break;

                    case "STOP_LOSS":
                    case "TAKE_PROFIT":
                        order = new StopMarketOrder(orderLeanSymbol, orderQuantity, item.StopPrice, orderTime);
                        break;

                    case "STOP_LOSS_LIMIT":
                    case "TAKE_PROFIT_LIMIT":
                        order = new StopLimitOrder(orderLeanSymbol, orderQuantity, item.StopPrice, item.Price, orderTime);
                        break;

                    default:
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1,
                            "BinanceBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                        continue;
                }

                order.BrokerId.Add(item.Id);
                order.Status = ConvertOrderStatus(item.Status);

                list.Add(order);
            }

            return list;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            if (!CanSubscribe(order.Symbol))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Symbol is not supported {order.Symbol}"));
                return false;
            }
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                submitted = ApiClient.PlaceOrder(order);
            });

            return submitted;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            throw new NotSupportedException("BinanceBrokerage.UpdateOrder: Order update not supported. Please cancel and re-create.");
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                submitted = ApiClient.CancelOrder(order);
            });

            return submitted;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(Data.HistoryRequest request)
        {
            if (request.Resolution == Resolution.Tick || request.Resolution == Resolution.Second)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidResolution",
                    $"{request.Resolution} resolution is not supported, no history returned"));
                yield break;
            }

            if (request.TickType != TickType.Trade)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidTickType",
                    $"{request.TickType} tick type not supported, no history returned"));
                yield break;
            }

            if (request.Symbol.SecurityType != GetSupportedSecurityType())
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidSecurityType",
                    $"{request.Symbol.SecurityType} security type not supported, no history returned"));
                yield break;
            }

            var period = request.Resolution.ToTimeSpan();
            var restApiClient = _apiClientLazy?.IsValueCreated == true
                ? ApiClient
                : GetApiClient(_symbolMapper, null, null, null, null);
            foreach (var kline in restApiClient.GetHistory(request))
            {
                yield return new TradeBar()
                {
                    Time = Time.UnixMillisecondTimeStampToDateTime(kline.OpenTime),
                    Symbol = request.Symbol,
                    Low = kline.Low,
                    High = kline.High,
                    Open = kline.Open,
                    Close = kline.Close,
                    Volume = kline.Volume,
                    Value = kline.Close,
                    DataType = MarketDataType.TradeBar,
                    Period = period
                };
            }
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMessage(object sender, WebSocketMessage e)
        {
            _messageHandler.HandleNewMessage(e);
        }

        #endregion IBrokerage

        #region IDataQueueHandler

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
            var aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
                Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"), forceTypeNameOnExisting: false);

            SetJobInit(job, aggregator);

            if (!IsConnected)
            {
                Connect();
            }
        }

        protected virtual void SetJobInit(LiveNodePacket job, IDataAggregator aggregator)
        {
            Initialize(
                wssUrl: job.BrokerageData["binance-websocket-url"],
                restApiUrl: job.BrokerageData["binance-api-url"],
                apiKey: job.BrokerageData["binance-api-key"],
                apiSecret: job.BrokerageData["binance-api-secret"],
                algorithm: null,
                aggregator,
                job,
                Market.Binance
            );
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return null;
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            SubscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            SubscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Checks if this brokerage supports the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>returns true if brokerage supports the specified symbol; otherwise false</returns>
        protected virtual bool CanSubscribe(Symbol symbol)
        {
            return !symbol.Value.Contains("UNIVERSE") &&
                   symbol.SecurityType == GetSupportedSecurityType() &&
                   symbol.ID.Market == MarketName;
        }

        /// <summary>
        /// Get's the supported security type by the brokerage
        /// </summary>
        protected virtual SecurityType GetSupportedSecurityType()
        {
            return SecurityType.Crypto;
        }

        #endregion IDataQueueHandler

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _keepAliveTimer.DisposeSafely();
            _reconnectTimer.DisposeSafely();
            if (_apiClientLazy?.IsValueCreated == true)
            {
                ApiClient.DisposeSafely();
            }
            _webSocketRateLimiter.DisposeSafely();
            SubscriptionManager.DisposeSafely();
        }

        /// <summary>
        /// Not used
        /// </summary>
        protected override bool Subscribe(IEnumerable<Symbol> symbols)
        {
            // NOP
            return true;
        }

        /// <summary>
        /// Initialize the instance of this class
        /// </summary>
        /// <param name="wssUrl">The web socket base url</param>
        /// <param name="restApiUrl">The rest api url</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        /// <param name="marketName">market name</param>
        protected void Initialize(string wssUrl, string restApiUrl, string apiKey, string apiSecret,
            IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job, string marketName)
        {
            if (IsInitialized)
            {
                return;
            }
            if (marketName.Equals(Market.BinanceUS) && restApiUrl.Contains("testnet.binance.vision"))
            {
                throw new InvalidOperationException("Binance.US doesn't support SPOT Testnet trading.");
            }

            ValidateSubscription();

            base.Initialize(wssUrl, new WebSocketClientWrapper(), null, apiKey, apiSecret);
            _job = job;
            _algorithm = algorithm;
            _aggregator = aggregator;
            _webSocketBaseUrl = wssUrl;
            _messageHandler = new BrokerageConcurrentMessageHandler<WebSocketMessage>(OnUserMessage);
            _symbolMapper = new(marketName);
            MarketName = marketName;

            var maximumWebSocketConnections = Config.GetInt("binance-maximum-websocket-connections");
            var symbolWeights = maximumWebSocketConnections > 0 ? FetchSymbolWeights(restApiUrl) : null;

            var subscriptionManager = new BrokerageMultiWebSocketSubscriptionManager(
                wssUrl,
                MaximumSymbolsPerConnection,
                maximumWebSocketConnections,
                symbolWeights,
                () => new BinanceWebSocketWrapper(null),
                Subscribe,
                Unsubscribe,
                OnDataMessage,
                new TimeSpan(23, 45, 0));

            SubscriptionManager = subscriptionManager;

            // can be null, if BinanceBrokerage is used as DataQueueHandler only
            if (_algorithm != null)
            {
                // Binance rest api endpoint is different for sport and margin trading
                // we need to delay initialization of rest api client until Algorithm is initialized
                // and user brokerage choise is actually applied
                _apiClientLazy = new Lazy<BinanceBaseRestApiClient>(() =>
                {
                    var apiClient = GetApiClient(_symbolMapper, _algorithm?.Portfolio, restApiUrl, apiKey, apiSecret);

                    apiClient.OrderSubmit += (s, e) => OnOrderSubmit(e);
                    apiClient.OrderStatusChanged += (s, e) => OnOrderEvent(e);
                    apiClient.Message += (s, e) => OnMessage(e);

                    // once we know the api endpoint we can subscribe to user data stream
                    apiClient.CreateListenKey();
                    _keepAliveTimer.Elapsed += (s, e) => apiClient.SessionKeepAlive();

                    Connect(apiClient.SessionId);

                    return apiClient;
                });
            }

            // User data streams will close after 60 minutes. It's recommended to send a ping about every 30 minutes.
            // Source: https://github.com/binance-exchange/binance-official-api-docs/blob/master/user-data-stream.md#pingkeep-alive-a-listenkey
            _keepAliveTimer = new Timer
            {
                // 30 minutes
                Interval = 30 * 60 * 1000
            };

            WebSocket.Open += (s, e) =>
            {
                _keepAliveTimer.Start();
            };
            WebSocket.Closed += (s, e) =>
            {
                ApiClient.StopSession();
                _keepAliveTimer.Stop();
            };

            // A single connection to stream.binance.com is only valid for 24 hours; expect to be disconnected at the 24 hour mark
            // Source: https://github.com/binance-exchange/binance-official-api-docs/blob/master/web-socket-streams.md#general-wss-information
            _reconnectTimer = new Timer
            {
                // 23.5 hours
                Interval = 23.5 * 60 * 60 * 1000
            };
            _reconnectTimer.Elapsed += (s, e) =>
            {
                Log.Trace("Daily websocket restart: disconnect");
                Disconnect();

                Log.Trace("Daily websocket restart: connect");
                Connect();
            };
        }

        /// <summary>
        /// Get's the appropiate API client to use
        /// </summary>
        protected virtual BinanceBaseRestApiClient GetApiClient(ISymbolMapper symbolMapper, ISecurityProvider securityProvider,
            string restApiUrl, string apiKey, string apiSecret)
        {
            restApiUrl ??= Config.Get("binance-api-url", "https://api.binance.com");

            return (_algorithm == null || _algorithm.BrokerageModel.AccountType == AccountType.Cash)
                 ? new BinanceSpotRestApiClient(symbolMapper, securityProvider, apiKey, apiSecret, restApiUrl)
                 : new BinanceCrossMarginRestApiClient(symbolMapper, securityProvider, apiKey, apiSecret,
                     restApiUrl);
        }

        /// <summary>
        /// Subscribes to the requested symbol (using an individual streaming channel)
        /// </summary>
        /// <param name="webSocket">The websocket instance</param>
        /// <param name="symbol">The symbol to subscribe</param>
        private bool Subscribe(IWebSocket webSocket, Symbol symbol)
        {
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            Send(webSocket,
                new
                {
                    method = "SUBSCRIBE",
                    @params = new[]
                    {
                        $"{brokerageSymbol.ToLowerInvariant()}@{TradeChannelName}",
                        $"{brokerageSymbol.ToLowerInvariant()}@bookTicker"
                    },
                    id = GetNextRequestId()
                }
            );

            return true;
        }

        /// <summary>
        /// Ends current subscription
        /// </summary>
        /// <param name="webSocket">The websocket instance</param>
        /// <param name="symbol">The symbol to unsubscribe</param>
        private bool Unsubscribe(IWebSocket webSocket, Symbol symbol)
        {
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            Send(webSocket,
                new
                {
                    method = "UNSUBSCRIBE",
                    @params = new[]
                    {
                        $"{brokerageSymbol.ToLowerInvariant()}@{TradeChannelName}",
                        $"{brokerageSymbol.ToLowerInvariant()}@bookTicker"
                    },
                    id = GetNextRequestId()
                }
            );

            return true;
        }

        private void Send(IWebSocket webSocket, object obj)
        {
            var json = JsonConvert.SerializeObject(obj);

            _webSocketRateLimiter.WaitToProceed();

            if (Log.DebuggingEnabled)
            {
                Log.Debug("BinanceBrokerage.Send(): " + json);
            }
            webSocket.Send(json);
        }

        private long GetNextRequestId()
        {
            return Interlocked.Increment(ref _lastRequestId);
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        private void OnOrderSubmit(BinanceOrderSubmitEventArgs e)
        {
            var brokerId = new List<string> { e.BrokerId };
            var order = e.Order;

            OnOrderIdChangedEvent(new BrokerageOrderIdChangedEvent { BrokerId = brokerId, OrderId = order.Id });
        }

        /// <summary>
        /// Returns the weights for each symbol (the weight value is the count of trades in the last 24 hours)
        /// </summary>
        private Dictionary<Symbol, int> FetchSymbolWeights(string restApiUrl)
        {
            try
            {
                var restClient = GetApiClient(_symbolMapper, null, restApiUrl, null, null);
                var symbolAndTradeCount = restClient.GetTickerPriceChangeStatistics();

                var result = new Dictionary<Symbol, int>();
                foreach (var s in symbolAndTradeCount)
                {
                    try
                    {
                        result[_symbolMapper.GetLeanSymbol(s.Symbol, GetSupportedSecurityType(), MarketName)] = s.Count;
                    }
                    catch
                    {
                        // will ignore symbols we do not support
                    }
                }

                Log.Trace($"BinanceBrokerage.FetchSymbolWeights(): returning weights for '{result.Count}' symbols");
                return result;
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        /// <summary>
        /// Force reconnect websocket
        /// </summary>
        private void Connect(string sessionId)
        {
            Log.Trace("BinanceBrokerage.Connect(): Connecting...");

            _reconnectTimer.Start();
            WebSocket.Initialize($"{_webSocketBaseUrl}/{sessionId}");
            ConnectSync();
        }

        private class ModulesReadLicenseRead : Api.RestResponse
        {
            [JsonProperty(PropertyName = "license")]
            public string License;
            [JsonProperty(PropertyName = "organizationId")]
            public string OrganizationId;
        }

        /// <summary>
        /// Validate the user of this project has permission to be using it via our web API.
        /// </summary>
        private static void ValidateSubscription()
        {
            try
            {
                const int productId = 176;
                var userId = Globals.UserId;
                var token = Globals.UserToken;
                var organizationId = Globals.OrganizationID;
                // Verify we can authenticate with this user and token
                var api = new ApiConnection(userId, token);
                if (!api.Connected)
                {
                    throw new ArgumentException("Invalid api user id or token, cannot authenticate subscription.");
                }
                // Compile the information we want to send when validating
                var information = new Dictionary<string, object>()
                {
                    {"productId", productId},
                    {"machineName", Environment.MachineName},
                    {"userName", Environment.UserName},
                    {"domainName", Environment.UserDomainName},
                    {"os", Environment.OSVersion}
                };
                // IP and Mac Address Information
                try
                {
                    var interfaceDictionary = new List<Dictionary<string, object>>();
                    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up))
                    {
                        var interfaceInformation = new Dictionary<string, object>();
                        // Get UnicastAddresses
                        var addresses = nic.GetIPProperties().UnicastAddresses
                            .Select(uniAddress => uniAddress.Address)
                            .Where(address => !IPAddress.IsLoopback(address)).Select(x => x.ToString());
                        // If this interface has non-loopback addresses, we will include it
                        if (!addresses.IsNullOrEmpty())
                        {
                            interfaceInformation.Add("unicastAddresses", addresses);
                            // Get MAC address
                            interfaceInformation.Add("MAC", nic.GetPhysicalAddress().ToString());
                            // Add Interface name
                            interfaceInformation.Add("name", nic.Name);
                            // Add these to our dictionary
                            interfaceDictionary.Add(interfaceInformation);
                        }
                    }
                    information.Add("networkInterfaces", interfaceDictionary);
                }
                catch (Exception)
                {
                    // NOP, not necessary to crash if fails to extract and add this information
                }
                // Include our OrganizationId is specified
                if (!string.IsNullOrEmpty(organizationId))
                {
                    information.Add("organizationId", organizationId);
                }
                var request = new RestRequest("modules/license/read", Method.POST) { RequestFormat = DataFormat.Json };
                request.AddParameter("application/json", JsonConvert.SerializeObject(information), ParameterType.RequestBody);
                api.TryRequest(request, out ModulesReadLicenseRead result);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Request for subscriptions from web failed, Response Errors : {string.Join(',', result.Errors)}");
                }

                var encryptedData = result.License;
                // Decrypt the data we received
                DateTime? expirationDate = null;
                long? stamp = null;
                bool? isValid = null;
                if (encryptedData != null)
                {
                    // Fetch the org id from the response if we are null, we need it to generate our validation key
                    if (string.IsNullOrEmpty(organizationId))
                    {
                        organizationId = result.OrganizationId;
                    }
                    // Create our combination key
                    var password = $"{token}-{organizationId}";
                    var key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                    // Split the data
                    var info = encryptedData.Split("::");
                    var buffer = Convert.FromBase64String(info[0]);
                    var iv = Convert.FromBase64String(info[1]);
                    // Decrypt our information
                    using var aes = new AesManaged();
                    var decryptor = aes.CreateDecryptor(key, iv);
                    using var memoryStream = new MemoryStream(buffer);
                    using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                    using var streamReader = new StreamReader(cryptoStream);
                    var decryptedData = streamReader.ReadToEnd();
                    if (!decryptedData.IsNullOrEmpty())
                    {
                        var jsonInfo = JsonConvert.DeserializeObject<JObject>(decryptedData);
                        expirationDate = jsonInfo["expiration"]?.Value<DateTime>();
                        isValid = jsonInfo["isValid"]?.Value<bool>();
                        stamp = jsonInfo["stamped"]?.Value<int>();
                    }
                }
                // Validate our conditions
                if (!expirationDate.HasValue || !isValid.HasValue || !stamp.HasValue)
                {
                    throw new InvalidOperationException("Failed to validate subscription.");
                }

                var nowUtc = DateTime.UtcNow;
                var timeSpan = nowUtc - Time.UnixTimeStampToDateTime(stamp.Value);
                if (timeSpan > TimeSpan.FromHours(12))
                {
                    throw new InvalidOperationException("Invalid API response.");
                }
                if (!isValid.Value)
                {
                    throw new ArgumentException($"Your subscription is not valid, please check your product subscriptions on our website.");
                }
                if (expirationDate < nowUtc)
                {
                    throw new ArgumentException($"Your subscription expired {expirationDate}, please renew in order to use this product.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"ValidateSubscription(): Failed during validation, shutting down. Error : {e.Message}");
                Environment.Exit(1);
            }
        }
    }
}
