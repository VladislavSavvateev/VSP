using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSP_Server.VSPEngine.PeerPart {
	/// <summary>
	/// Класс, реализующий хранение информации о регистрации пользователей.
	/// </summary>
	class RegistrationInfo {
		String mName;
		String mPassword;
		String mEmail;
		long mToken;
		RegistrationStatus mRegStatus;
		int mRegCode;

		/// <summary>
		/// Определяет статус регистрации.
		/// </summary>
		public enum RegistrationStatus {
			/// <summary>
			/// На подтверждении.
			/// </summary>
			ON_CONFIRM = 1,
			/// <summary>
			/// Подтвержден.
			/// </summary>
			CONFIRMED = 0,
			/// <summary>
			/// Забанен.
			/// </summary>
			BANNED = 2
		}
		
		/// <summary>
		/// Стандартный конструктор.
		/// </summary>
		/// <param name="name">Имя.</param>
		/// <param name="password">Пароль.</param>
		/// <param name="email">E-mail.</param>
		/// <param name="status">Статус регистрации.</param>
		public RegistrationInfo(String name, String password, String email, RegistrationStatus status) {
			mName = name;
			mPassword = password;
			mEmail = email;
			mRegStatus = status;
			mToken = GenerateRandomToken();
			if (status == RegistrationStatus.ON_CONFIRM) mRegCode = GenerateRandomRegCode();
		}

		private long GenerateRandomToken() {
			String hex = "";
			Random r = new Random();
			for (int i = 0; i < 16; i++) 
				hex += r.Next(16).ToString("X");
			return Convert.ToInt64(hex, 16);
		}
		private int GenerateRandomRegCode() {
			Random r = new Random();
			return r.Next(100000, 999999);
		}

		/// <summary>
		/// Возвращает имя пользователя.
		/// </summary>
		public String Name {
			get { return mName; }
		}
		/// <summary>
		/// Возвращает пароль пользователя
		/// </summary>
		public String Password {
			get { return mPassword; }
		}
		/// <summary>
		/// Возвращает E-mail пользователя.
		/// </summary>
		public String Email {
			get { return mEmail; }
		}
		/// <summary>
		/// Возвращает токен регистрации/логина пользователя.
		/// </summary>
		public long Token {
			get { return mToken; }
		}
		/// <summary>
		/// Возвращает или задаёт статус регистрации пользователя.
		/// </summary>
		public RegistrationStatus RegStatus {
			get { return mRegStatus; }
			set { mRegStatus = RegStatus; }
		}
		/// <summary>
		/// Возвращает регистрационный код.
		/// </summary>
		public int RegCode {
			get { return mRegCode; }
		}

		/// <summary>
		/// Возвращает общую информацию об информации о регистрации.
		/// </summary>
		/// <returns>Информация о регистрации (кроме конфедициальной информации).</returns>
		public override string ToString() {
			return String.Format("{0} ({1})", mName, mEmail);
		}
		/// <summary>
		/// Возвращает значение, которое показывает, равна ли заданная информация о регистрации той, которая дана.
		/// </summary>
		/// <param name="obj">Информация о регистрации, с которой необходимо произвести сравнение.</param>
		/// <returns>Результат сравнения.</returns>
		public override bool Equals(object obj) {
			RegistrationInfo ri = (RegistrationInfo) obj;
			return mName.Equals(ri.Name);
		}
		public override int GetHashCode() {
			return (mName + mEmail).GetHashCode();
		}
	}
}
