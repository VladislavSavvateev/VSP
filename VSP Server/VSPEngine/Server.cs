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
			bool status = LoadRegInfos();
			InformationLoading?.Invoke(status);

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
		/// Останавливает сервер.
		/// </summary>
		public void Stop() {
			mInterrupted = true;
			foreach (Peer p in mPeers) p.Disconnect();
			mListener.Stop();
			bool status = SaveRegInfos();
			InformationSaving?.Invoke(status);
		}

		/// <summary>
		/// Возвращает список всех пиров.
		/// </summary>
		public List<Peer> Peers {
			get { return mPeers; }
		}

		/// <summary>
		/// Возвращает список информации о регистрации пользователей.
		/// </summary>
		public List<RegistrationInfo> RegInfos {
			get { return mRegInfos; }
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

		private bool LoadRegInfos() {
			mRegInfos = new List<RegistrationInfo>(); // создаём пустой лист для хранения информации о регистрациях

			// проверяем наличие нужных для получения информации папок
			DirectoryInfo di = new DirectoryInfo(mDataFolder);
			if (!di.Exists) di.Create();
			if (di.GetDirectories("Users").Length == 0) {
				di.CreateSubdirectory("Users");
				return false;
			}

			// получаем информацию
			di = new DirectoryInfo(di.FullName + "\\Users");
			foreach (FileInfo fi in di.GetFiles()) {
				try {
					FileStream fs = fi.Open(FileMode.Open);
					String name = ReadString(fs, 1);
					String password = ReadString(fs, 1);
					String email = ReadString(fs, 1);
					fs.Close();
					mRegInfos.Add(new RegistrationInfo(name, password, email, RegistrationInfo.RegistrationStatus.CONFIRMED));
				} catch (Exception ex) { continue; }
			}
			return true;
		}
		private bool SaveRegInfos() {
			bool status = true;
			// проверяем наличие нужных для хранения информации папок
			DirectoryInfo di = new DirectoryInfo(mDataFolder);
			if (!di.Exists) di.Create();
			if (di.GetDirectories("Users").Length == 0) {
				di.CreateSubdirectory("Users");
			}
			
			// удаляем имеющуюся информацию с локального хранилища
			di = new DirectoryInfo(di.FullName + "\\Users");
			foreach (FileInfo fi in di.GetFiles()) {
				try {
					fi.Delete();
				} catch (Exception ex) { status = false; continue; }
			}

			// записываем новую информацию
			foreach (RegistrationInfo regInfo in mRegInfos) {
				if (regInfo.RegStatus == RegistrationInfo.RegistrationStatus.ON_CONFIRM) continue;
				BinaryWriter bw = new BinaryWriter(File.Open(String.Format("{0}\\{1}.usr", di.FullName, regInfo.Name), FileMode.Create));
				byte[] buffer = Encoding.UTF8.GetBytes(regInfo.Name);
				bw.Write((byte) buffer.Length);
				bw.Write(buffer);
				buffer = Encoding.UTF8.GetBytes(regInfo.Password);
				bw.Write((byte) buffer.Length);
				bw.Write(buffer);
				buffer = Encoding.UTF8.GetBytes(regInfo.Email);
				bw.Write((byte) buffer.Length);
				bw.Write(buffer);
				bw.Close();
			}
			return status;
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
			try {
				TcpClient tcp = mListener.EndAcceptTcpClient(result);
				Peer p = new Peer(this, tcp);
				mPeers.Add(p);
				PeerConnected(p);
				mListener.BeginAcceptTcpClient(new AsyncCallback(gotPeer), null);
			} catch (Exception ex) {}
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
		/// Предоставляет метод, обрабатывающий событие InformationLoaded.
		/// </summary>
		/// <param name="status">Статус операции.</param>
		public delegate void InformationLoadingHandler(bool status);
		/// <summary>
		/// Предоставляет метод, обрабатывающий событие InformationSaved.
		/// </summary>
		/// <param name="status">Статус операции.</param>
		public delegate void InformationSavingHandler(bool status);

		/// <summary>
		/// Вызывается, когда пир подключается к серверу.
		/// </summary>
		public event PeerConnectedHandler PeerConnected;
		/// <summary>
		/// Вызывается, когда пир отключается от сервера.
		/// </summary>
		public event PeerDisconnectedHandler PeerDisconnected;
		/// <summary>
		/// Вызывается, когда была предпринята попытка загрузить информацию.
		/// </summary>
		public event InformationLoadingHandler InformationLoading;
		/// <summary>
		/// Вызывается, когда была предпринята попытка сохранить информацию.
		/// </summary>
		public event InformationSavingHandler InformationSaving;
	}
}
