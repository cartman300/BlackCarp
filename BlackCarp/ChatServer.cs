using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BlackCarp {
	enum MessageType {
		ChatMessage = 0,
		ServerInfo = 1,
	}

	static class ChatServer {
		static WebSocketServer WSS;
		static List<ChatClient> Clients;

		public static void Init() {
			Clients = new List<ChatClient>();
			WSS = new WebSocketServer(IPAddress.Any, 10000);
			WSS.AddWebSocketService<ChatClient>("/blackcarp");
			WSS.Start();
		}

		public static IEnumerable<ChatClient> GetClientsExcept(params ChatClient[] Except) {
			foreach (var C in Clients)
				if (!Except.Contains(C))
					yield return C;
		}

		public static void OnOpen(ChatClient C) {
			if (!Clients.Contains(C)) {
				Clients.Add(C);
				Lobby.Enqueue(C);
			}
		}

		public static void OnClose(ChatClient C) {
			if (Clients.Contains(C)) {
				Clients.Remove(C);
				Lobby.Remove(C);
			}
		}

		public static void SendMessage(string Msg, MessageType T, ChatClient Client) {
			if (Client.Valid)
				Client.SendMessage(((int)T).ToString() + Msg);
		}

		public static void SendMessage(string Msg, MessageType T, IEnumerable<ChatClient> Clients) {
			foreach (var C in Clients)
				SendMessage(Msg, T, C);
		}
	}

	class ChatClient : WebSocketBehavior {
		static int CID = 0;

		public int ClientID = CID++;
		public bool Valid;
		public Action<ChatClient, string> OnMessageEvent;
		public Action<ChatClient> OnClosedEvent;

		protected override void OnOpen() {
			Valid = true;
			ChatServer.OnOpen(this);
		}

		protected override void OnClose(CloseEventArgs e) {
			Valid = false;
			if (OnClosedEvent != null)
				OnClosedEvent(this);
			ChatServer.OnClose(this);
		}
		
		protected override void OnMessage(MessageEventArgs e) {
			if (e.IsText && OnMessageEvent != null)
				OnMessageEvent(this, e.Data);
		}

		public void SendMessage(string Msg) {
			if (Valid)
				Send(Msg);
		}

		public override string ToString() {
			return string.Format("Client({0})", ClientID);
		}
	}
}