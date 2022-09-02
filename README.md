![header-cheetah](https://user-images.githubusercontent.com/79997186/184224088-de4f3003-0c22-4a17-8cc7-b341b8e5b55d.png)

&nbsp;
&nbsp;
&nbsp;

## Introduction

This repository hosts the Binance and BinanceUS Brokerage Plugin Integration with the QuantConnect LEAN Algorithmic Trading Engine. LEAN is a brokerage agnostic operating system for quantitative finance. Thanks to open-source plugins such as this [LEAN](https://github.com/QuantConnect/Lean) can route strategies to almost any market.

[LEAN](https://github.com/QuantConnect/Lean) is maintained primarily by [QuantConnect](https://www.quantconnect.com), a US based technology company hosting a cloud algorithmic trading platform. QuantConnect has successfully hosted more than 200,000 live algorithms since 2015, and trades more than $1B volume per month.

### About Binance

<p align="center">
<picture >
 <source srcset="https://user-images.githubusercontent.com/79997186/188238225-e9f8275f-cd70-4778-beac-77311f28bc83.png">
 <img alt="binance" width="32%">
</picture>
<p>

[Binance](https://www.binance.com/) was founded by Changpeng Zhao in 2017 with the goal to "increase the freedom of money globally". Binance provides access to trading Crypto through spot markets and perpetual Futures. They serve clients with no minimum deposit when depositing Crypto. Binance also provides an NFT marketplace, a mining pool, and services to deposit Crypto coins in liquidity pools to earn rewards.

For more information about the Binance brokerage, see the [QuantConnect-Binance Integration Page](https://www.quantconnect.com/docs/v2/our-platform/live-trading/brokerages/binance).

## Using the Brokerage Plugin
  
### Deploying Binance with VSCode User Interace

  You can deploy using a visual interface in the QuantConnect Cloud. For more information see the [QuantConnect-Binance Integration Page](https://www.quantconnect.com/brokerages/binance).

![deploy-binance](https://user-images.githubusercontent.com/38889814/188243919-3a77ab4c-4c8b-40c4-97c9-b82114f09f35.gif)

  In the QuantConnect Cloud Platform you can harness the QuantConnect Live Data Feed. For most users this is substantially cheaper and easier than self-hosting.
  
### Deploying Binance with LEAN CLI

Follow these steps to start local live trading with the Binance brokerage:

1.  Open a terminal in your [CLI root directory](https://www.quantconnect.com/docs/v2/lean-cli/initialization/directory-structure#02-lean-init).
2.  Run lean live "`<projectName>`" to start a live deployment wizard for the project in ./`<projectName>` and then enter the brokerage number.

    ```
    $ lean live 'My Project'
     
    Select a brokerage:
    1. Paper Trading
    2. Interactive Brokers
    3. Tradier
    4. OANDA
    5. Bitfinex
    6. Coinbase Pro
    7. Binance
    8. Zerodha
    9. Samco
    10. Terminal Link
    11. Atreyu
    12. Trading Technologies
    13. Kraken
    14. FTX
    Enter an option: 
    ```
  
3.  Enter the number of the organization that has a subscription for the Binance module.

    ```
    $ lean live "My Project"

    Select the organization with the Binance module subscription:
    1. Organization 1
    2. Organization 2
    3. Organization 3
       Enter an option: 1
    ```

4.  Enter the exchange to use.

    ```
    $ lean live "My Project"

    Binance Exchange (Binance, BinanceUS): BinanceUS
    ```

5.  Enter the environment to use.

    ```
    $ lean live "My Project"

    Use the testnet? (live, paper): live
    ```

6.  Enter your API key id and secret.

    ```
    $ lean live "My Project"

    API key: 6d3ef5ca2d2fa52e4ee55624b0471261
    API secret: ********************************
    ```

    To create a new API key, see the API Management page on [Binance](https://www.binance.com/en/my/settings/api-management) or [Binance US](https://www.binance.us/en/usercenter/settings/api-management).

7.  Enter the number of the data feed to use and then follow the steps required for the data connection.

    ``` 
    $ lean live 'My Project'

    Select a data feed:
    1. Interactive Brokers
    2. Tradier
    3. Oanda
    4. Bitfinex
    5. Coinbase Pro
    6. Binance
    7. Zerodha
    8. Samco
    9. Terminal Link
    10. Trading Technologies
    11. Kraken
    12. FTX
    13. IQFeed
    14. Polygon Data Feed
    15. Custom Data Only
  
        To enter multiple options, separate them with comma:
    ```

8. View the result in the `<projectName>/live/<timestamp>` directory. Results are stored in real-time in JSON format. You can save results to a different directory by providing the `--output <path>` option in step 2.

If you already have a live environment configured in your [Lean configuration file](https://www.quantconnect.com/docs/v2/lean-cli/initialization/configuration#03-Lean-Configuration), you can skip the interactive wizard by providing the `--environment <value>` option in step 2. The value of this option must be the name of an environment which has `live-mode` set to true.

## Account Types

Binance supports cash and margin accounts, but Binance US only supports cash accounts.

## Order Types and Asset Classes

Our Binance and Binance US integrations support trading Crypto and the following order types:

- Market Order
- Limit Order
- Stop-Limit Order

## Downloading Data

For local deployment, the algorithm needs to download the following dataset:

[Binance Crypto Price Data](https://www.quantconnect.com/datasets/binance-crypto-price-data)  
[Binance US Crypto Price Data](https://www.quantconnect.com/datasets/binance-us-crypto-price-data)  


## Brokerage Model

Lean models the brokerage behavior for backtesting purposes. The margin model is used in live trading to avoid placing orders that will be rejected due to insufficient buying power.

You can set the Brokerage Model with the following statements

    SetBrokerageModel(BrokerageName.Binance, AccountType.Cash);
    SetBrokerageModel(BrokerageName.Binance, AccountType.Margin);

    SetBrokerageModel(BrokerageName.BinanceUS, AccountType.Cash)

[Read Documentation](https://www.quantconnect.com/docs/v2/our-platform/live-trading/brokerages/binance)

### Fees

We model the order fees of Binance and Binance US at the VIP 0 level, which is a 0.1% maker and taker fee. If you add liquidity to the order book by placing a limit order that doesn't cross the spread, you pay maker fees. If you remove liquidity from the order book by placing an order that crosses the spread, you pay taker fees. Binance adjusts your fees based on your 30-day trading volume and BNB balance, but we don't currently model these metrics to adjust fees. To check the latest fees at all the fee levels, see the [Trading Fees](https://www.binance.com/en/fee/schedule) page on the Binance.com website or the [Fee Structure](https://www.binance.us/en/fee/schedule) page on the Binance.us website. The Binance Spot Test Network does not charge order fees.

### Margin

We model buying power and margin calls to ensure your algorithm stays within the margin requirements.

#### Buying Power

Binance allows up to 3x leverage for margin accounts, but Binance US doesn't currently support margin accounts.

#### Margin Calls

Regulation T margin rules apply. When the amount of margin remaining in your portfolio drops below 5% of the total portfolio value, you receive a [warning](https://www.quantconnect.com/docs/v2/writing-algorithms/reality-modeling/margin-calls#08-Monitor-Margin-Call-Events). When the amount of margin remaining in your portfolio drops to zero or goes negative, the portfolio sorts the generated margin call orders by their unrealized profit and executes each order synchronously until your portfolio is within the margin requirements.

### Slippage

Orders through Binance and Binance US do not experience slippage in backtests. In live trading, your orders may experience slippage.

### Fills

We fill market orders immediately and completely in backtests. In live trading, if the quantity of your market orders exceeds the quantity available at the top of the order book, your orders are filled according to what is available in the order book.

### Settlements

Trades settle immediately after the transaction.

### Deposits and Withdraws

You can deposit and withdraw cash from your brokerage account while you run an algorithm that's connected to the account. We sync the algorithm's cash holdings with the cash holdings in your brokerage account every day at 7:45 AM Eastern Time (ET).

&nbsp;
&nbsp;
&nbsp;

![whats-lean](https://user-images.githubusercontent.com/79997186/184042682-2264a534-74f7-479e-9b88-72531661e35d.png)

&nbsp;
&nbsp;
&nbsp;

LEAN Engine is an open-source algorithmic trading engine built for easy strategy research, backtesting, and live trading. We integrate with common data providers and brokerages, so you can quickly deploy algorithmic trading strategies.

The core of the LEAN Engine is written in C#, but it operates seamlessly on Linux, Mac and Windows operating systems. To use it, you can write algorithms in Python 3.8 or C#. QuantConnect maintains the LEAN project and uses it to drive the web-based algorithmic trading platform on the website.

## Contributions

Contributions are warmly very welcomed but we ask you to read the existing code to see how it is formatted, commented and ensure contributions match the existing style. All code submissions must include accompanying tests. Please see the [contributor guide lines](https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md).

## Code of Conduct

We ask that our users adhere to the community [code of conduct](https://www.quantconnect.com/codeofconduct) to ensure QuantConnect remains a safe, healthy environment for
high quality quantitative trading discussions.

## License Model

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You
may obtain a copy of the License at

<http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language
governing permissions and limitations under the License.
