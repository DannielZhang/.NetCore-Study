using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketDemo
{
    public class WebSocketHandler
    {
        public const int _bufferSize = 4096;
        WebSocket socket;

        #region Extended Vars define zone

        /// <summary>
        /// 客户端Id-链接字典
        /// </summary>
        static Dictionary<string, WebSocket> ClientId_SocketDict = new Dictionary<string, WebSocket>();
        

        #endregion

        WebSocketHandler(WebSocket socket)
        {
            this.socket = socket;
        }

        async Task EchoLoop()
        {
            var buffer = new byte[_bufferSize];
            var seg = new ArraySegment<byte>(buffer);
            
            while(this.socket.State == WebSocketState.Open)
            {
                var incoming = await this.socket.ReceiveAsync(seg, CancellationToken.None);
                var recTime = DateTime.Now.ToString("HH:mm:ss.fff");
                
                //通过incoming的messageType 判断是否关闭
                if(incoming.MessageType == WebSocketMessageType.Close)
                {
                    if (ClientId_SocketDict.Values.Contains(this.socket))
                    {
                        ClientId_SocketDict.Remove(ClientId_SocketDict.First(p => p.Value == this.socket).Key);
                    }

                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    return;
                }

                //修改发送: 客户端Id, 客户端IP, 客户端发送时间
                var id = ClientId_SocketDict.First(p => p.Value == this.socket).Key;

                var outMsg = string.Format("Id:{0}, ReciveTime:{1}, Online:{2}",id, recTime, ClientId_SocketDict.Count);
                var outMsgBuff = Encoding.UTF8.GetBytes(outMsg);

                var outgoing = new ArraySegment<byte>(outMsgBuff, 0, outMsgBuff.Length);
                await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// 基本WebSocket Handshake
        /// </summary>
        /// <param name="hc"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        static async Task Acceptor(HttpContext hc, Func<Task> n)
        {
            if (!hc.WebSockets.IsWebSocketRequest)
                return;

            var socket = await hc.WebSockets.AcceptWebSocketAsync();
            var h = new WebSocketHandler(socket);
            await h.EchoLoop();
        }

        public static void Map(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(WebSocketHandler.Acceptor2);
        }

        /// <summary>
        /// Extended WebSocket Handshake
        /// </summary>
        /// <param name="hc"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        static async Task Acceptor2(HttpContext hc, Func<Task> n)
        {
            if (!hc.WebSockets.IsWebSocketRequest)
                return;
            //要求客户端的请求带参数： ?id=111 
            var clientId = hc.Request.Query["id"].ToString();

            var socket = await hc.WebSockets.AcceptWebSocketAsync();
            var h = new WebSocketHandler(socket);

            ClientId_SocketDict.Add(clientId, socket);

            await h.EchoLoop();
        }

    }
}
