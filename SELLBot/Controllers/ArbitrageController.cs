using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SELLBot.Models;

namespace SELLBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArbitrageController : ControllerBase
    {
        public Root configuration;
        public DateTime now;
        public int counter = 0;

        public ArbitrageController()
        {
            string path = @"D:\Documents\Repos\SELLBot\SELLBot\Configuration\coinsData.json";
            this.configuration = JsonConvert.DeserializeObject<Root>(System.IO.File.ReadAllText(path))!;
            now = DateTime.Today;
        }


        [HttpPost]
        public async Task<ResponseMessage> CreateOrder(OrdersInput order)
        {
            try
            {
                var ordersList = new List<NewOrder>();
                IExchange exchange = IExchange.Binance;

                string mainBaseFTXCoin = IExchange.Binance == exchange ?
                    configuration.BaseCoins.LastOrDefault()!.ConfigCoins.FirstOrDefault()!.Name! :
                    configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.FirstOrDefault()!.Name!;


                //var mainBaseFTXCoin = configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.FirstOrDefault()!.Name;
                var quantity = order.quantity / order.ask;
                ResponseMessage response = new ResponseMessage();
                BinanceClient client = new BinanceClient();

                var apiCredentials = new BinanceApiCredentials(configuration.Exchange_ApiData.LastOrDefault()!.ApiKey, configuration.Exchange_ApiData.LastOrDefault()!.Secret);

                client.SetApiCredentials(apiCredentials);

                var firstOrder = new NewOrder(OrderSide.Sell)
                {
                    symbol = mainBaseFTXCoin,
                    quantity = order.quantity,
                    price = order.lastPrice,
                };

                var secondOrder = new NewOrder(OrderSide.Buy)
                {
                    symbol = order.buyer,
                    quantity = Math.Round(quantity, Convert.ToInt16(order.coinDecimals)),
                    price = order.bid
                };

                var thirdOrder = new NewOrder(OrderSide.Sell)
                {
                    symbol = order.seller,
                    quantity = Math.Round(quantity, Convert.ToInt16(order.coinDecimals)),
                    price = order.ask,
                };

                long firstOrderId = 0;
                long secondOrderId = 0;
                var firstOrderSent = false;
                var secondOrderSent = false;

                var message = "";


                var firstOrderResponse = await client.SpotApi.Trading.PlaceOrderAsync
                    (
                    firstOrder.symbol, (OrderSide)firstOrder.orderSide,
                    (SpotOrderType)firstOrder.spotOrderType, quantity: firstOrder.quantity,
                    null, null, (decimal)firstOrder.price,
                    firstOrder.timeInForce
                    );

                if (!firstOrderResponse.Success)
                {
                    response.Message += firstOrderResponse.Error!.ToString() + ". ";
                    return response;
                }

                var firstOrderTime = DateTime.Now;
                firstOrderId = firstOrderResponse.Data.Id;
                firstOrderSent = true;
                while (firstOrderSent)
                {
                    var firstOrderInfo = await client.SpotApi.Trading.GetOrderAsync(firstOrder.symbol, firstOrderId);
                    var firstOrderData = firstOrderInfo.Data;

                    var refDate = DateTime.Now;
                    TimeSpan ts = refDate - firstOrderTime;

                    if (ts.Seconds >= 30)
                    {
                        firstOrderSent = false;
                        secondOrderSent = false;

                        if (firstOrderData.Status != OrderStatus.PartiallyFilled)
                        {
                            var res = await client.SpotApi.Trading.CancelOrderAsync(firstOrder.symbol, firstOrderId);

                            message = "First order was cancelled. Reached time limit.";
                        }
                    }

                    if (firstOrderData.Status == OrderStatus.Filled)
                    {
                        firstOrderSent = false;

                        var secondOrderResp = await client.SpotApi.Trading.PlaceOrderAsync(secondOrder.symbol,
                                (OrderSide)secondOrder.orderSide,
                                (SpotOrderType)secondOrder.spotOrderType,
                                secondOrder.quantity, null, null,
                                (decimal)secondOrder.price,
                                secondOrder.timeInForce);

                        if (!secondOrderResp.Success)
                        {
                            response.Message += secondOrderResp.Error!.ToString() + ". ";
                            return response;
                        }
                        secondOrderId = secondOrderResp.Data.Id;
                        secondOrderSent = true;
                    }
                }
                var secondOrderTime = DateTime.Now;
                while (secondOrderSent)
                {
                    var secondOrderInfo = await client.SpotApi.Trading.GetOrderAsync(secondOrder.symbol, secondOrderId);
                    var secondOrderData = secondOrderInfo.Data;

                    var secondRefDate = DateTime.Now;
                    TimeSpan tsp = secondRefDate - secondOrderTime;

                    //if (tsp.Minutes >= 15)
                    //{
                    //    var respo = IExchange.Binance == exchange ?
                    //        await client.SpotApi.Trading.CancelOrderAsync(secondOrder.symbol, secondOrderId) :
                    //        await client.TradeApi.CommonSpotClient.CancelOrderAsync(secondOrderId);

                    //    message = "Second order was cancelled. Reached time limit.";
                    //    //Logic for other sell 

                    //    var secondOrderReverse = new NewOrder(OrderSide.Sell)
                    //    {
                    //        symbol = order.buyer,
                    //        quantity = quantity,
                    //        price = (order.price * Convert.ToDecimal(1.1)),
                    //    };

                    //    var secondOrderReverseResponse = IExchange.Binance == exchange ?
                    //        await client.SpotApi.Trading.PlaceOrderAsync(
                    //            secondOrderReverse.symbol,
                    //            (OrderSide)secondOrderReverse.orderSide,
                    //            (SpotOrderType)secondOrderReverse.spotOrderType,
                    //            secondOrderReverse.quantity, null, null,
                    //            (decimal)secondOrderReverse.price,
                    //            secondOrderReverse.timeInForce) :
                    //        await client.TradeApi.CommonSpotClient.PlaceOrderAsync(
                    //            secondOrderReverse.symbol,
                    //            (CommonOrderSide)secondOrderReverse.orderSide,
                    //            (CommonOrderType)secondOrderReverse.spotOrderType,
                    //            secondOrderReverse.quantity,
                    //            (decimal)secondOrderReverse.price);

                    //    secondOrderSent = false;

                    //    continue;
                    //}

                    if (secondOrderData.Status == OrderStatus.Filled)
                    {
                        secondOrderSent = false;
                        var thirdOrderResponse = await client.SpotApi.Trading.PlaceOrderAsync(
                                thirdOrder.symbol,
                                (OrderSide)thirdOrder.orderSide,
                                (SpotOrderType)thirdOrder.spotOrderType,
                                thirdOrder.quantity, null, null,
                                (decimal)thirdOrder.price,
                                thirdOrder.timeInForce) ;

                        if (!thirdOrderResponse.Success)
                        {
                            response.Message += thirdOrderResponse.Error!.ToString() + ". ";
                            return response;
                        }

                        message = "All orders placed succesfully.";
                    }
                }

                response.Message += message;

                return response;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ". " + ex.StackTrace);
            }
        }

        [HttpGet]
        public async Task<List<ArbitrageResult>> Get()
        {
            try
            {
                IExchange exchange = IExchange.Binance;
                List<ArbitrageResult> result = new List<ArbitrageResult>();
                DateTime date = new DateTime(now.Year, now.Month, now.Day, Convert.ToInt16(configuration.StartProcess_Hour), Convert.ToInt16(configuration.StartProcess_Minute), 0);
                string mainBaseCoinFTX = IExchange.Binance == exchange ?
                    configuration.BaseCoins.LastOrDefault()!.ConfigCoins.FirstOrDefault()!.Name! :
                    configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.FirstOrDefault()!.Name!;
                string secondBaseCoinFTX = IExchange.Binance == exchange ?
                    configuration.BaseCoins.LastOrDefault()!.ConfigCoins.LastOrDefault()!.Name! :
                    configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.LastOrDefault()!.Name!;
                List<Coin> coins = new List<Coin>();
                BinanceClient client = new BinanceClient();

                var apiCredentials = new BinanceApiCredentials(configuration.Exchange_ApiData.LastOrDefault()!.ApiKey, configuration.Exchange_ApiData.LastOrDefault()!.Secret);

                client.SetApiCredentials(apiCredentials);

                var coinsInfo = await client.SpotApi.ExchangeData.GetTickersAsync();

                IEnumerable<dynamic> coinsData = coinsInfo.Data;

                decimal bid_BTCUSDT = (decimal)coinsData.FirstOrDefault(t => t.Symbol == mainBaseCoinFTX)!.BestBidPrice!;
                decimal ask_BTCUSDT = (decimal)coinsData.FirstOrDefault(t => t.Symbol == mainBaseCoinFTX)!.BestAskPrice!;
                decimal bid_ETHUSDT =(decimal)coinsData.FirstOrDefault(t => t.Symbol == secondBaseCoinFTX)!.BestBidPrice!;


                var page_BTCPrice = (decimal)coinsData.FirstOrDefault(t => t.Symbol == mainBaseCoinFTX)!.LastPrice!;
                var page_ETHPrice = (decimal)coinsData.FirstOrDefault(t => t.Symbol == secondBaseCoinFTX)!.LastPrice!;

                var arbitrageResult = new ArbitrageResult();
                arbitrageResult.Exchange = IExchange.Binance == exchange ? "binance" : "ftx";
                arbitrageResult.Coins = new List<CoinsList>();
                arbitrageResult.BTC = page_BTCPrice;
                arbitrageResult.ETH = page_ETHPrice;

                foreach (var orderCoin in configuration.TradeCoins)
                {
                    dynamic addCoin = new Coin();
                    string concatChar = IExchange.Binance == exchange ? "" : "/";

                    addCoin.Symbol = orderCoin.Name;
                    addCoin.BTC = orderCoin.Name + configuration.Sufixes.FirstOrDefault()!.Sufix;
                    addCoin.USDT = orderCoin.Name + configuration.Sufixes.LastOrDefault()!.Sufix;
                    addCoin.BTCFTX = orderCoin.Name + concatChar + configuration.Sufixes.FirstOrDefault()!.Sufix;
                    addCoin.USDTFTX = orderCoin.Name + concatChar + configuration.Sufixes.LastOrDefault()!.Sufix;
                    addCoin.IsAutomatic = IExchange.Binance == exchange ?
                        configuration.AutomaticProcess_Coins.LastOrDefault()!.ConfigCoins.Where(t => t.Name == orderCoin.Name).Any() :
                        configuration.AutomaticProcess_Coins.FirstOrDefault()!.ConfigCoins.Where(t => t.Name == orderCoin.Name).Any();

                    coins.Add(addCoin);
                    //coins.Add(new Coin()
                    //{
                    //    Symbol = orderCoin.Name,
                    //    BTC = orderCoin.Name + configuration.Sufixes.FirstOrDefault()!.Sufix,
                    //    USDT = orderCoin.Name + configuration.Sufixes.LastOrDefault()!.Sufix,
                    //    BTCFTX = orderCoin.Name + "/" + configuration.Sufixes.FirstOrDefault()!.Sufix,
                    //    USDTFTX = orderCoin.Name + "/" + configuration.Sufixes.LastOrDefault()!.Sufix,
                    //    IsAutomatic = configuration.AutomaticProcess_Coins.FirstOrDefault()!.ConfigCoins.Where(t => t.Name == orderCoin.Name).Any(),
                    //});
                }

                decimal diff = 0;
                decimal diffLastPrice = 0;
                double directionalRatio = 0;
                bool canOperateCandle = false;
                BinanceClient bClient = new BinanceClient();
                bClient.SetApiCredentials(apiCredentials);

                foreach (var coin in coins)
                {
                    IBinanceTick coinBidI = IExchange.Binance == exchange ?
                        coinsData.FirstOrDefault(t => t.Symbol == coin.USDT)! :
                        coinsData.FirstOrDefault(t => t.Name == coin.USDT)!;
                    var coinBid = coinBidI!.BestBidPrice!;
                    IBinanceTick coinAskI = IExchange.Binance == exchange ?
                        coinsData.FirstOrDefault(t => t.Symbol == coin.BTCFTX)! :
                        coinsData.FirstOrDefault(t => t.Name == coin.BTCFTX)!;
                    var coinAsk = coinAskI!.BestAskPrice!;
                    var calculatedVal = (((1 * ask_BTCUSDT) / coinBid) * coinAsk) > 1 ? (((1 * ask_BTCUSDT) / coinBid) * coinAsk) - 1 : 0;

                    if (coin.IsAutomatic)
                    {
                        diff = 0;
                        diffLastPrice = 0;
                        directionalRatio = 0;
                        canOperateCandle = false;


                        var opnOrders = await bClient.SpotApi.CommonSpotClient.GetOpenOrdersAsync();

                        var ordersBTCInfo = await bClient.SpotApi.CommonSpotClient.GetOpenOrdersAsync(coin.BTCFTX);

                        var ordersUSDTInfo = await bClient.SpotApi.CommonSpotClient.GetOpenOrdersAsync(coin.USDTFTX);
                        
                        var btcOrders = await bClient.SpotApi.CommonSpotClient.GetOpenOrdersAsync(mainBaseCoinFTX);

                        bool openedOrders = (
                                            ordersBTCInfo.Data.Any() ||
                                            ordersUSDTInfo.Data.Any() ||
                                            btcOrders.Data.Any() ||
                                            opnOrders.Data.Where(t => t.Symbol == coin.BTCFTX).Any() ||
                                            opnOrders.Data.Where(t => t.Symbol == coin.USDTFTX).Any() ||
                                            opnOrders.Data.Where(t => t.Symbol == mainBaseCoinFTX).Any())
                                            ? true : false;
                        decimal perc = Math.Round(((calculatedVal / 1) * 100), 5);
                        DateTime refDate = date.AddMinutes(Convert.ToInt16(configuration.AutomaticProcess_Duration));

                        var historicPricesB = await bClient.SpotApi.ExchangeData.GetKlinesAsync("BTCUSDT",
                                KlineInterval.FifteenMinutes, DateTime.Today.Date, DateTime.Today.Date.AddHours(24));

                        //var historicPricesF = await fClient.TradeApi.ExchangeData.GetKlinesAsync("BTC/USDT",
                        //        FTX.Net.Enums.KlineInterval.FifteenMinutes, DateTime.Today.Date, DateTime.Today.Date.AddHours(24));

                        var mm20RecordsB = historicPricesB.Data.OrderByDescending(t => t.OpenTime).Take(20).ToList();
                        //var mm20RecordsF = historicPricesF.Data.OrderByDescending(t => t.OpenTime).Take(20).ToList();
                        var mm8RecordsB = mm20RecordsB.Take(8);
                        //var mm8RecordsF = mm20RecordsF.Take(8);
                        var mm8B = mm8RecordsB.Sum(t => t.ClosePrice) / mm8RecordsB.Count();
                        //var mm8F = mm8RecordsF.Sum(t => t.ClosePrice) / mm8RecordsF.Count();
                        var mm20B = mm20RecordsB.Sum(t => t.ClosePrice) / mm20RecordsB.Count();
                        //var mm20F = mm20RecordsF.Sum(t => t.ClosePrice) / mm20RecordsF.Count();
                        var lastPriceB = historicPricesB.Data.OrderByDescending(t => t.OpenTime).FirstOrDefault();
                        //var lastPriceF = historicPricesF.Data.OrderByDescending(t => t.OpenTime).FirstOrDefault();
                        var movementSpeedB = Convert.ToDouble(Math.Abs((lastPriceB!.ClosePrice - mm8RecordsB.LastOrDefault()!.ClosePrice) / mm8RecordsB.Count()));
                        //var movementSpeedF = Convert.ToDouble(Math.Abs((lastPriceF!.ClosePrice - mm8RecordsF.LastOrDefault()!.ClosePrice) / mm8RecordsF.Count()));
                        var standarDeviationRecordsB = mm20RecordsB.Take(9);
                        //var standarDeviationRecordsF = mm20RecordsF.Take(9);
                        var mm3RecordsB = mm20RecordsB.Take(4);
                        //var mm3RecordsF = mm20RecordsF.Take(4);

                        var appliedDiffList = new List<double>();

                        var deviationList = standarDeviationRecordsB.Select(t => Convert.ToDouble(t.ClosePrice)).ToList();
                        //var deviationListF = standarDeviationRecordsF.Select(t => Convert.ToDouble(t.ClosePrice)).ToList();
                        for (int i = 0; i < deviationList.Count() - 1; i++)
                        {
                            appliedDiffList.Add(deviationList[i + 1] - deviationList[i]);
                        }

                        var organizedList = appliedDiffList.AsEnumerable<double>();
                        var volatility = CalculateStandardDeviation(organizedList);

                        diff = Math.Abs(mm20B - mm8B);
                        var diffVal = mm8B < mm20B ? mm8B : mm20B;
                        var val = (diff / 2) + diffVal;
                        diffLastPrice = Math.Abs(lastPriceB!.ClosePrice - val);
                        directionalRatio = movementSpeedB / volatility;
                        var coinsState = new List<Candle>();
                        int counter = 0;

                        foreach (var c in mm3RecordsB)
                        {
                            switch (counter)
                            {
                                case 0:

                                    break;
                                case 1:
                                    coinsState.Add(new Candle() { Order = "Third", OpenPrice = c.OpenPrice, ClosePrice = c.ClosePrice });
                                    break;
                                case 2:
                                    coinsState.Add(new Candle() { Order = "Second", OpenPrice = c.OpenPrice, ClosePrice = c.ClosePrice });
                                    break;
                                case 3:
                                    coinsState.Add(new Candle() { Order = "First", OpenPrice = c.OpenPrice, ClosePrice = c.ClosePrice });
                                    break;
                            }
                            counter += 1;
                        }

                        counter = 0;

                        foreach (var it in coinsState)
                        {
                            it.Type = it.OpenPrice > it.ClosePrice ? "Red" : it.OpenPrice < it.ClosePrice ? "Green" : "N/A";
                        }

                        canOperateCandle = !(coinsState.Where(t => t.Type == "Red").Count() == 3
                            || coinsState.Where(t => t.Type == "Green").Count() == 3);

                        if (
                            perc > (decimal)configuration.ArbitragePercentageValue &&
                            !openedOrders
                            //DateTime.Now >= date &&
                            //DateTime.Now <= refDate && diff < 53 &&
                            //diffLastPrice < 53 && 
                            //directionalRatio < 0.4 && canOperateCandle
                            )
                        {
                            //var decimalNumb = coinBidI.BestAskQuantity.ToString("0.########");
                            int decimalQuantity = 0;

                            decimalQuantity = configuration!.AutomaticProcess_Coins!.FirstOrDefault()!.ConfigCoins!.Where(t => t.Name == coin.Symbol).FirstOrDefault()!.CoinDecimals;

                            //var cut = decimalNumb != null ? decimalNumb.Replace(",", ".").Split('.') : new string[0];
                            //if (cut.Count() > 1) decimalQuantity = Convert.ToInt32(cut[1].Length);

                            await CreateOrder(new OrdersInput()
                            {
                                buyer = coin.USDTFTX,
                                seller = coin.BTCFTX,
                                price = coinBid,
                                quantity = configuration.DefaultQuantity,
                                bid = coinBid,
                                ask = coinAsk,
                                lastPrice = ask_BTCUSDT,
                                exchange = "binance",
                                percentage = Math.Round(((calculatedVal / 1) * 100), 5).ToString() + "%",
                                coinDecimals = decimalQuantity
                            });
                        }
                    }
                }

                var myOrdersInfoB = await bClient.SpotApi.CommonSpotClient.GetOpenOrdersAsync();
                //var myOrdersInfoF = await client.TradeApi.CommonSpotClient.GetOpenOrdersAsync();
                var myOrders = myOrdersInfoB.Data;
                foreach (var coin in coins)
                {
                    var coinBid = IExchange.Binance == exchange ?
                        (decimal)coinsData.FirstOrDefault(t => t.Symbol == coin.BTCFTX)!.BestBidPrice! :
                        (decimal)coinsData.FirstOrDefault(t => t.Name == coin.BTCFTX)!.BestBidPrice!;
                    var coinAsk = IExchange.Binance == exchange ?
                        (decimal)coinsData.FirstOrDefault(t => t.Symbol == coin.USDTFTX)!.BestAskPrice! :
                        (decimal)coinsData.FirstOrDefault(t => t.Name == coin.USDTFTX)!.BestAskPrice!;

                    var valFTX = (((1 / coinBid) * coinAsk) / bid_BTCUSDT) > 1 ? (((1 / coinBid) * coinAsk) / bid_BTCUSDT) - 1 : 0;
                    var refDate = date.AddMinutes(Convert.ToInt16(configuration.AutomaticProcess_Duration));
                    var timeToFinish = "OFF";

                    if (DateTime.Now >= date && DateTime.Now <= refDate)
                    {
                        TimeSpan ts = refDate - DateTime.Now;

                        timeToFinish = ts.Hours.ToString().PadLeft(2, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');

                    }

                    arbitrageResult.TimeToFinish = timeToFinish;
                    arbitrageResult.Difference = diff;
                    arbitrageResult.LastPriceDifference = diffLastPrice;
                    arbitrageResult.DirectionalRatio = directionalRatio;
                    arbitrageResult.CandleCanOperate = canOperateCandle;

                    arbitrageResult.Coins.Add(new CoinsList()
                    {
                        Symbol = coin.Symbol,
                        Percentage = Math.Round(((valFTX / 1) * 100), 5).ToString() + "%",
                        BTC = coin.BTCFTX,
                        USDT = coin.USDTFTX,
                        Price = coinBid,
                        Quantity = Convert.ToDouble(configuration.DefaultQuantity),
                        FirstQuantity = Math.Round(coinAsk, 3),
                        LastPrice = Math.Round(bid_BTCUSDT, 3),
                        HasOpendOrders = (myOrders.Where(t => t.Symbol == coin.BTCFTX).Any() || myOrders.Where(t => t.Symbol == coin.USDTFTX).Any()) ? true : false,
                    });
                }

                result.Add(arbitrageResult);

                return result;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ". " + ex.StackTrace);
            }
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double standardDeviation = 0;

            if (values.Any())
            {
                // Compute the average.     
                double avg = values.Average();

                // Perform the Sum of (value-avg)_2_2.      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                // Put it all together.      
                standardDeviation = Math.Sqrt((sum) / (values.Count() - 1));
            }

            return standardDeviation;
        }
    }
}