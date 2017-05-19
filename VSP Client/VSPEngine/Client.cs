using System;
using System.Collections.Generic;
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
		public Client() {}

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
		/// Вернёт null, если ответ сервера или его поведение будет расценено как неожиданное.
		/// </summary>
		/// <returns>Ответ.</returns>
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
		#endregion
	}
}
