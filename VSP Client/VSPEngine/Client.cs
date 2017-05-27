using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VSP_Client.VSPEngine {
	/// <summary>
	/// Класс, органиующий работу с сервером.
	/// </summary>
	class Client {

		TcpClient mTCP;
		NetworkStream mStream;

		/// <summary>
		/// Пустой конструктор.
		/// </summary>
		public Client() { }

		/// <summary>
		/// Создаёт клиента (пира) и устанавливает подключение с сервером.
		/// </summary>
		/// <param name="hostname">Хост.</param>
		public Client(String hostname) : this(hostname, 1209) { }

		/// <summary>
		/// Создаёт клиента (пира) и устанавливает подключение с сервером (с кастомным портом).
		/// </summary>
		/// <param name="hostname">Хост.</param>
		/// <param name="port">Порт.</param>
		public Client(String hostname, int port) {
			Connect(hostname, port);
		}

		#region Connection methods
		/// <summary>
		/// Устанавливает подключение с сервером.
		/// </summary>
		/// <param name="hostname">Хост.</param>
		/// <returns></returns>
		public bool Connect(String hostname) {
			return Connect(hostname, 1209);
		}

		/// <summary>
		/// Устанавливает подключение с сервером (с кастомным портом).
		/// </summary>
		/// <param name="hostname">Хост.</param>
		/// <param name="port">Порт.</param>
		/// <returns></returns>
		public bool Connect(String hostname, int port) {
			try {
				mTCP = new TcpClient(hostname, port);
				mStream = mTCP.GetStream();
			} catch (Exception ex) { mTCP = null; mStream = null; return false; }
			return true;
		}

		/// <summary>
		/// Отключает клиента от сервера.
		/// </summary>
		public void Disconnect() {
			if (mTCP != null)
				if (mTCP.Connected)
					mStream.WriteByte(255);
			mTCP.Close();
			mTCP = null;
		}

		/// <summary>
		/// Возвращает состояние подключения.
		/// </summary>
		public bool IsConnected {
			get {
				if (mTCP != null)
					if (mTCP.Connected)
						return true;
				return false;
			}
		}
		#endregion

		#region Action methods
		/// <summary>
		/// Посылает запрос Say Hello! серверу и получает ответ.
		/// </summary>
		/// <returns>Ответ. (null, если ответ сервера или его поведение будет расценено как неожиданное)</returns>
		public String SayHello() {
			if (!IsConnected) return null;
			mStream.WriteByte(1);
			int length = mStream.ReadByte();
			if (length == -1) {
				mStream.Flush();
				return null;
			}
			byte[] buffer = new byte[length];
			if (mStream.Read(buffer, 0, length) != length) return null;
			mStream.Flush();
			return Encoding.UTF8.GetString(buffer);
		}
		/// <summary>
		/// Посылает запрос серверу на регистрацию нового пользователя.
		/// </summary>
		/// <param name="name">Имя.</param>
		/// <param name="password">Пароль.</param>
		/// <param name="email">E-mail.</param>
		/// <returns>Токен регистрации. (null, если ответ сервера или его поведение будет расценено как неожиданное)</returns>
		public long? Register(String name, String password, String email) {
			// тонна проверок на правильность аргументов
			if (name == null) throw new ArgumentException("\"name\" не может быть null.");
			byte[] name_raw = Encoding.UTF8.GetBytes(name);
			if (name.Length < 3) throw new ArgumentException("\"name\" не может быть короче трёх символов.");
			if (name_raw.Length > 255) throw new ArgumentException("\"name\" не может быть больше 255 байт.");

			if (password == null) throw new ArgumentException("\"password\" не может быть null.");
			byte[] password_raw = Encoding.UTF8.GetBytes(password);
			if (password.Length < 3) throw new ArgumentException("\"password\" не может быть короче трёх символов.");
			if (password.Length > 255) throw new ArgumentException("\"password\" не может быть больше 255 байт.");

			if (email == null) throw new ArgumentException("\"email\" не может быть null.");
			byte[] email_raw = Encoding.UTF8.GetBytes(email);
			if (email.Length < 3) throw new ArgumentException("\"email\" не может быть короче трёх символов.");
			if (email_raw.Length > 255) throw new ArgumentException("\"email\" не может быть больше 255 байт.");
			//if (SqlMethods.Like(email, "*@*.*")) throw new ArgumentException("\"email\" имеет некорректную форму.");

			// отправка инфы
			mStream.WriteByte(252);
			mStream.WriteByte((byte) name_raw.Length);
			mStream.Write(name_raw, 0, name_raw.Length);
			mStream.WriteByte((byte) password_raw.Length);
			mStream.Write(password_raw, 0, password_raw.Length);
			mStream.WriteByte((byte)email_raw.Length);
			mStream.Write(email_raw, 0, email_raw.Length);

			// получение инфы
			int status = mStream.ReadByte();
			if (status == -1 || status == 0) return null;
			byte[] token = new byte[8];
			for (int i = 0; i < 8; i++) {
				int val = mStream.ReadByte();
				if (val == -1) return null;
				token[i] = (byte) val;
			}
			return BitConverter.ToInt64(token, 0);
		}
		/// <summary>
		/// Посылает запрос серверу на подтверждение регистрации.
		/// </summary>
		/// <param name="token">Токен регистрации.</param>
		/// <param name="code">Код регистрации.</param>
		/// <returns>Статус регистрации.</returns>
		public bool Confirm(long token, int code) {
			// отправка данных
			mStream.WriteByte(253);
			mStream.Write(BitConverter.GetBytes(token), 0, 8);
			mStream.Write(BitConverter.GetBytes(code), 0, 4);

			// получение данных
			int status = mStream.ReadByte();
			return status == 1; 
		}
		#endregion
	}
}
