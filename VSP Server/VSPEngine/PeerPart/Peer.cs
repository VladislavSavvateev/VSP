using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
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
			/// Команда регистрации.
			/// </summary>
			Register = 252,
			/// <summary>
			/// Команда подтверждения регистрации.
			/// </summary>
			Confirmation = 253,
			/// <summary>
			/// Команда логина.
			/// </summary>
			Login = 254, 
			/// <summary>
			/// Команда отключения от сервера.
			/// </summary>
			Disconnect = 255
		}
		TcpClient mTCP;
		NetworkStream mStream;
		Server mServer;
		RegistrationInfo mRegInfo;

		bool mIsBusy;
		Thread mThread;
		bool mInterrupted;
		String mLastString;

		/// <summary>
		/// Стандартный конструктор.
		/// </summary>
		/// <param name="tcp">TCP-клиент пира.</param>
		public Peer(Server s, TcpClient tcp) {
			mTCP = tcp;
			mServer = s;
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
		/// <summary>
		/// Отключает пира от сервера.
		/// </summary>
		public void Disconnect() {
			mInterrupted = true;
			mStream.Close();
			mTCP.Close();
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
						#region #252 - Register (send request)
						case 252:
							c252_register();
							break;
						#endregion
						#region #253 - Register (confirmation)
						case 253:
							c253_confirmation();
							break;
						#endregion
						#region #254 - Login
						case 254:
							c254_login();
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
		private void c252_register() {
			try {
				// получаем данные
				String name = getString();
				String password = getString();
				String email = getString();

				if (name == null) throw new Exception();
				if (name.Length < 3) throw new Exception();

				if (password == null) throw new Exception();
				if (password.Length < 3) throw new Exception();

				if (email == null) throw new Exception();
				//if (!SqlMethods.Like(email, "*@*.*")) throw new Exception();

				// проверяем на наличие уже имеющейся регинфы
				RegistrationInfo ri = new RegistrationInfo(name, password, email, RegistrationInfo.RegistrationStatus.ON_CONFIRM);
				bool isExists = false;
				foreach (RegistrationInfo ri_ in mServer.RegInfos) {
					if (ri_.Equals(ri)) {
						isExists = true;
						if (ri_.RegStatus == RegistrationInfo.RegistrationStatus.ON_CONFIRM) ri = ri_;
						else throw new Exception();
						break;
					}
				}
				mRegInfo = ri;
				
				// если такого нет, то добавляем
				if (!isExists) mServer.RegInfos.Add(ri);
				EmailSender.SendRegCode(ri);
				mStream.WriteByte(1);
				mStream.Write(BitConverter.GetBytes(ri.Token), 0, 8);
			} catch (Exception ex) { mStream.WriteByte(0); }
		}
		private void c253_confirmation() {
			try {
				// получаем данные
				byte[] token_raw = new byte[8];
				for (int i = 0; i < 8; i++) {
					int val = mStream.ReadByte();
					if (val == -1) throw new Exception();
					token_raw[i] = (byte) val;
				}
				byte[] code_raw = new byte[4];
				for (int i = 0; i < 4; i++) {
					int val = mStream.ReadByte();
					if (val == -1) throw new Exception();
					code_raw[i] = (byte) val;
				}

				// проверяем совпадение данных
				long token = BitConverter.ToInt64(token_raw, 0);
				if (mRegInfo.Token != token) throw new Exception();
				int code = BitConverter.ToInt32(code_raw, 0);
				if (mRegInfo.RegCode != code) throw new Exception();

				mRegInfo.RegStatus = RegistrationInfo.RegistrationStatus.CONFIRMED;
				mStream.WriteByte(1);
			} catch	(Exception ex) { mStream.WriteByte(0); }
		}
		private void c254_login() {
			String name = getString();
			String password = getString();
			RegistrationInfo ri = null;
			foreach (RegistrationInfo ri_ in mServer.RegInfos) 
				if (ri_.Name.Equals(name) && ri_.Password.Equals(password)) {
					ri = ri_;
					break;
				}
			if (ri == null) mStream.WriteByte(0);
			else {
				ri.RegenerateToken();
				mStream.WriteByte(1);
				mStream.Write(BitConverter.GetBytes(ri.Token), 0, 8);
				mRegInfo = ri;
			}
		}
		private void c255_disconnect() {
			mStream.Close();
			mTCP.Close();
			mInterrupted = true;
			mIsBusy = false;
		}
		#endregion

		#region Tools
		private String getString() {
			return getString(1);
		}
		private String getString(int lengthOfLength) {
			int length = 0;
			for (int i = 0; i < lengthOfLength; i++) {
				int val = mStream.ReadByte();
				if (val == -1) return null;
				length = (length << 8) + val;
			}
			byte[] buffer = new byte[length];
			if (mStream.Read(buffer, 0, length) != length) return null;
			return Encoding.UTF8.GetString(buffer);
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
			try {
				if (mRegInfo != null) mLastString = String.Format("{0} ({1})", mRegInfo.Name, mRegInfo.Email);
				else mLastString = String.Format("{0}", GetAddress());
				return mLastString;
			} catch (Exception ex) { return mLastString; }
		}
	}
}
