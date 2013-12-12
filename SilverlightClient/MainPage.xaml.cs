using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace SilverlightClient
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();
		}

#region Old
		// The method is called when a message from the desktop application is received.
		private void ResponseReceived(object sender, TypedResponseReceivedEventArgs<byte[]> e)
		{
			textBox2.Text = GetString(e.ResponseMessage);
		}

		// The method is called when the button to send message is clicked.
		private void SendMessage_Click(object sender, RoutedEventArgs e)
		{
			// Create message sender sending request messages of type Person and receiving responses of type string.
			IDuplexTypedMessagesFactory aTypedMessagesFactory = new DuplexTypedMessagesFactory();
			myMessageSender = aTypedMessagesFactory.CreateDuplexTypedMessageSender<byte[], byte[]>();
			myMessageSender.ResponseReceived += ResponseReceived;

			// Create messaging based on TCP.
			IMessagingSystemFactory aMessagingSystemFactory = new TcpMessagingSystemFactory();
			IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("tcp://127.0.0.1:4502/");

			// Attach output channel and be able to send messages and receive response messages.
			myMessageSender.AttachDuplexOutputChannel(aDuplexOutputChannel);

			myMessageSender.SendRequestMessage(GetBytes(textBox1.Text));
		}
		
		private IDuplexTypedMessageSender<byte[], byte[]> myMessageSender;
#endregion

#region Common Utilities
		static byte[] GetBytes(string str)
		{
			byte[] bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}

		static string GetString(byte[] bytes)
		{
			char[] chars = new char[bytes.Length / sizeof(char)];
			System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}

		private void Log(string s)
		{
			// check if we not in main thread
			if (!Dispatcher.CheckAccess())
			{
				// call same method in main thread
				Dispatcher.BeginInvoke(() =>
				{
					Log(s);
				});
				return;
			}
			// in main thread now
			LogListBox.Items.Add(s);
		}

#endregion
		private void Download1MB(object sender, RoutedEventArgs e)
		{
			if (InitializeCommandConnection())
			{
				CommandMessageSender.SendMessage("Give|Me|Data|1048576");	
			}
		}

		private IDuplexTypedMessageSender<byte[], byte[]>[] WorkerMessageSender = new IDuplexTypedMessageSender<byte[], byte[]>[256];
		//private IDuplexTypedMessageSender<byte[], byte[]> WorkerMessageSender;
		private IDuplexStringMessageSender CommandMessageSender;

		private bool InitializeCommandConnection()
		{
			// Create duplex message sender.
			// It can send messages and also receive messages.
			IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
			CommandMessageSender = aStringMessagesFactory.CreateDuplexStringMessageSender();
			CommandMessageSender.ResponseReceived += CommandResponseReceived;

			// Create TCP based messaging.
			IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
			IDuplexOutputChannel aDuplexOutputChannel = aMessaging.CreateDuplexOutputChannel("127.0.0.1:4502");

			// Attach the duplex output channel to the message sender
			// and be able to send messages and receive messages.
			try
			{
				CommandMessageSender.AttachDuplexOutputChannel(aDuplexOutputChannel);
			}
			catch (Exception e)
			{
				Log(e.Message);
			}

			if (CommandMessageSender.IsDuplexOutputChannelAttached)
			{
				Log("Initialized connection.");
				return true;
			}
			else
			{
				Log("Unable to initialize connection");
				return false;
			}
		}

		private void CommandResponseReceived(object sender, StringResponseReceivedEventArgs e)
		{
			Log("Received : " + e.ResponseMessage);

			// Analyze command received(received list of server to connect to) ip:port
			// if download
			// connect to worker 
			// ready for download
			// if upload
			// use this for upload
			// Connect to supplied list of server
			
			/*
			var ip = new IPEndPoint[256];
			ip[0].Address = IPAddress.Loopback;
			ip[0].Port = 4504;
			byte[] data = new byte[1048576]; // initialize 1MB data
			Random random = new Random();
			random.NextBytes(data);

			if (InitializeWorkerConnection(ip[0]))
			{
				WorkerMessageSender[0].SendRequestMessage(data);
			}
			*/

			// test disconnect
			//if(CommandMessageSender.IsDuplexOutputChannelAttached)
			//	CommandMessageSender.DetachDuplexOutputChannel();
		}

		private bool InitializeWorkerConnection(IPEndPoint iPEndPoint)
		{
			throw new NotImplementedException();
		}
	}
}
