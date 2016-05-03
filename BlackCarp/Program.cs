using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace BlackCarp {
	class Program {
		public static string Print(string Msg) {
			Msg = string.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), Msg);
			Console.WriteLine(Msg);
			File.AppendAllText(string.Format("{0}log.txt", DateTime.Now.ToString("dd_MM_yyyy_")), Msg + "\n");
			return Msg + "\n";
		}

		public static string Print(string Fmt, params object[] Args) {
			return Print(string.Format(Fmt, Args));
		}

		static Action<ChatClient, string> CreateMessageRoute(ChatClient To, string FileName) {
			return (This, Msg) => {
				File.AppendAllText(FileName, Print("{0} to {1}: {2}", This, To, Msg));
				ChatServer.SendMessage(Msg, MessageType.ChatMessage, To);
			};
		}

		static Action<ChatClient> CreateClosedRoute(ChatClient To) {
			return (This) => {
				Print("{0} and {1} unlinked", This, To);
				ChatServer.SendMessage("Other party disconnected", MessageType.ServerInfo, To);
				Lobby.Enqueue(To);
			};
		}

		static void Main(string[] args) {
			Console.Title = "BlackCarp";
			ChatServer.Init();
			int LogFile = 0;

			Thread Dispatcher = new Thread(() => {
				while (true) {
					while (Lobby.GetCount() < 2)
						Thread.Sleep(100);

					ChatClient A = Lobby.Dequeue();
					ChatClient B = Lobby.Dequeue();

					string LogFileName = "log_" + LogFile + ".txt";
					LogFile++;

					A.OnMessageEvent = CreateMessageRoute(B, LogFileName);
					A.OnClosedEvent = CreateClosedRoute(B);
					B.OnMessageEvent = CreateMessageRoute(A, LogFileName);
					B.OnClosedEvent = CreateClosedRoute(A);

					Print("{0} and {1} linked", A, B);
					ChatServer.SendMessage("Found partner", MessageType.ServerInfo, new[] { A, B });
				}
			});
			Dispatcher.IsBackground = true;
			Dispatcher.Start();

			Print("Server started");
			while (true)
				Thread.Sleep(1000);
		}
	}
}