using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Kombu
{
    internal class KombuClientManager
    {
        private readonly object @lock = new object();
        private readonly List<WebSocket> _clients;

        public KombuClientManager()
        {
            _clients = new List<WebSocket>();
        }

        public void AddWebSocket(WebSocket webSocket)
        {
            lock (@lock)
            {
                _clients.Add(webSocket);
            }

            Task.Run(async () =>
            {
                var message = await webSocket.ReceiveAsync(new ArraySegment<byte>(), CancellationToken.None);

                if (message.MessageType == WebSocketMessageType.Close)
                {
                    lock (@lock)
                    {
                        _clients.Remove(webSocket);
                    }
                }
            });
        }

        public async Task SendMessage(string message)
        {
            WebSocket[] currentClients;
            lock (@lock)
            {
                currentClients = _clients.ToArray();
            }

            foreach (var client in currentClients)
            {
                try
                { 
                    var data = System.Text.Encoding.UTF8.GetBytes(message);
                    var segment = new ArraySegment<byte>(data);
                    await client.SendAsync(segment, WebSocketMessageType.Text, true, default);
                }
                catch
                {
                    lock (@lock)
                    {
                        _clients.Remove(client);
                    }
                }
            }
        }
    }
}
