using Jarvis.Utils;

namespace Jarvis.Common
{
	#region Events

	public class CollectionChangedEventArgs<T> : EventArgs<T>
	{
		public CollectionChangedEventArgs(CollectionChange type, T value, int valueIndex = -1) : base(value)
		{
			ChangeType = type;
			ValueIndex = valueIndex;
		}

		public CollectionChange ChangeType { get; }

		public int ValueIndex { get; }
	}

	public enum CollectionChange
	{
		ElementAdded,

		ElementUpdated,

		ElementRemoved,
	}

	public class CollectionChangedEventArgs : EventArgs
	{
		public CollectionChangedEventArgs(CollectionChange type, int valueIndex = -1, object? item = null)
		{
			ChangeType = type;
			ValueIndex = valueIndex;
			Item = item;
		}

		public CollectionChange ChangeType { get; }

		public object? Item { get; }

		public int ValueIndex { get; }
	}

	#endregion Events

	public enum ConnectionStatusType
	{
		Connected,

		Disconnected,

		Closed,

		Connecting,
		Closing,

		WaitingForServer,

		WaitingForClient,
	}

	public class BytesReceivedEventArgs : EventArgs
	{
		#region Definitions

		public byte[] Msg { get; set; }

		#endregion Definitions

		#region Constructor

		public BytesReceivedEventArgs(byte[] msg)
		{
			Msg = msg;
		}

		#endregion Constructor
	}

	public class ConnectionChangedEventArgs : EventArgs
	{
		#region Definitions

		public string Id { get; set; }

		public ConnectionStatusType Status { get; private set; }

		#endregion Definitions

		#region Constructor

		public ConnectionChangedEventArgs(ConnectionStatusType status, string id = "")
		{
			Status = status;
			Id = id;
		}

		#endregion Constructor
	}

	public class DataReceivedEventArgs : EventArgs
	{
		#region Definitions

		public string Msg { get; set; }

		#endregion Definitions

		#region Constructor

		public DataReceivedEventArgs(string msg = "")
		{
			Msg = msg;
		}

		#endregion Constructor
	}

	public class ErrorOccuredEventArgs : EventArgs
	{
		public Exception? Exception { get; set; }

		public string Message { get; set; } = "";

		public ErrorOccuredEventArgs(Exception exception)
		{
			Exception = exception;
		}

		public ErrorOccuredEventArgs(string message)
		{
			Message = message;
		}
	}
}