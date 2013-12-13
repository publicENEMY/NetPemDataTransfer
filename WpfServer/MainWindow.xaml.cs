using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using NLog;

namespace WpfServer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ObservableCollection<string> _messages = new ObservableCollection<string>();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private const string TargetName = "memoryex";

		public MainWindow()
		{
			InitializeComponent();

			var target =
				LogManager.Configuration.AllTargets
				.Where(x => x.Name == TargetName)
				.Single() as MemoryTargetEx;

			if (target != null)
				target.Messages.Subscribe(msg => Dispatcher.InvokeAsync(() =>
				{
					_messages.Add(msg);
				}));

			Logger.Info("Initialized");
		}

		public ObservableCollection<string> IncomingMessages
		{
			get { return _messages; }
			private set { _messages = value; }
		}

		private TcpPolicyServer myPolicyServer = new TcpPolicyServer();
		private IDuplexStringMessageReceiver CommandReceiver;

		//private IDuplexTypedMessageReceiver<byte[],byte[]> Worker4504Receiver;
		private IDuplexInputChannel Worker4504InputChannel;

		public void StartServer()
		{
			// Start the policy server to be able to communicate with silverlight.
			myPolicyServer.StartPolicyServer();

			// Create duplex message receiver.
			// It can receive messages and also send back response messages.
			IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
			CommandReceiver = aStringMessagesFactory.CreateDuplexStringMessageReceiver();
			CommandReceiver.ResponseReceiverConnected += ClientConnected;
			CommandReceiver.ResponseReceiverDisconnected += ClientDisconnected;
			CommandReceiver.RequestReceived += MessageReceived;

			// Create TCP based messaging.
			IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
			IDuplexInputChannel aDuplexInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:4502");

			// Attach the duplex input channel to the message receiver and start listening.
			// Note: Duplex input channel can receive messages but also send messages back.
			CommandReceiver.AttachDuplexInputChannel(aDuplexInputChannel);
			Logger.Info("Server started");

			StartWorker4504Server();
		}

		public void StartWorker4504Server()
		{
			// Create TCP based messaging.
			IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
			Worker4504InputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:4504");
			Worker4504InputChannel.MessageReceived += Worker4504InputChannel_MessageReceived;
			Worker4504InputChannel.ResponseReceiverConnected += ClientConnected;
			Worker4504InputChannel.ResponseReceiverDisconnected += ClientDisconnected;
			//Worker4504InputChannel.ResponseReceiverConnected += Worker4504InputChannel_ResponseReceiverConnected;
			//Worker4504InputChannel.ResponseReceiverDisconnected += Worker4504InputChannel_ResponseReceiverDisconnected;
			Worker4504InputChannel.StartListening();

			Logger.Info("Worker server 4504 started");
		}

		void Worker4504InputChannel_MessageReceived(object sender, DuplexChannelMessageEventArgs e)
		{
			// echo back
			//Logger.Info(BitConverter.ToString(e.Message as byte[]));
			//string s = BitConverter.ToString(e.Message as byte[]);
			//Logger.Info("Received : " + s);
			Logger.Info("Received data length : " + (e.Message as byte[]).Length);
			//Logger.Info(e.Message.ToString());
			Worker4504InputChannel.SendResponseMessage(e.ResponseReceiverId, e.Message);
		}

		public void StopServer()
		{
			// Close listenig.
			// Note: If the listening is not closed, then listening threads are not ended
			//       and the application would not be closed properly.
			CommandReceiver.DetachDuplexInputChannel();
			Worker4504InputChannel.StopListening();

			myPolicyServer.StopPolicyServer();
		}

		// The method is called when a message from the client is received.
		private void MessageReceived(object sender, StringRequestReceivedEventArgs e)
		{
			Logger.Info("Received : " + e.RequestMessage);

			// Analyze message
				// split strings
				// receiving responsereceiverid from client
			// Calculate mtu
			// Warm up stopwatch
			// Send message
			CommandReceiver.SendResponseMessage(e.ResponseReceiverId, e.RequestMessage);
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			StopServer();
		}

		// The method is called when a client is connected.
		// The Silverlight client is connected when the client attaches the output duplex channel.
		private void ClientConnected(object sender, ResponseReceiverEventArgs e)
		{
			// Add the connected client to the listbox.
			Dispatcher.InvokeAsync(() =>
			{
				ConnectedClientsListBox.Items.Add(e.ResponseReceiverId);
			});
			Logger.Info("ResponseReceiverId : " + e.ResponseReceiverId);
			Logger.Info("SenderAddress : " + e.SenderAddress);
		}

		// The method is called when a client is disconnected.
		// The Silverlight client is disconnected if the web page is closed.
		private void ClientDisconnected(object sender, ResponseReceiverEventArgs e)
		{
			// Remove the disconnected client from the listbox.
			Dispatcher.InvokeAsync(() =>
			{
				ConnectedClientsListBox.Items.Remove(e.ResponseReceiverId);
			});
			Logger.Info("ResponseReceiverId : " + e.ResponseReceiverId);
			Logger.Info("SenderAddress : " + e.SenderAddress);
		}

		private void BroadcastMessage(string s)
		{
			// Send the message to all connected clients.
			foreach (string aClientId in ConnectedClientsListBox.Items)
			{
				CommandReceiver.SendResponseMessage(aClientId, s);
			}
		}

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


		private void Server_Click(object sender, RoutedEventArgs e)
		{
			if (CommandReceiver == null)
			{
				StartServer();
				var s = sender as Button;
				s.Content = "Stop Server";
			}
			else if (CommandReceiver.IsDuplexInputChannelAttached)
			{
				StopServer();
				var s = sender as Button;
				s.Content = "Start Server";
			}
			else
			{
				StartServer();
				var s = sender as Button;
				s.Content = "Stop Server";
			}
		}
	}
}
