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
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Binance.Tests
{
    /// <summary>
    /// An order processor for the algorithm the brokerage is created with, which gives it access to the orders of the brokerage tests:
    /// the brokerage finds the orders of its order events through the algorithm transactions
    /// </summary>
    public class OrderProviderOrderProcessor : IOrderProcessor
    {
        private readonly IOrderProvider _orderProvider;

        public OrderProviderOrderProcessor(IOrderProvider orderProvider)
        {
            _orderProvider = orderProvider;
        }

        public int OrdersCount => _orderProvider.OrdersCount;
        public Order GetOrderById(int orderId) => _orderProvider.GetOrderById(orderId);
        public List<Order> GetOrdersByBrokerageId(string brokerageId) => _orderProvider.GetOrdersByBrokerageId(brokerageId);
        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null) => _orderProvider.GetOrderTickets(filter);
        public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null) => _orderProvider.GetOpenOrderTickets(filter);
        public OrderTicket GetOrderTicket(int orderId) => _orderProvider.GetOrderTicket(orderId);
        public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null) => _orderProvider.GetOrders(filter);
        public List<Order> GetOpenOrders(Func<Order, bool> filter = null) => _orderProvider.GetOpenOrders(filter);
        public ProjectedHoldings GetProjectedHoldings(Security security) => _orderProvider.GetProjectedHoldings(security);
        public OrderTicket Process(OrderRequest request) => throw new NotSupportedException("The brokerage tests place the orders directly with the brokerage");
    }
}
