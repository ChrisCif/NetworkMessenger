using System;
using System.Globalization;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace P3_Server
{

    public class SocketHandler
    {
        public const int BufferSize = 4096;

        WebSocket socket;

        SocketHandler(WebSocket socket)
        {
            this.socket = socket;
        }

        async Task ReceiveMessages()
        {

            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);

            while (this.socket.State == WebSocketState.Open)
            {
                
                var inbuffer = new byte[BufferSize];

                await this.socket.ReceiveAsync(inbuffer, CancellationToken.None);
                await ParseMessage(inbuffer);

                Thread.Sleep(1000);

                //await EchoMessages();

            }

        }

        async Task ParseMessage(byte[] buffer)
        {
            
            var sMess = System.Text.Encoding.Default.GetString(buffer);
            var message = JsonConvert.DeserializeObject<Message>(sMess);
            
            Messages.Add(message);

            var outbuffer = System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(message));
            var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);
            await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

        }

        async Task EchoMessages()
        {

            foreach(Message m in Messages.list)
            {

                var outbuffer = System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(m));
                var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);
                await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

            }

        }

        static async Task Acceptor(HttpContext hc, Func<Task> n)
        {
            if (!hc.WebSockets.IsWebSocketRequest)
            {
                await hc.Response.WriteAsync("Not a web socket request");
                return;
            }

            while (true)
            {
                var socket = await hc.WebSockets.AcceptWebSocketAsync();
                var h = new SocketHandler(socket);
                await h.ReceiveMessages();
            }
            
        }

        public static void Map(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(SocketHandler.Acceptor);
        }
    }



}