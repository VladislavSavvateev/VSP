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
		List<RegistrationInfo> mRegInfos;

		Thread mThread;
		bool mInterrupted;

		String mUserFolder;
		String mDataFolder;

		/// <summary>
		/// Основной конструктор.
		/// </summary>
		public Server() {
			mListener = new TcpListener(IPAddress.Any, 1209); // создаём экземпляр TcpListener
			mPeers = new List<Peer>(); // инициализируем движок пиров

			// загрузка локальной информации сервера
			mUserFolder = "Users";
			mDataFolder = "Data";
			LoadRegInfos();

			// инициализируем поток
			mThread = new Thread(new ThreadStart(workThread));
			mThread.Start();
			mInterrupted = false;
		}

		/// <summary>
		/// Запускает сервер.
		/// </summary>
		/// <returns>Результат запуска сервера.</returns>
		public bool Start() {
			// пытаемся запустить прослушиватель
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
		/// По умолчанию - Users
		/// </summary>
		public String UserFolder {
			get { return mUserFolder; }
			set {
				DirectoryInfo di = new DirectoryInfo(mUserFolder);
				if (!di.Exists) di.Create();
				mUserFolder = UserFolder;
			}
		}
		/// <summary>
		/// Возвращает или задаёт папку для хранения данных пользователей.
		/// По умолчанию - Data
		/// </summary>
		public String DataFolder {
			get { return mDataFolder; }
			set {
				DirectoryInfo di = new DirectoryInfo(mDataFolder);
				if (!di.Exists) di.Create();
				mDataFolder = DataFolder;
			}
		}


		public void LoadRegInfos() {
			mRegInfos = new List<RegistrationInfo>();
			DirectoryInfo di = new DirectoryInfo(mDataFolder);
			if (!di.Exists) di.Create();
			if (di.GetDirectories("Users").Length == 0) {
				di.CreateSubdirectory("Users");
				return;
			}
			di = new DirectoryInfo(di.FullName + "\\Users");
			foreach (FileInfo fi in di.GetFiles()) {
				try {
					FileStream fs = fi.Open(FileMode.Open);
					String name = ReadString(fs, 1);
					String password = ReadString(fs, 1);
					String email = ReadString(fs, 1);
					mRegInfos.Add(new RegistrationInfo(name, password, email, RegistrationInfo.RegistrationStatus.CONFIRMED));
				} catch (Exception ex) { continue; }
			}
		}
		// удаляем пиры, если они отключаются
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

		private String ReadString(Stream stream, int lengthOfLength) {
			long length = 0;
			for (int i = 0; i < lengthOfLength; i++) {
				int val = stream.ReadByte();
				if (val == -1) return null;
				length = (length << 8) + val;
			}
			byte[] buffer = new byte[length];
			if (stream.Read(buffer, 0, (int) length) != length) return null;
			return Encoding.UTF8.GetString(buffer);
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
