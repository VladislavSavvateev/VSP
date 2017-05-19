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
			mClient.Disconnect();
			Console.ReadKey();
		}
	}
}
