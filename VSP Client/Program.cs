using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSP_Client.VSPEngine;

namespace VSP_Client {
	class Program {
		static Client mClient;

		static void Main(string[] args) {
			Console.WriteLine("Connecting to server... ");
			mClient = new Client();
			if (mClient.Connect("localhost")) Console.WriteLine("done!");
			else Console.WriteLine("done!");

			Console.Write("Sending SayHello request... ");
			String response = mClient.SayHello();
			if (response == null) Console.WriteLine("fail!");
			else Console.WriteLine("done! Response: \"{0}\"", response);

			Console.Write("Registering new user... ");
			long? token = mClient.Register("VladislavSavvateev", "vlad760497098", "savvateevvlad@mail.ru");
			if (token == null) Console.WriteLine("fail!");
			else {
				Console.WriteLine("done! Token: {0:X}", token);

				Console.Write("Enter RegCode: ");
				int code;
				while (true) {
					try {
						code = int.Parse(Console.ReadLine());
						break;
					} catch (Exception ex) { }
				}
				Console.Write("Confirming... ");
				if (mClient.Confirm((long)token, code)) Console.WriteLine("done!");
				else Console.WriteLine("fail!");
			}
			mClient.Disconnect();
			Console.ReadKey();
		}
	}
}
