using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Kombu
{
    internal class KombuServer
    {
        private KombuClientManager _clientManager;

        public KombuServer()
        {
            _clientManager = new KombuClientManager();
        }

        public void Listen(int port)
        {
            Task.Run(async () =>
            {
                try
                { 
                    // Httpリスナーを立ち上げ、クライアントからの接続を待つ
                    HttpListener httpListener = new HttpListener();
                    httpListener.Prefixes.Add($"http://127.0.0.1:{port}/kombu/");
                    httpListener.Start();

                    while (true) {
                        var httpContext = await httpListener.GetContextAsync();

                        // クライアントからのリクエストがWebSocketでない場合は処理を中断
                        if (!httpContext.Request.IsWebSocketRequest)
                        {
                            // クライアント側にエラー(400)を返却し接続を閉じる
                            httpContext.Response.StatusCode = 400;
                            httpContext.Response.Close();
                            continue;
                        }

                        var webSocketContext = await httpContext.AcceptWebSocketAsync(null);
                        OnNewClientConnected(webSocketContext.WebSocket);
                    }
                }
                catch (Exception e)
                {
                    System.IO.File.AppendAllText(@"kombu-server-error.log", e.ToString());
                }
            });
        }

        public void SendMessage(string message)
        {
            _clientManager.SendMessage(message).Wait();
        }

        private void OnNewClientConnected(WebSocket webSocket)
        {
            _clientManager.AddWebSocket(webSocket);
        }
    }
}
