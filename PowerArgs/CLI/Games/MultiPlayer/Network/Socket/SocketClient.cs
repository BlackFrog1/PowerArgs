﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class SocketClientNetworkProvider : Lifetime, IClientNetworkProvider
    {
        public Event<Exception> ListenException { get; private set; } = new Event<Exception>();
        public string ClientId { get; private set; }

        public Event<string> MessageReceived { get; private set; } = new Event<string>();

        private Socket client;
        public Promise Connect(ServerInfo server)
        {
            var d = Deferred.Create();
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                if (IPAddress.TryParse(server.Server, out IPAddress ip))
                {
                    client.Connect(ip, server.Port);
                }
                else
                {
                    client.Connect(server.Server, server.Port);
                }
                this.ClientId = (client.LocalEndPoint as IPEndPoint).Address + ":"+ (client.LocalEndPoint as IPEndPoint).Port;
                Thread t = new Thread(ListenForMessages);
                t.IsBackground = true;
                t.Start(d);
            }
            catch (Exception ex)
            {
                d.Reject(ex);
            }
            return d.Promise;
        }

        private void ListenForMessages(object deferred)
        {
            try
            {
                Deferred d = deferred as Deferred;
                d.Resolve();
                byte[] buffer = new byte[1024 * 1024];
                while (this.IsExpired == false)
                {
                    SocketHelpers.Read(this, client, buffer, 4);
                    if (this.IsExpired) return;
                    var messageLength = BitConverter.ToInt32(buffer, 0);
                    SocketHelpers.Read(this, client, buffer, messageLength);
                    if (this.IsExpired) return;
                    var messageText = Encoding.UTF8.GetString(buffer, 0, messageLength);
                    MessageReceived.Fire(messageText);
                }
            }
            catch(Exception ex)
            {
                ListenException.Fire(ex);
                this.Dispose();
            }
        }

        public void SendMessage(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            var sent = client.Send(lengthBytes);
            if (sent != lengthBytes.Length) throw new Exception("WTF");
            sent = client.Send(bytes);
            if (sent != bytes.Length) throw new Exception("WTF");
        }

        protected override void DisposeManagedResources()
        {
            client?.Close();
        }
    }
}
