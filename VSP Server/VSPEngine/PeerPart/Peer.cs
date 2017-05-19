using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VSP_Server.VSPEngine.PeerPart {
	/// <summary>
	/// Класс, описывающий пира.
	/// </summary>
	class Peer {

		/// <summary>
		/// Соотносит номер команды с её назначением.
		/// </summary>
		public enum Command {
			/// <summary>
			/// Команда SayHello.
			/// </summary>
			SayHello = 1,
			/// <summary>
			/// Команда отключения от сервера.
			/// </summary>
			Disconnect = 255
		}
		TcpClient mTCP;
		NetworkStream mStream;
		RegistrationInfo mRegInfo;

		bool mIsBusy;
		Thread mThread;
		bool mInterrupted;

		/// <summary>
		/// Стандартный конструктор.
		/// </summary>
		/// <param name="tcp">TCP-клиент пира.</param>
		public Peer(TcpClient tcp) {
			mTCP = tcp;
			mStream = mTCP.GetStream();
			mIsBusy = false;
			mInterrupted = false;
			mThread = new Thread(new ThreadStart(workThread));
			mThread.Start();
		}
		/// <summary>
		/// Возращает состояние занятости пира.
		/// </summary>
		public bool IsBusy {
			get { return mIsBusy; }
		}
		/// <summary>
		/// Возвращает привязанный к пиру TcpClient.
		/// </summary>
		/// <returns>TcpClient.</returns>
		public TcpClient GetTcpClient() {
			return mTCP;
		}
		/// <summary>
		/// Возвращает адрес пира.
		/// </summary>
		/// <returns>Адрес.</returns>
		public String GetAddress() {
			return mTCP.Client.RemoteEndPoint.ToString();
		}

		private void workThread() {
			while (!mInterrupted) {
				Thread.Sleep(50);
				if (mStream.DataAvailable) {
					mIsBusy = true;
					byte command = (byte) mStream.ReadByte();
					while (GotCommand == null);
					GotCommand(this, Enum.GetName(typeof(Command),command));
					switch (command) {
						#region #1 - Say Hello!
						case 1:
							c1_sayHello();
							break;
						#endregion
						#region #255 - Disconnect
						case 255:
							c255_disconnect();
							break;
						#endregion
					}
					continue;
				}
				mIsBusy = false;
			}
		}

		#region Server commands handlers
		private void c1_sayHello() {
			mStream.WriteByte(5);
			mStream.Write(Encoding.UTF8.GetBytes("HELLO"), 0, 5);
		}
		private void c255_disconnect() {
			mStream.Close();
			mTCP.Close();
			mInterrupted = true;
			mIsBusy = false;
		}
		#endregion

		#region Events
		/// <summary>
		/// Предоставляет метод, обрабатывающий событие GotCommand.
		/// </summary>
		/// <param name="command">Команда.</param>
		public delegate void GotCommandHandler(Peer peer, String command);

		/// <summary>
		/// Вызывается, когда пир присылает команду на обработку.
		/// </summary>
		public event GotCommandHandler GotCommand;
		#endregion

		public override string ToString() {
			return String.Format("{0} ({1})", mRegInfo.Name, GetAddress());
		}
	}
}
