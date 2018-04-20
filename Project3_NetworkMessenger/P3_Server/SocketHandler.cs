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
        public List<User> users;
        
        SocketHandler()
        {
            this.sockets = new List<WebSocket>();
            this.channels = new List<Channel>();

            // Pub Channel
            channels.Add(new Channel(0, "Pub"));

        }

        async Task ReceiveMessages(WebSocket ws)
        {

            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);

            while (ws.State == WebSocketState.Open)
            {

                var inbuffer = new byte[BufferSize];
                var result = await ws.ReceiveAsync(inbuffer, CancellationToken.None);

                // Close socket when finished
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed in server by the client", CancellationToken.None);
                    return;
                }

                // Check message type
                var sObj = System.Text.Encoding.Default.GetString(inbuffer);
                if (sObj[0] == 'M')
                {
                    // Message
                    await ParseMessage(sObj);
                }
                else if (sObj[0] == 'C')
                {
                    // New Channel
                    await CreateChannel(sObj);
                }
                else if (sObj[0] == 'U')
                {
                    // New User
                    await AddUser(sObj);
                }

            }

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

            // Add to Pub
            channels[0].addParticipant(user);
            var c_outbuffer = System.Text.Encoding.Default.GetBytes("C:" + JsonConvert.SerializeObject(channels[0]));
            var c_outgoing = new ArraySegment<byte>(c_outbuffer, 0, c_outbuffer.Length);
            await sockets[user.getID()].SendAsync(c_outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

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

                Console.WriteLine("Accepted web socket");

                sockets.Add(socket);

                await ReceiveMessages(socket);
                //Thread.Sleep(1000);
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