using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackCarp {
	static class Lobby {
		static LinkedList<ChatClient> Clients;

		static Lobby() {
			Clients = new LinkedList<ChatClient>();
		}

		public static bool Contains(ChatClient C) {
			return Clients.Contains(C);
		}

		public static int GetCount() {
			lock (Clients) {
				return Clients.Count;
			}
		}

		public static void Remove(ChatClient C) {
			lock (Clients) {
				if (Clients.Contains(C))
					Clients.Remove(C);
			}
		}

		public static void Enqueue(ChatClient C) {
			lock (Clients) {
				Remove(C);
				Clients.AddLast(C);
				ChatServer.SendMessage("Queueing for partner", MessageType.ServerInfo, C );
			}
		}

		public static ChatClient Dequeue() {
			lock (Clients) {
				ChatClient Ret = null;

				if (GetCount() > 0) {
					Ret = Clients.First.Value;
					Clients.RemoveFirst();
				}

				return Ret;
			}
		}
	}
}