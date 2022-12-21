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

using QuantConnect.BinanceBrokerage.Messages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;

namespace QuantConnect.BinanceBrokerage
{
    public partial class BinanceBrokerage
    {
        private IDataAggregator _aggregator;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        protected readonly object TickLocker = new object();

        private void OnUserMessage(WebSocketMessage webSocketMessage)
        {
            var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;

            try
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"BinanceBrokerage.OnUserMessage(): {e.Message}");
                }

                var obj = JObject.Parse(e.Message);

                var objError = obj["error"];
                if (objError != null)
                {
                    var error = objError.ToObject<ErrorMessage>();
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, error.Code, error.Message));
                    return;
                }

                var objData = obj;

                var objEventType = objData["e"];
                if (objEventType != null)
                {
                    var eventType = objEventType.ToObject<string>();

                    Execution execution = null;
                    switch (eventType)
                    {
                        case "executionReport":
                            execution = objData.ToObject<Execution>();
                            break;
                        case "ORDER_TRADE_UPDATE":
                            execution = objData["o"].ToObject<Execution>();
                            break;
                    }

                    if (execution != null && (execution.ExecutionType.Equals("TRADE", StringComparison.OrdinalIgnoreCase)
                        || execution.ExecutionType.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase)))
                    {
                        OnFillOrder(execution);
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnDataMessage(WebSocketMessage webSocketMessage)
        {
            var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;

            try
            {
                var obj = JObject.Parse(e.Message);

                var objError = obj["error"];
                if (objError != null)
                {
                    var error = objError.ToObject<ErrorMessage>();
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, error.Code, error.Message));
                    return;
                }

                var objData = obj;

                var objEventType = objData["e"];
                if (objEventType != null)
                {
                    var eventType = objEventType.ToObject<string>();

                    switch (eventType)
                    {
                        case "trade":
                            var trade = objData.ToObject<Trade>();
                            // futures feed send upper and lower case T confusing json
                            trade.Time = objData["T"].ToObject<long>();
                            EmitTradeTick(
                                _symbolMapper.GetLeanSymbol(trade.Symbol, GetSupportedSecurityType(), MarketName),
                                Time.UnixMillisecondTimeStampToDateTime(trade.Time),
                                trade.Price,
                                trade.Quantity);
                            break;
                        case "bookTicker":
                            // futures stream the event type but spot doesn't, that's why we have the next 'else if'
                            HandleQuoteTick(objData);
                            break;
                    }
                }
                else if (objData["u"] != null)
                {
                    HandleQuoteTick(objData);
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void HandleQuoteTick(JObject objData)
        {
            var quote = objData.ToObject<BestBidAskQuote>();
            EmitQuoteTick(
                _symbolMapper.GetLeanSymbol(quote.Symbol, GetSupportedSecurityType(), MarketName),
                quote.BestBidPrice,
                quote.BestBidSize,
                quote.BestAskPrice,
                quote.BestAskSize);
        }

        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            var tick = new Tick
            {
                AskPrice = askPrice,
                BidPrice = bidPrice,
                Time = DateTime.UtcNow,
                Symbol = symbol,
                TickType = TickType.Quote,
                AskSize = askSize,
                BidSize = bidSize
            };
            tick.SetValue();

            lock (TickLocker)
            {
                _aggregator.Update(tick);
            }
        }

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal quantity)
        {
            var tick = new Tick
            {
                Symbol = symbol,
                Value = price,
                Quantity = Math.Abs(quantity),
                Time = time,
                TickType = TickType.Trade
            };

            lock (TickLocker)
            {
                _aggregator.Update(tick);
            }
        }

        private void OnFillOrder(Execution data)
        {
            try
            {
                var order = _algorithm.Transactions.GetOrderByBrokerageId(data.OrderId);
                if (order == null)
                {
                    // not our order, nothing else to do here
                    Log.Error($"BinanceBrokerage.OnFillOrder(): order not found: {data.OrderId}");
                    return;
                }

                var fillPrice = data.LastExecutedPrice;
                var fillQuantity = data.Direction == OrderDirection.Sell ? -data.LastExecutedQuantity : data.LastExecutedQuantity;
                var updTime = Time.UnixMillisecondTimeStampToDateTime(data.TransactionTime);
                var orderFee = OrderFee.Zero;
                if (!string.IsNullOrEmpty(data.FeeCurrency))
                {
                    // might not be sent if zero fee
                    orderFee = new OrderFee(new CashAmount(data.Fee, data.FeeCurrency));
                }
                var status = ConvertOrderStatus(data.OrderStatus);
                var orderEvent = new OrderEvent
                (
                    order.Id, order.Symbol, updTime, status,
                    data.Direction, fillPrice, fillQuantity,
                    orderFee, $"Binance Order Event {data.Direction}"
                );

                OnOrderEvent(orderEvent);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }
    }
}
