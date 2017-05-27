using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSP_Server.VSPEngine;
using VSP_Server.VSPEngine.PeerPart;
using static VSP_Server.VSPEngine.PeerPart.Peer;

namespace VSP_Server {
	class Program {

		static Server mServer;

		static void Main(string[] args) {
			Console.WriteLine("=== VSP SERVER v1.0 ===");
			mServer = new Server();
			mServer.PeerConnected += PeerConnected;
			mServer.PeerDisconnected += PeerDisconnected;
			Console.CancelKeyPress += ExitServer;
			report(TypeOfReport.OK, "Server initialized!");
			if (mServer.Start()) report(TypeOfReport.OK, "Server started!");
			else {
				report(TypeOfReport.FAIL, "Starting server was failed!");
				return;
			}
			Thread.Sleep(-1);
		}

		public static void ExitServer(object sender, ConsoleCancelEventArgs args) {
			mServer.Stop();
			report(TypeOfReport.OK, "Server stopped!");
			Environment.Exit(0);
		}

		private static void PeerConnected(Peer peer) {
			report(TypeOfReport.INFO, "Peer connected!", peer);
			peer.GotCommand += PeerCommand;
		}
		private static void PeerDisconnected(Peer peer) {
			report(TypeOfReport.INFO, "Peer disconnected!", peer);
			peer.GotCommand -= PeerCommand;
		}

		private static void PeerCommand(Peer peer, String command) {
			report(TypeOfReport.INFO, String.Format("Command: {0}", command), peer);
		}

		enum TypeOfReport {
			INFO,
			FAIL,
			OK,
			UNKNOWN
		}
		static void report(TypeOfReport type, String val) {
			report(type, val, null);
		}
		static void report(TypeOfReport type, String val, Peer peer) {
			Console.Write("[ ");
			switch (type) {
				case TypeOfReport.INFO:
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write("INFO");
				break;
				case TypeOfReport.OK:
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(" OK ");
				break;
				case TypeOfReport.FAIL:
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("FAIL");
				break;
				case TypeOfReport.UNKNOWN:
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.Write("????");
				break;
			}
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(" ] ");
			if (peer != null) {
				Console.Write("{");
				Console.Write("{0}", peer.ToString());
				Console.Write("} ");
			}
			Console.WriteLine(val);
		}
	}
}
