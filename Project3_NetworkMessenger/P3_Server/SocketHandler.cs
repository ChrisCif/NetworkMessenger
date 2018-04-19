using System;
using System.Collections.Generic;
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

        //WebSocket socket;
        public List<WebSocket> sockets;
        public List<Channel> channels;
        
        SocketHandler()
        {
            this.sockets = new List<WebSocket>();
            this.channels = new List<Channel>();
        }

        async Task ReceiveMessages()
        {

            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);

            while (OpenSockets())
            {
                foreach(WebSocket ws in sockets)
                {
                    if(ws.State == WebSocketState.Open)
                    {

                        var inbuffer = new byte[BufferSize];

                        await ws.ReceiveAsync(inbuffer, CancellationToken.None);

                        var sObj = System.Text.Encoding.Default.GetString(inbuffer);
                        if (sObj[0] == 'M')
                        {
                            await ParseMessage(sObj);
                        }
                        else if (sObj[0] == 'C')
                        {
                            await CreateChannel(sObj);
                        }
                        else if (sObj[0] == 'U')
                        {
                            await AddUser(sObj);
                        }

                    }
                }   
                Thread.Sleep(1000);
            }

        }
        bool OpenSockets()
        {
            bool open = false;

            foreach(WebSocket ws in sockets)
            {

                if (ws.State == WebSocketState.Open)
                    open = true;

            }

            return open;
        }

        async Task ParseMessage(string sMess)
        {
            
            // Get and add message
            var message = JsonConvert.DeserializeObject<Message>(sMess.Substring(2));
            message.setID(Int32.Parse(message.getChannel().getID() + "" + message.getChannel().getMessages().Count));
            
            var destinationChannel = channels[message.getChannel().getID()];
            destinationChannel.getMessages().Add(message);

            // Echo message
            var outbuffer = System.Text.Encoding.Default.GetBytes("M:" + JsonConvert.SerializeObject(message));
            var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);

            // Send to every user in the channel
            foreach(User u in destinationChannel.getParticipants())
            {
                await sockets[u.getID()].SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            
        }

        async Task CreateChannel(string sChan)
        {

            // Get and add channel
            var channel = JsonConvert.DeserializeObject<Channel>(sChan.Substring(2));
            channel.setID(channels.Count);
            channels.Add(channel);

            // Echo channel
            var outbuffer = System.Text.Encoding.Default.GetBytes("C:" + JsonConvert.SerializeObject(channel));
            var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);

            // Send to creator
            await sockets[channel.getCreator().getID()].SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

        }

        async Task AddUser(string sUser)
        {

            // Get and add user
            var user = JsonConvert.DeserializeObject<User>(sUser.Substring(2));
            user.setID(sockets.Count - 1);
           
            // Echo user
            var outbuffer = System.Text.Encoding.Default.GetBytes("U:" + JsonConvert.SerializeObject(user));
            var outgoing = new ArraySegment<byte>(outbuffer, 0, outbuffer.Length);

            // Send to correct web socket
            await sockets[user.getID()].SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

        }
        
        public async Task Acceptor(HttpContext hc, Func<Task> n)
        {

            if (!hc.WebSockets.IsWebSocketRequest)
            {
                await hc.Response.WriteAsync("Not a web socket request");
                return;
            }

            while (true)
            {
                var socket = await hc.WebSockets.AcceptWebSocketAsync();
                sockets.Add(socket);
                await ReceiveMessages();
            }
            
        }

        public static void Map(IApplicationBuilder app)
        {
            app.UseWebSockets();
            var sh = new SocketHandler();
            app.Use(sh.Acceptor);
        }
    }



}