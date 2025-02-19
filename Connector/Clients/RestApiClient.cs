using Connector.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace Connector.Clients
{
    public class RestApiClient
    {
        private readonly HttpClient _httpClient;

        public RestApiClient()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://api-pub.bitfinex.com/v2/") };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // Получение трейдов
        public async Task<List<Trade>> GetTradesAsync(string symbol, int limit = 100)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

            var url = $"trades/{symbol}/hist?limit={limit}";

            using (var response = await _httpClient.GetAsync(url))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error getting trades: {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonArray = JArray.Parse(content); 

                return jsonArray.ToObject<List<List<object>>>()?
                    .ConvertAll(x => new Trade
                    {
                        Id = Convert.ToInt64(x[0]),
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(x[1])).UtcDateTime,
                        Amount = Convert.ToDecimal(x[2]),
                        Price = Convert.ToDecimal(x[3])
                    });
            }
        }

        // Получение свечей
        public async Task<List<Candle>> GetCandlesAsync(string symbol, string timeframe, int limit = 100)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
            if (string.IsNullOrEmpty(timeframe))
                throw new ArgumentException("Timeframe cannot be null or empty", nameof(timeframe));

            var url = $"candles/trade:{timeframe}:{symbol}/hist?limit={limit}";

            using (var response = await _httpClient.GetAsync(url))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error getting candles: {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonArray = JArray.Parse(content); 

                return jsonArray.ToObject<List<List<decimal>>>()?
                    .ConvertAll(x => new Candle
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)x[0]).UtcDateTime,
                        Open = x[1],
                        Close = x[2],
                        High = x[3],
                        Low = x[4],
                        Volume = x[5]
                    });
            }
        }

        // Получение информации о тикере
        public async Task<Ticker> GetTickerAsync(string symbol, int retries = 3, int delayMs = 1000)
        {

            var url = $"ticker/{symbol}";
            Console.WriteLine($"Requesting URL: {url}");

            for (int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    using (var response = await _httpClient.GetAsync(url))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Attempt {attempt + 1}: Failed to get ticker for {symbol}. Status code: {response.StatusCode}");
                            continue;
                        }

                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ticker response for {symbol}: {content}");

                        var tickerData = JsonConvert.DeserializeObject<List<float>>(content);

                        if (tickerData == null || tickerData.Count < 10)
                        {
                            Console.WriteLine($"Attempt {attempt + 1}: Invalid data format for {symbol}: {content}");
                            continue;
                        }

                        return new Ticker
                        {
                            Bid = tickerData[0],
                            Ask = tickerData[2],
                            LastPrice = tickerData[6],
                            Volume = tickerData[7],
                            High = tickerData[8],
                            Low = tickerData[9]
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt + 1}: Exception while fetching ticker for {symbol}: {ex.Message}");
                }

                if (attempt < retries - 1)
                {
                    Console.WriteLine($"Retrying in {delayMs} ms...");
                    await Task.Delay(delayMs);
                }
            }

            Console.WriteLine($"Failed to fetch ticker for {symbol} after {retries} attempts.");
            return null;
        }

        public async Task<Dictionary<string, Ticker>> GetAllTickersAsync()
        {
            var url = "tickers?symbols=ALL";

            using (var response = await _httpClient.GetAsync(url))
            {
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error getting all tickers: {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"All tickers response: {content}");

                try
                {
                    // Десериализуем массив массивов
                    var tickerData = JsonConvert.DeserializeObject<List<List<object>>>(content);

                    var tickersDict = new Dictionary<string, Ticker>();

                    foreach (var ticker in tickerData)
                    {
                        if (ticker.Count < 2 || !(ticker[0] is string symbol))
                        {
                            Console.WriteLine("Invalid ticker format, skipping...");
                            continue;
                        }

                        if (!symbol.StartsWith("t"))
                        {
                            Console.WriteLine($"Skipping non-trading pair: {symbol}");
                            continue;
                        }

                        tickersDict[symbol] = new Ticker
                        {
                            Symbol = symbol,
                            Bid = float.Parse(ticker[1].ToString()),
                            Ask = float.Parse(ticker[3].ToString()),
                            LastPrice = float.Parse(ticker[7].ToString()),
                            Volume = float.Parse(ticker[7].ToString()),
                            High = float.Parse(ticker[8].ToString()),
                            Low = float.Parse(ticker[9].ToString())
                        };
                    }

                    return tickersDict;
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"Error deserializing all tickers: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
