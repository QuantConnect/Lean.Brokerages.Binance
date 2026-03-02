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

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using QuantConnect.Brokerages.Binance.Enums;
using QuantConnect.Brokerages.Binance.Messages;

namespace QuantConnect.Brokerages.Binance.Tests
{
    [TestFixture]
    public class BinanceOnNewBrokerageOrderNotificationTests
    {
        /// <summary>
        /// Expected (action, Lean status) for every WS event in a lifecycle file.
        /// </summary>
        public record LifecycleStep(ExecutionAction Action, OrderStatus Status);

        private static IEnumerable<TestCaseData> LifecycleCases
        {
            get
            {
                // Spot MARKET SELL: NEW => PARTIALLY_FILLED => FILLED
                yield return new TestCaseData(
                    "WebSocketGlobalSpotMarketSubmitPartiallyFilledFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.PartiallyFilled),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("SpotMarket_Sell_NEW_PartialFill_Fill");

                // Spot LIMIT BUY: NEW => FILLED
                yield return new TestCaseData(
                    "WebSocketGlobalSpotLimitSubmittedFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("SpotLimit_Buy_NEW_Fill");

                // Spot STOP_LOSS_LIMIT BUY: NEW (placed) => NEW (working, stop hit) => TRADE/FILLED
                // TryGetExecution returns NewOrder for both NEW events; the duplicate-order guard in
                // OnNewBrokerageOrder handles de-duplication at runtime.
                yield return new TestCaseData(
                    "WebSocketGlobalSpotStopLimitSubmittedTriggerFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("SpotStopLossLimit_Buy_NEW_NEW_Fill");

                // Future MARKET BUY: NEW => TRADE/FILLED
                yield return new TestCaseData(
                    "WebSocketFutureMarketSubmittedFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("FutureMarket_Buy_NEW_Fill");

                // Future LIMIT BUY: NEW => TRADE/FILLED
                yield return new TestCaseData(
                    "WebSocketFutureLimitSubmittedFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("FutureLimit_Buy_NEW_Fill");

                // Future STOP (stop-limit) BUY: ALGO_UPDATE[NEW, TRIGGERING] are skipped,
                // then ORDER_TRADE_UPDATE[NEW] => working limit order, then FILLED.
                // The file also contains interleaved ALGO_UPDATE events that TryGetExecution skips.
                yield return new TestCaseData(
                    "WebSocketFutureStopLimitSubmittedFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("FutureStopLimit_Buy_NEW_Fill");

                // Future STOP_MARKET BUY: ALGO_UPDATE events are skipped, the resulting
                // MARKET child order partially fills then fully fills.
                yield return new TestCaseData(
                    "WebSocketFutureStopMarketSubmitPartiallyFilledFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.PartiallyFilled),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("FutureStopMarket_Buy_NEW_PartialFill_Fill");

                // Future STOP_MARKET BUY that is canceled before triggering.
                // All events are ALGO_UPDATE — TryGetExecution returns false for every one.
                yield return new TestCaseData(
                    "WebSocketFutureStopMarketSubmittedCanceled.json",
                    Array.Empty<LifecycleStep>()
                ).SetArgDisplayNames("FutureStopMarket_Buy_Canceled_NoParseableEvents");

                // Spot LIMIT BUY placed via the WS-API (subscriptionId wrapper format): NEW => FILLED.
                // Note: despite the "Market" in the filename the Binance payload type is LIMIT.
                yield return new TestCaseData(
                    "WebSocketGlobalSpotMarketSubmittedFilled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Filled),
                    }
                ).SetArgDisplayNames("SpotWsApi_Buy_NEW_Fill");

                // Future LIMIT BUY that is externally canceled: NEW => CANCELED.
                // CANCELED is routed to Fill so OnFillOrder can propagate Canceled status to Lean.
                yield return new TestCaseData(
                    "WebSocketFutureLimitSubmittedCancelled.json",
                    new[]
                    {
                        new LifecycleStep(ExecutionAction.NewOrder, OrderStatus.Submitted),
                        new LifecycleStep(ExecutionAction.Fill,     OrderStatus.Canceled),
                    }
                ).SetArgDisplayNames("FutureLimit_Buy_NEW_Canceled");
            }
        }

        /// <summary>
        /// Parses every WS event in a real-capture NDJSON file and verifies that
        /// <see cref="BinanceBrokerage.TryGetExecution"/> produces the expected
        /// <see cref="ExecutionAction"/> and that <see cref="BinanceBrokerage.ConvertOrderStatus"/>
        /// maps the order-status field to the correct Lean <see cref="OrderStatus"/>.
        /// No live Binance connection is required.
        /// </summary>
        [TestCaseSource(nameof(LifecycleCases))]
        public void ExecutionLifecycleProducesExpectedActionsAndStatuses(string fileName, LifecycleStep[] expectedSteps)
        {
            var filePath = Path.Combine("TestData", fileName);
            Assert.IsTrue(File.Exists(filePath), $"TestData file not found: {filePath}");

            // Collect only parseable events — ALGO_UPDATE events return false and are skipped.
            var parseableEvents = new List<(Execution Execution, ExecutionAction Action)>();
            foreach (var jObj in ParseNdjson(filePath))
            {
                if (BinanceBrokerage.TryGetExecution(jObj, out var exec, out var act))
                {
                    parseableEvents.Add((exec, act));
                }
            }

            Assert.AreEqual(expectedSteps.Length, parseableEvents.Count,
                $"Expected {expectedSteps.Length} parseable WS events in {fileName} but found {parseableEvents.Count}.");

            for (var i = 0; i < parseableEvents.Count; i++)
            {
                var step = expectedSteps[i];
                var (execution, action) = parseableEvents[i];

                Assert.AreEqual(step.Action, action, $"Event {i}: expected action {step.Action} but got {action}.");

                var leanStatus = BinanceBrokerage.ConvertOrderStatus(execution.OrderStatus);
                Assert.AreEqual(step.Status, leanStatus,
                    $"Event {i}: expected Lean status {step.Status} but got {leanStatus} (raw Binance status '{execution.OrderStatus}').");
            }
        }

        /// <summary>
        /// Verifies that <see cref="BinanceBrokerage.TryGetExecution"/> correctly maps
        /// <c>x:EXPIRED</c> on a STOP / STOP_MARKET order to <see cref="ExecutionAction.None"/>
        /// (stop-trigger consumed — NOT a real failure).  All other EXPIRED variants must map to
        /// <see cref="ExecutionAction.Fill"/> so that <c>ConvertOrderStatus("EXPIRED")</c> can
        /// emit <c>OrderStatus.Invalid</c> for genuine order expiries.
        /// </summary>
        [TestCase("STOP", "EXPIRED", ExecutionAction.None, OrderStatus.None, Description = "ExpiredStop_IsSkipped")]
        [TestCase("STOP_MARKET", "EXPIRED", ExecutionAction.None, OrderStatus.None, Description = "ExpiredStopMarket_IsSkipped")]
        [TestCase("LIMIT", "EXPIRED", ExecutionAction.Fill, OrderStatus.Invalid, Description = "ExpiredLimit_IsInvalid")]
        [TestCase("MARKET", "TRADE", ExecutionAction.Fill, OrderStatus.Filled, Description = "TradeMarket_IsFill")]
        [TestCase("LIMIT", "NEW", ExecutionAction.NewOrder, OrderStatus.Submitted, Description = "NewLimit_IsNewOrder")]
        [TestCase("LIMIT", "CANCELED", ExecutionAction.Fill, OrderStatus.Canceled, Description = "Canceled_IsFill")]
        public void TryGetExecutionExecutionTypeRoutesToExpectedAction(string orderType, string execType, ExecutionAction expectedAction, OrderStatus expectedStatus)
        {
            // `x` = execution type; `X` = order status — they are different fields.
            // A TRADE execution typically leaves the order as FILLED (or PARTIALLY_FILLED),
            // not "TRADE".  All other execution types use their own string as the order status.
            var rawOrderStatus = execType.Equals("TRADE", StringComparison.OrdinalIgnoreCase)
                ? "FILLED"
                : execType.ToUpperInvariant();

            // Build a minimal executionReport payload in the Spot wire format.
            var json = $@"{{
                ""e"": ""executionReport"",
                ""s"": ""ACHUSDT"",
                ""S"": ""BUY"",
                ""o"": ""{orderType}"",
                ""q"": ""100"",
                ""p"": ""0.01"",
                ""P"": ""0"",
                ""x"": ""{execType}"",
                ""X"": ""{rawOrderStatus}"",
                ""i"": ""99999"",
                ""l"": ""100"",
                ""z"": ""100"",
                ""L"": ""0.01"",
                ""n"": ""0"",
                ""T"": 1700000000000,
                ""t"": 1
            }}";

            var jObj = JObject.Parse(json);
            Assert.IsTrue(BinanceBrokerage.TryGetExecution(jObj, out var execution, out var action));

            Assert.AreEqual(expectedAction, action, $"Unexpected action for x={execType}, o={orderType}");

            if (expectedAction != ExecutionAction.None)
            {
                var leanStatus = BinanceBrokerage.ConvertOrderStatus(execution.OrderStatus);
                Assert.AreEqual(expectedStatus, leanStatus, $"Unexpected Lean status for x={execType}, X={execution.OrderStatus}");
            }
        }

        /// <summary>
        /// When Lean places an order via the REST API the brokerage registers the broker ID
        /// internally.  A subsequent <c>x:NEW</c> WS event for the same ID must NOT trigger
        /// <see cref="BinanceBrokerage.OnNewBrokerageOrderNotification"/> — otherwise Lean would
        /// try to register a duplicate order.
        /// </summary>
        /// <remarks>
        /// "Already tracked by Lean" guard — verifies OnNewBrokerageOrder skips
        // the notification when the order was placed through Lean (not externally).
        /// </remarks>
        [Test]
        public void AlreadyTrackedLeanOrderNewBrokerageOrderNotificationIsNotFired()
        {
            const string trackedBrokerId = "1281722417"; // taken from WebSocketGlobalSpotLimitSubmittedFilled.json

            // First event in the file is x:NEW for orderId=trackedBrokerId.
            // TryGetExecution must still return NewOrder — the guard lives one level up.
            var firstEvent = ParseNdjson(Path.Combine("TestData", "WebSocketGlobalSpotLimitSubmittedFilled.json")).First();

            Assert.IsTrue(BinanceBrokerage.TryGetExecution(firstEvent, out var execution, out var action));
            Assert.AreEqual(ExecutionAction.NewOrder, action,
                "TryGetExecution should return NewOrder regardless of whether Lean tracks the order; " +
                "deduplication is OnNewBrokerageOrder's responsibility.");

            // Spin up a testable brokerage that pre-registers the broker ID so GetLeanOrder returns
            // a non-null sentinel, causing OnNewBrokerageOrder to return early.
            using var brokerage = new TestableBinanceBrokerage();
            brokerage.TrackBrokerageId(trackedBrokerId);

            var notificationFired = false;
            brokerage.NewBrokerageOrderNotification += (_, _) => notificationFired = true;

            // Act — simulate the WS pipeline calling OnNewBrokerageOrder.
            brokerage.OnNewBrokerageOrder(execution);

            // Assert — notification must NOT fire because the order is already in Lean.
            Assert.IsFalse(notificationFired,
                "OnNewBrokerageOrderNotification must not fire for an order already tracked by Lean.");
        }

        /// <summary>
        /// Covers two CANCELED scenarios for <see cref="BinanceBrokerage.OnFillOrder"/>:
        /// <list type="bullet">
        ///   <item>
        ///     <term>Already Canceled (REST-initiated)</term>
        ///     <description>
        ///       <c>CancelOrder</c> (REST) fires <c>OnOrderEvent(Canceled)</c> immediately on
        ///       success, so the subsequent WS <c>x:CANCELED</c> is a duplicate and must be
        ///       dropped (0 events fired).
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Externally canceled (Web UI / another client)</term>
        ///     <description>
        ///       No Lean-side <c>CancelOrder</c> call was made — the order is still
        ///       <see cref="OrderStatus.Submitted"/>.  The WS event is the only notification
        ///       and must propagate (1 event fired with <see cref="OrderStatus.Canceled"/>).
        ///     </description>
        ///   </item>
        /// </list>
        /// </summary>
        [TestCase(OrderStatus.Canceled, 0, Description = "RestCanceled_WsDuplicate_IsDropped")]
        [TestCase(OrderStatus.Submitted, 1, Description = "ExternalCancel_Propagates")]
        public void OnFillOrderCanceledExecutionDeduplicatesRestAndPropagatesExternal(
            OrderStatus priorOrderStatus, int expectedEventCount)
        {
            const string orderId = "4904480369";

            using var brokerage = new TestableBinanceBrokerage();
            brokerage.TrackBrokerageId(orderId, priorOrderStatus);

            var eventsFired = new List<OrderEvent>();
            brokerage.OrdersStatusChanged += (_, events) => eventsFired.AddRange(events);

            var execution = JObject.Parse($@"{{
                ""s"": ""ACHUSDT"", ""S"": ""BUY"", ""o"": ""LIMIT"",
                ""x"": ""CANCELED"", ""X"": ""CANCELED"",
                ""i"": ""{orderId}"", ""si"": ""0"",
                ""l"": ""0"", ""L"": ""0"", ""n"": ""0"",
                ""T"": 1700000000000, ""t"": 0
            }}").ToObject<Execution>();

            brokerage.OnFillOrder(execution);

            Assert.AreEqual(expectedEventCount, eventsFired.Count,
                priorOrderStatus == OrderStatus.Canceled
                    ? "WS CANCELED must be dropped when CancelOrder (REST) already fired the event."
                    : "External cancel must propagate to Lean via OnFillOrder.");

            if (expectedEventCount > 0)
            {
                Assert.AreEqual(OrderStatus.Canceled, eventsFired[0].Status,
                    "The propagated event must carry Canceled status.");
            }
        }

        /// <summary>
        /// Reads a Newline-Delimited JSON (NDJSON) file where each top-level JSON object is one
        /// WS event and returns them in order.  Blank lines between objects are ignored.
        /// </summary>
        private static IEnumerable<JObject> ParseNdjson(string filePath)
        {
            var content = File.ReadAllText(filePath);
            using var reader = new JsonTextReader(new StringReader(content))
            {
                SupportMultipleContent = true
            };
            while (reader.Read())
            {
                yield return JObject.Load(reader);
            }
        }

        /// <summary>
        /// Minimal <see cref="BinanceBrokerage"/> subclass for offline unit tests.
        /// <para>
        /// Overrides <see cref="BinanceBrokerage.GetLeanOrder"/> so tests can control which broker
        /// IDs are "already known to Lean" without standing up a real algorithm or REST connection.
        /// </para>
        /// </summary>
        private sealed class TestableBinanceBrokerage : BinanceBrokerage
        {
            // Maps broker order ID => the Lean OrderStatus that GetLeanOrder should report.
            private readonly Dictionary<string, OrderStatus> _trackedIds =
                new(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Registers <paramref name="brokerageId"/> as an order that Lean already tracks
            /// with <see cref="OrderStatus.None"/> (sentinel — caller only checks for non-null).
            /// </summary>
            public void TrackBrokerageId(string brokerageId) =>
                _trackedIds[brokerageId] = OrderStatus.None;

            /// <summary>
            /// Registers <paramref name="brokerageId"/> with a specific <paramref name="status"/>
            /// so tests can simulate any lifecycle state (e.g. already-Canceled after REST cancel).
            /// </summary>
            public void TrackBrokerageId(string brokerageId, OrderStatus status) =>
                _trackedIds[brokerageId] = status;

            /// <inheritdoc/>
            internal override Orders.Order GetLeanOrder(string brokerageOrderId)
            {
                if (!string.IsNullOrEmpty(brokerageOrderId)
                    && !brokerageOrderId.Equals("0", StringComparison.OrdinalIgnoreCase)
                    && _trackedIds.TryGetValue(brokerageOrderId, out var status))
                {
                    return new MarketOrder { Status = status };
                }

                return null;
            }
        }
    }
}
