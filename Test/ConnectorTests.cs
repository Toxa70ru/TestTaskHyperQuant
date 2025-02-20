using Connector.Clients;
using Connector.Models;
using Connector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    [TestClass]
    public class ConnectorTests
    {
        private RestApiClient _restClient;
        private PortfolioService _portfolioService;

        [TestInitialize]
        public void Initialize()
        {
            _restClient = new RestApiClient();
            _portfolioService = new PortfolioService(_restClient);
        }

        /// <summary>
        /// Тест получения трейдов.
        /// </summary>
        [TestMethod]
        public async Task TestGetTradesAsync()
        {
            var trades = await _restClient.GetTradesAsync("tBTCUSD", limit: 10);

            Assert.IsNotNull(trades, "Список трейдов не должен быть null.");
            Assert.IsTrue(trades.Count > 0, "Список трейдов должен содержать элементы.");
            Assert.IsInstanceOfType(trades[0], typeof(Trade), "Элементы должны быть типа Trade.");
        }

        /// <summary>
        /// Тест получения свечей.
        /// </summary>
        [TestMethod]
        public async Task TestGetCandlesAsync()
        {
            var candles = await _restClient.GetCandlesAsync("tBTCUSD", "1D", limit: 10);

            Assert.IsNotNull(candles, "Список свечей не должен быть null.");
            Assert.IsTrue(candles.Count > 0, "Список свечей должен содержать элементы.");
            Assert.IsInstanceOfType(candles[0], typeof(Candle), "Элементы должны быть типа Candle.");
        }

        /// <summary>
        /// Тест получения информации о тикере.
        /// </summary>
        [TestMethod]
        public async Task TestGetTickerAsync()
        {
            var ticker = await _restClient.GetTickerAsync("tBTCUSD");

            Assert.IsNotNull(ticker, "Тикер не должен быть null.");
            Assert.IsTrue(ticker.LastPrice > 0, "Последняя цена должна быть больше нуля.");
        }

        /// <summary>
        /// Тест расчета портфеля.
        /// </summary>
        [TestMethod]
        public async Task TestCalculatePortfolioAsync()
        {
            var portfolio = await _portfolioService.CalculatePortfolioAsync();

            Assert.IsNotNull(portfolio, "Результат не должен быть null.");
            Assert.AreEqual(5, portfolio.Count, "Должно быть 5 валют в результате.");

            foreach (var item in portfolio)
            {
                Console.WriteLine($"{item.Key}: {item.Value}");
                Assert.IsTrue(item.Value >= 0, $"Значение для {item.Key} должно быть неотрицательным.");
            }
        }

        /// <summary>
        /// Тест проверки недопустимой валютной пары.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task TestInvalidSymbol()
        {
            await _restClient.GetTickerAsync("INVALIDPAIR");
        }
    }
}

