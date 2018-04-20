using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace P3_Server
{
    
    [JsonObject(MemberSerialization.OptIn)]
    public class Message
    {
        
        [JsonProperty]
        private string content;
        [JsonProperty]
        private User sender;
        [JsonProperty]
        private Channel channel;
        [JsonProperty]
        private DateTime timestamp;
        [JsonProperty]
        private int id;
        [JsonProperty]
        private int partID; // If the message is too long and needs to be broken up into parts (different messages)
        
        public void setID(int id)
        {
            this.id = id;
        }

        public User getUser() { return sender; }
        public Channel getChannel() { return channel; }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class User
    {
        [JsonProperty]
        private string username;
        [JsonProperty]
        private int id;
        //private string password;

        public void setID(int id) {
            this.id = id;
        }
        
        public int getID() { return id; }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Channel
    {
        [JsonProperty]
        private int id;
        [JsonProperty]
        private string name;
        [JsonProperty]
        private User creator;
        [JsonProperty]
        private List<User> participants;
        [JsonProperty]
        private List<Message> messages;

        public Channel(int id, string name)
        {
            this.id = id;
            this.name = name;
            this.creator = new User();
            this.participants = new List<User>();
            this.messages = new List<Message>();
        }

        public void addParticipant(User user)
        {
            participants.Add(user);
        }
        public void setID(int id)
        {
            this.id = id;
        }
        
        public int getID() { return id; }
        public string getName() { return name; }
        public List<User> getParticipants() { return participants; }
        public User getCreator() { return creator; }
        public List<Message> getMessages() { return messages; }

    }

}
