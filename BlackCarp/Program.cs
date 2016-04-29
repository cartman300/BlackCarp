using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BlackCarp {
	class Program {
		static Action<ChatClient, string> CreateMessageRoute(ChatClient To) {
			return (This, Msg) => {
				ChatServer.SendMessage(Msg, MessageType.ChatMessage, To);
			};
		}

		static Action<ChatClient> CreateClosedRoute(ChatClient To) {
			return (This) => {
				ChatServer.SendMessage("Other party disconnected", MessageType.ServerInfo, To);
				Lobby.Enqueue(To);
			};
		}

		static void Main(string[] args) {
			Console.Title = "BlackCarp";

			ChatServer.Init();

			Thread Dispatcher = new Thread(() => {
				while (true) {
					while (Lobby.GetCount() < 2)
						Thread.Sleep(100);

					ChatClient A = Lobby.Dequeue();
					ChatClient B = Lobby.Dequeue();

					A.OnMessageEvent = CreateMessageRoute(B);
					A.OnClosedEvent = CreateClosedRoute(B);
					B.OnMessageEvent = CreateMessageRoute(A);
					B.OnClosedEvent = CreateClosedRoute(A);

					ChatServer.SendMessage("Found partner", MessageType.ServerInfo, new[] { A, B });
				}
			});
			Dispatcher.IsBackground = true;
			Dispatcher.Start();

			Console.WriteLine("Running!");
			Console.ReadLine();
			Console.WriteLine("Done!");
		}
	}
}