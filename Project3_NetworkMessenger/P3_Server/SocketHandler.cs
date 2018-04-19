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

                var sObj = System.Text.Encoding.Default.GetString(inbuffer);
                if(sObj[0] == 'M')
                {
                    await ParseMessage(sObj);
                }
                else if(sObj[0] == 'C')
                {
                    await CreateChannel(sObj);
                }
                

                Thread.Sleep(1000);

                //await EchoMessages();

            }

        }

        async Task ParseMessage(string sMess)
        {
            
            // Get and add message
            var message = JsonConvert.DeserializeObject<Message>(sMess.Substring(2));
            message.setID((ulong)Messages.list.Count);
            Messages.Add(message);

            // Echo message
            var outbuffer = System.Text.Encoding.Default.GetBytes("M:" + JsonConvert.SerializeObject(message));
            var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);
            
            // TODO: Send to all
            await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

        }

        async Task CreateChannel(string sChan)
        {

            // Get and add channel
            var channel = JsonConvert.DeserializeObject<Channel>(sChan.Substring(2));
            channel.setID((ulong)Channels.list.Count);
            Channels.Add(channel);

            // Echo channel
            var outbuffer = System.Text.Encoding.Default.GetBytes("C:" + JsonConvert.SerializeObject(channel));
            var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);

            // TODO: Send to all
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