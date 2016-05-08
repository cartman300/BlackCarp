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
		public static WebSocketServer WSS;
		static List<ChatClient> Clients;

		public static void Init() {
			Clients = new List<ChatClient>();
			WSS = new WebSocketServer(IPAddress.Any, 10000);
			WSS.AddWebSocketService<ChatClient>("/blackcarp");
			WSS.Start();
		}

		public static void Purge() {
			ChatClient[] Cl = Clients.ToArray();
			for (int i = 0; i < Cl.Length; i++)
				if ((DateTime.Now - Cl[i].LastMessage > TimeSpan.FromMinutes(3)))
					Drop(Cl[i], "AFK");
		}

		public static IEnumerable<ChatClient> GetClientsExcept(params ChatClient[] Except) {
			foreach (var C in Clients)
				if (!Except.Contains(C))
					yield return C;
		}

		public static void OnOpen(ChatClient C) {
			if (!Clients.Contains(C)) {
				Clients.Add(C);
				Program.Print("{0} joined", C);
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

		public static void Drop(ChatClient C, string Reason) {
			SendMessage("Dropped from server: " + Reason, MessageType.ServerInfo, C);
			C.Drop();
		}
	}

	class ChatClient : WebSocketBehavior {
		static int CID = 0;

		public int ClientID = CID++;
		public bool Valid;
		public Action<ChatClient, string> OnMessageEvent;
		public Action<ChatClient> OnClosedEvent;
		public DateTime LastMessage;
		IPEndPoint EndPoint;

		protected override void OnOpen() {
			Valid = true;
			LastMessage = DateTime.Now;
			EndPoint = Context.UserEndPoint;
			ChatServer.OnOpen(this);
		}

		protected override void OnClose(CloseEventArgs e) {
			Valid = false;
			if (OnClosedEvent != null)
				OnClosedEvent(this);
			ChatServer.OnClose(this);
		}

		protected override void OnError(WebSocketSharp.ErrorEventArgs e) {
			Program.Print("Error in {0}", this);
		}

		protected override void OnMessage(MessageEventArgs e) {
			LastMessage = DateTime.Now;
			if (e.IsText && OnMessageEvent != null)
				OnMessageEvent(this, e.Data);
		}

		public void SendMessage(string Msg) {
			if (Valid)
				Send(Msg);
		}

		public void Drop(CloseStatusCode Code = CloseStatusCode.Normal) {
			Context.WebSocket.Close(Code);
		}

		public override string ToString() {
			return string.Format("({0}, {1})", ClientID, EndPoint);
		}
	}
}