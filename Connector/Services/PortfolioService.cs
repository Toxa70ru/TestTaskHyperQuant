using Connector.Clients;
using Connector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connector.Services
{
    public class PortfolioService
    {
        private readonly RestApiClient _restClient;

        // Начальный баланс портфеля
        private readonly Dictionary<string, float> _balances = new Dictionary<string, float>
        {
            { "BTC", 1 },
            { "XRP", 15000 },
            { "XMR", 50 },
            { "DSH", 30 }
        };

        // Целевые валюты для расчета портфеля
        private readonly List<string> _targetCurrencies = new List<string> { "USD", "BTC", "XRP", "XMR", "DSH" };

        public List<string> _allSymbols = new List<string>(); 

        public PortfolioService(RestApiClient restClient)
        {
            _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
        }

        /// <summary>
        /// Рассчитывает общий баланс портфеля в каждой из целевых валют.
        /// </summary>
        /// <returns>Словарь с результатами в формате: Currency => TotalValue.</returns>
        public async Task<Dictionary<string, float>> CalculatePortfolioAsync()
        {
            
            var portfolio = new Dictionary<string, float>();
            var allTickers = await _restClient.GetAllTickersAsync();

            foreach (var tick in allTickers) 
            {
                _allSymbols.Add(tick.Key);
            }

            if (allTickers == null)
            {
                Console.WriteLine("Failed to load tickers, cannot calculate portfolio.");
                return portfolio;
            }

            foreach (var targetCurrency in _targetCurrencies)
            {
                float totalValue = 0;

                foreach (var balance in _balances)
                {
                    var conversionRate = GetConversionRate(allTickers, balance.Key, targetCurrency);

                    if (conversionRate > 0)
                    {
                        totalValue += balance.Value * conversionRate;
                    }
                    else if (balance.Key == targetCurrency)
                    {
                        totalValue += balance.Value; // Если валюта совпадает с целевой, цена = 1
                    }
                    else
                    {
                        Console.WriteLine($"Skipping invalid or missing conversion rate for {balance.Key}/{targetCurrency}");
                    }
                }

                portfolio[targetCurrency] = (float)Math.Round(totalValue, 2);
            }

            return portfolio;
        }

        /// <summary>
        /// Возвращает допустимую валютную пару для торговли.
        /// </summary>
        /// <param name="baseCurrency">Базовая валюта.</param>
        /// <param name="quoteCurrency">Котируемая валюта.</param>
        /// <returns>Допустимая валютная пара или null, если пара недопустима.</returns>
        private string GetValidPair(string baseCurrency, string quoteCurrency)
        {
            string pair = $"t{baseCurrency}{quoteCurrency}";

            // Проверяем, что пара начинается с допустимого префикса
            foreach (var prefix in _allSymbols)
            {
                if (pair.Contains(prefix))
                {
                    return pair;
                }
            }

            pair = $"t{quoteCurrency}{baseCurrency}";
            foreach (var prefix in _allSymbols)
            {
                if (pair.Contains(prefix))
                {
                    return pair;
                }
            }

            Console.WriteLine($"Invalid pair format: {pair}");
            return null;
        }

        private float GetConversionRate(Dictionary<string, Ticker> tickers, string baseCurrency, string quoteCurrency)
        {
            string directPair = GetValidPair(baseCurrency, quoteCurrency);

            if (!string.IsNullOrEmpty(directPair) && tickers.ContainsKey(directPair))
            {
                return tickers[directPair].LastPrice;
            }

            // Индиректная конвертация через BTC
            string btcPair = GetValidPair(baseCurrency, "BTC");
            string btcQuotePair = GetValidPair("BTC", quoteCurrency);

            if (!string.IsNullOrEmpty(btcPair) && !string.IsNullOrEmpty(btcQuotePair) &&
                tickers.ContainsKey(btcPair) && tickers.ContainsKey(btcQuotePair))
            {
                var btcRate = tickers[btcPair].LastPrice;
                var quoteBtcRate = tickers[btcQuotePair].LastPrice;

                if (btcRate > 0 && quoteBtcRate > 0)
                {
                    return baseCurrency == "BTC" ? quoteBtcRate : btcRate / quoteBtcRate;
                }
            }

            Console.WriteLine($"Unable to calculate conversion rate for {baseCurrency}/{quoteCurrency}");
            return (float)0m;
        }
    }
}
