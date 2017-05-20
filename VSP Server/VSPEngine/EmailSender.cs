using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using VSP_Server.VSPEngine.PeerPart;

namespace VSP_Server.VSPEngine {
	class EmailSender {
		public static String RegCodeTemplate = "Добро пожаловать на сервер, использующий VSP!\n" +
			"Для продолжения регистрации введите код: {0}";

		public static void SendRegCode(RegistrationInfo ri) {
			SmtpClient smtpServer = new SmtpClient(EmailConstants.HOST, EmailConstants.PORT);
			smtpServer.EnableSsl = true;
			smtpServer.Credentials = new NetworkCredential(EmailConstants.LOGIN, EmailConstants.PASSWORD);
			MailMessage message = new MailMessage();
			message.To.Add(ri.Email);
			message.Body = String.Format(RegCodeTemplate, ri.RegCode);
			message.Subject = "Регистрация на сервере с VSP";
			smtpServer.SendMailAsync(message);
		}
	}
}
