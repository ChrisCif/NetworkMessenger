using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace P3_Server
{

    public static class Messages
    {

        public static List<Message> list = new List<Message>();

        public static void Add(Message mes)
        {
            list.Add(mes);
        }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Message
    {

        [JsonProperty]
        private string content;
        [JsonProperty]
        private string sender;
        [JsonProperty]
        private string channel;
        [JsonProperty]
        private DateTime timestamp;
        
        /*
        private string content;
        private User sender;
        private Channel channel;
        private DateTime timestamp;
        private ulong id;
        private int partID; // If the message is too long and needs to be broken up into parts (different messages)

        public Message(string content, User sender, Channel channel, DateTime timestamp, ulong id, int partID)
        {
            this.content = content;
            this.sender = sender;
            this.channel = channel;
            this.timestamp = timestamp;
            this.id = id;
            this.partID = partID;
        }
        */

    }

    public class User
    {
        private string username;
        private ulong id;
        private string password;

        public User(string username, ulong id, string password)
        {
            this.username = username;
            this.id = id;
            this.password = password;
        }
    }

    public class UserChannel
    {
        private User user;
        private Channel channel;

        public UserChannel(User user, Channel channel)
        {
            this.user = user;
            this.channel = channel;
        }
    }

    public class Channel
    {
        private ulong id;
        private string name;
        private User creator;

        public Channel(ulong id, string name, User creator)
        {
            this.id = id;
            this.name = name;
            this.creator = creator;
        }
    }

    public class UserChannelList
    {

        static List<UserChannel> list = new List<UserChannel>();

        public static void Add(UserChannel uc)
        {
            list.Add(uc);
        }

    }

}
