using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Connector.Clients
{
    public class WebSocketClient : IDisposable
    {
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly BlockingCollection<string> _messageQueue = new BlockingCollection<string>();
        private readonly string _wsUrl = "wss://api-pub.bitfinex.com/ws/2";

        public WebSocketClient()
        {
            Task.Run(StartReceivingMessages);
        }

        public async Task ConnectAsync()
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                await _webSocket.ConnectAsync(new Uri(_wsUrl), _cancellationTokenSource.Token);
            }
        }

        public async Task SubscribeTradesAsync(string symbol)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            var subscriptionMessage = JsonConvert.SerializeObject(new
            {
                @event = "subscribe",
                channel = "trades",
                symbol = symbol.StartsWith("t") ? symbol : $"t{symbol}"
            });

            await SendAsync(subscriptionMessage);
        }

        public async Task SubscribeCandlesAsync(string symbol, string timeframe)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            var subscriptionMessage = JsonConvert.SerializeObject(new
            {
                @event = "subscribe",
                channel = "candles",
                key = $"trade:{timeframe}:{(symbol.StartsWith("t") ? symbol : $"t{symbol}")}"
            });

            await SendAsync(subscriptionMessage);
        }

        private async Task SendAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }

        private async Task StartReceivingMessages()
        {
            try
            {
                await ConnectAsync();

                var buffer = new byte[1024 * 4];
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationTokenSource.Token);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _messageQueue.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving messages: {ex.Message}");
            }
        }

        public IEnumerable<string> GetMessages()
        {
            return _messageQueue.GetConsumingEnumerable(_cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _webSocket.Dispose();
            _messageQueue.Dispose();
        }
    }
}
