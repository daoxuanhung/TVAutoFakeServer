using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Net;


namespace TVAutoFakeServer
{
	class Program
	{
		static void Main(string[] args)
		{
			new AppRun();
		}
	}

	class AppRun
	{
		// do code này mình dịch ngược từ bản exe năm 2016 do mình không giữ
		// source nên code đã được Visual Studio optimize, nhiều đoạn sẽ hơi khác với cách con người code
		// ví dụ như cái biến writing này
		bool writing = false;

		public AppRun()
		{
			// bind len IP cua server TVAuto
			TcpListener listener = new TcpListener(IPAddress.Parse("103.92.26.100"), 61000);
			listener.Start();
			while(true)
			{
				TcpClient client = listener.AcceptTcpClient();
				new Thread(ServerServe).Start(client);
			}
		}

		private void ServerServe(object obj)
		{
			Thread.CurrentThread.IsBackground = true;
			TcpClient server = (TcpClient)obj;
			StreamWriter writer = new StreamWriter(server.GetStream());
			StreamReader reader = new StreamReader(server.GetStream());

			writer.AutoFlush = true;

			//server hello
			writer.Write(ToHex("1040"));
			writer.Write(0x0d);
			writer.Write(0x0a);
			Console.WriteLine("Server Send: " + "1040");

			// khởi tạo thread update thời gian xuống client
			new Thread(Update).Start(writer);

			while (true)
			{
				string message = ReadUntilEnd(server);
				
				message = FromHex(message);
				Console.WriteLine("Received: " + message);

				if (message.StartsWith("1002")) // code check vớ vẩn gì đó, nhưng đại loại là nó gửi 1002 thì mình gửi 1010
				{
					while (writing) { }
					writing = true;
					writer.Write(ToHex("1010") + "\r\n");
					writing = false;
					Console.WriteLine("Server Send: " + "1010");
				}
				else if (message.StartsWith("1011")) // hỏi các chức năng được active
				{
					while (writing) { }
					writing = true;
					writer.Write(ToHex("1020") + "83" + ToHex("100000") + "\r\n");
					// ID chức năng từ 1000 -> 1040
					// do không biết ID của từng chức năng thế nào nên gửi hết từ 1000 -> 1040
					for (int i = 1000; i < 1040; i++)
						if (i != 1010 && i != 1020)
							writer.Write(ToHex(i.ToString()) + "83" + ToHex("125/06/2099") + "\r\n");
					writing = false;
					Console.WriteLine("Server Send: " + "1020" + " 83 " + "99999999" + " 0D0A " + "1014" + " 83 " + "125/06/2099");
				}
			}
		}

		private string ReadUntilEnd(TcpClient client)
		{
			Socket socket = client.Client;

			byte[] buffer = new byte[150];
			int pointer = 0;
			while (true)
			{
				socket.Receive(buffer, pointer, 1, SocketFlags.None);

				// neu nhan duoc 0d 0a thi break;
				if (buffer.Length > 1 && (buffer[pointer] == 0x0a && buffer[pointer - 1] == 0x0d))
					break;

				pointer++;
			}

			return Encoding.ASCII.GetString(buffer, 0, pointer - 1); // bo di 2 ki tu cuoi cung
		}

		// update thời gian của server xuống cho client. Dưới footer của tvauto có cái chỗ chạy thời gian là nó
		private void Update(object obj)
		{
			Thread.CurrentThread.IsBackground = true;
			StreamWriter writer = (StreamWriter)obj;

			while (true)
			{
				int thu_trong_tuan = (int)DateTime.Today.DayOfWeek + 1;
				thu_trong_tuan = (thu_trong_tuan == 1 ? 8 : thu_trong_tuan);
				string time = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

				while (writing) { }
				writing = true;
				writer.Write(ToHex("1008") + "83" + ToHex("Th") + "F820" + ToHex((thu_trong_tuan).ToString()) + "20" + ToHex(time) + "\r\n");
				writing = false;

				Console.WriteLine("Server Send: " + "1008" + " 83 " + "Thứ " + thu_trong_tuan.ToString() + " " + time);
				
				Thread.Sleep(1000);			
			}
		}

		private string ToHex(string str)
		{
			var sb = new StringBuilder();

			var bytes = Encoding.ASCII.GetBytes(str);
			foreach (var t in bytes)
			{
				sb.Append(t.ToString("X2"));
			}

			return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
		}

		private string FromHex(string hexString)
		{
			var bytes = new byte[hexString.Length / 2];
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
			}

			return Encoding.ASCII.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
		}
	}
}
