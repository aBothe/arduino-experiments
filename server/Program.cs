//
// Program.cs
//
// Author:
//       Alexander Bothe <info@alexanderbothe.com>
//
// Copyright (c) 2013 Alexander Bothe
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO.Ports;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace arduinoComm
{
	class MainClass
	{
		public enum SensorType : byte
		{
			Temperature = 254,
			Brightness = 255
		}

		static Dictionary<int, byte[]> fileCache = new Dictionary<int, byte[]>();

		static float Temperature;
		static float Brightness;
		static SerialPort serialPort;

		public static void Main (string[] args)
		{
			const int port = 12000;
			serialPort = new SerialPort (args.Length == 0 ? "/dev/ttyACM0" : args [0], 9600);
			serialPort.DataBits = 8;
			serialPort.RtsEnable = true;
			serialPort.Handshake = Handshake.None;
			serialPort.StopBits = StopBits.One;

			try{
				serialPort.Open ();
			}catch(Exception ex) {
				Console.WriteLine (ex.Message);
				return;
			}

			// Start Listener
			new Thread(readTh).Start(serialPort);

			// Acquire sensors update
			serialPort.BaseStream.WriteByte ((byte)'#');

			var listener = new HttpListener();
			// Add the prefixes.
			listener.Prefixes.Add ("http://*:"+port.ToString()+"/");
			listener.Start();

			const string responseString = @"";

			Console.WriteLine("Listening on port "+port.ToString());

			while (true)
				ThreadPool.QueueUserWorkItem (BuildAnswer, listener.GetContext ());

			listener.Stop();
		}

		static void readTh(object s)
		{
			Thread.CurrentThread.IsBackground = true;
			var port = s as SerialPort;

			var data = new byte[3];
			int len;

			while(port.IsOpen)
			{
				try{
					len = port.Read(data, 0, data.Length);
				}catch(IOException io) {
					len = 0;
				}

				if(len < 3)
				{
					if(len == 0)
						Thread.Sleep(100);
					continue;
				}

				switch((SensorType) data[0])
				{
					case SensorType.Brightness:
						Brightness = ((float)BitConverter.ToInt16 (data, 1) / 1024f) * 100f;
						break;
					case SensorType.Temperature:
						Temperature = (float)data[1];
						if (data [2] == 1)
							Temperature = -Temperature;
						break;
				}
			}
		}

		static DateTime LastIndexWriteTime;
		static string indexContent;
		const string indexFile = "/index.html";

		static void BuildAnswer(object s)
		{
			var ctxt = s as HttpListenerContext;
			var request = ctxt.Request;
			var response = ctxt.Response;

			var hash = request.Url.LocalPath.GetHashCode ();
			var absPath = "." + request.Url.LocalPath;
			byte[] data = null;

			if (request.Url.LocalPath == "/" || request.Url.LocalPath == indexFile) {
				byte ans;
				byte.TryParse (request.QueryString ["m"], out ans);

				var ser = serialPort.BaseStream;
				if (ans > 0) {
					ser.WriteByte (ans);
				}

				var color = request.QueryString ["color"];

				if (color != null && color.Length == 6) {
					try{
						byte r,g,b;
						GetColor(color, out r, out g, out b);
						ser.WriteByte((byte)'$');
						ser.WriteByte(r);
						ser.WriteByte(g);
						ser.WriteByte(b);
					}catch(Exception ex){
					}
				}

				if (!string.IsNullOrEmpty (request.QueryString ["data"])) {
					data = Encoding.UTF8.GetBytes (string.Format (System.Globalization.CultureInfo.InvariantCulture.NumberFormat,"{{\"temp\":{0:0.#} , \"brightness\":{1:0.##}}}", Temperature, Brightness));
				} else {
					if (request.Url.LocalPath == "/")
						absPath = "." + indexFile;

					if (File.GetLastWriteTimeUtc (absPath) > LastIndexWriteTime || indexContent == null) {
						indexContent = File.ReadAllText (absPath);
						LastIndexWriteTime = File.GetLastWriteTimeUtc (absPath);
					}

					data = Encoding.UTF8.GetBytes (indexContent);
				}
			} else if (request.RawUrl.Contains ("/res/")) {
				if (File.Exists (absPath)) {
					if (!fileCache.TryGetValue (hash, out data))
						fileCache [hash] = data = File.ReadAllBytes (absPath);
				} else {
					response.StatusCode = 404;
					response.StatusDescription = absPath + " not found";
				}
			}

			if (data != null) {
				// Get a response stream and write the response to it.
				response.ContentLength64 = data.Length;
				response.OutputStream.Write (data, 0, data.Length);
				response.OutputStream.Close();
			}
			response.Close ();
		}


		public static void GetColor(string s, out byte r, out byte g, out byte b)
		{
			if (s.Length < 6) {
				r = g = b = 0;
				return;
			}
			r = (byte)((GetHexNumber (s [0]) * 16) + GetHexNumber (s [1]));
			g = (byte)((GetHexNumber (s [2]) * 16) + GetHexNumber (s [3]));
			b = (byte)((GetHexNumber (s [4]) * 16) + GetHexNumber (s [5]));
		}

		public static int GetHexNumber(char digit)
		{
			if (digit >= '0' && digit <= '9')
			{
				return digit - '0';
			}
			if ('A' <= digit && digit <= 'F')
			{
				return digit - 'A' + 0xA;
			}
			if ('a' <= digit && digit <= 'f')
			{
				return digit - 'a' + 0xA;
			}
			//errors.Error(line, col, String.Format("Invalid hex number '" + digit + "'"));
			return 0;
		}
	}
}
