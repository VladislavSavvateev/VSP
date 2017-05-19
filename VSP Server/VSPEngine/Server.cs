using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSP_Server.VSPEngine.PeerPart;

namespace VSP_Server.VSPEngine {
	/// <summary>
	///	Класс, обеспечивающий работу серверной части протокола VSP.
	/// </summary>
	class Server {
		TcpListener mListener;
		List<Peer> mPeers;

		Thread mThread;
		bool mInterrupted;

		String mUserFolder;

		/// <summary>
		/// Основной конструктор.
		/// </summary>
		public Server() {
			mListener = new TcpListener(IPAddress.Any, 1209); // creating instance of TCPListener
			mPeers = new List<Peer>(); // init peers engine
			mUserFolder = "/Users";

			// init thread
			mThread = new Thread(new ThreadStart(workThread));
			mThread.Start();
			mInterrupted = false;
		}

		/// <summary>
		/// Запускает сервер.
		/// </summary>
		/// <returns>Результат запуска сервера.</returns>
		public bool Start() {
			// trying to start listener
			try {
				mListener.Start();
				mListener.BeginAcceptTcpClient(new AsyncCallback(gotPeer), null);
			} catch (Exception ex) { return false; }
			return true;
		}

		/// <summary>
		/// Возвращает список всех пиров.
		/// </summary>
		public List<Peer> Peers {
			get { return mPeers; }
		}

		/// <summary>
		/// Возвращает или задаёт папку для хранения данных пользователей.
		/// </summary>
		public String UserFolder {
			get { return mUserFolder; }
			set {
				DirectoryInfo di = new DirectoryInfo(mUserFolder);
				if (!di.Exists) di.Create();
				mUserFolder = UserFolder;
			}
		}

		// delete peers if they disconnected
		private void workThread() {
			while (!mInterrupted) {
				try {
					for (int i = mPeers.Count - 1; i >= 0; i--) {
						Peer p = mPeers[i];
						if (!p.IsBusy) {
							TcpClient tcp = p.GetTcpClient();
							if (!tcp.Connected) {
								PeerDisconnected(mPeers[i]);
								mPeers.RemoveAt(i);
							}
						}
					}
				} catch (Exception ex) { Console.WriteLine(ex.Message); }
				Thread.Sleep(50);
			}
		}

		private void gotPeer(IAsyncResult result) {
			TcpClient tcp = mListener.EndAcceptTcpClient(result);
			Peer p = new Peer(tcp);
			mPeers.Add(p);
			PeerConnected(p);
			mListener.BeginAcceptTcpClient(new AsyncCallback(gotPeer), null);
		}

		/// <summary>
		/// Предоставляет метод, обрабатывающий событие PeerConnected.
		/// </summary>
		/// <param name="peer">Клиент (пир).</param>
		public delegate void PeerConnectedHandler(Peer peer);
		/// <summary>
		/// Предоставляет метод, обрабатывающий событие PeerDisconnected.
		/// </summary>
		/// <param name="peer">Клиент (пир).</param>
		public delegate void PeerDisconnectedHandler(Peer peer);

		/// <summary>
		/// Вызывается, когда пир подключается к серверу.
		/// </summary>
		public event PeerConnectedHandler PeerConnected;
		/// <summary>
		/// Вызывается, когда пир отключается от сервера.
		/// </summary>
		public event PeerDisconnectedHandler PeerDisconnected;
	}
}
