using Jarvis.Common;
using Raven.Client.Documents;
using Raven.Client.Documents.Changes;
using Raven.Client.ServerWide.Operations;

namespace Jarvis.Database.Raven
{
	public class DbCollection<TItem> : DbCollectionBase<TItem> where TItem : IId
	{
		private readonly IDocumentStore _documentStore;
		public string? CollectionName => Info.CollectionName;
		protected override ICollectionCRUD<TItem>? CollectionCRUD { get; set; }

		#region Constructor

		public DbCollection(IDocumentStore client, IDbCollectionInfo settings) : base(settings)
		{
			_documentStore = client;
		}

		protected override void IniCollectionCRUD()
		{
			CollectionCRUD = new CollectionCRUD<TItem>(Info.CollectionName, _documentStore);
			CollectionCRUD.ErrorOccurred += (s, e) =>
			{
				InvokeErrorOccurred(e);
			};
		}

		protected override void Init()
		{
		}

		protected override void PostInit()
		{
		}

		#endregion Constructor

		public override Task<bool> Close()
		{
			return Task.FromResult(false);
		}

		protected override Task<bool> CheckConnection()
		{
			try
			{
				Task[] tasks = new Task[1] { _documentStore.Maintenance.Server.SendAsync(new GetBuildNumberOperation()) };
				bool success = Task.WaitAll(tasks, 10000);
				return Task.FromResult(success);
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		protected override void Disconnect()
		{
		}

		protected override void Listen()
		{
			_documentStore
				.Changes()
			.ForDocumentsInCollection<TItem>()
				.Subscribe(new SubscriptionObserver<TItem>(value =>
				{
					if (value.Type == DocumentChangeTypes.Delete)
					{
						InvokeCollectionChanged(new(CollectionChange.ElementRemoved));
					}
					else if (value.Type == DocumentChangeTypes.Put)
					{
						InvokeCollectionChanged(new(CollectionChange.ElementAdded));
					}
					else
					{
						InvokeCollectionChanged(new(CollectionChange.ElementUpdated));
					}
				}));
		}

		protected override void DisposeImpl()
		{
			_documentStore.Dispose();
		}

		#region Classes

		private class SubscriptionObserver<TValue> : IObserver<DocumentChange>
		{
			private readonly Action<DocumentChange> _action;

			public SubscriptionObserver(Action<DocumentChange> action)
			{
				_action = action;
			}

			public void OnCompleted()
			{
			}

			public void OnError(Exception error)
			{
			}

			public void OnNext(DocumentChange value)
			{
				_action(value);
			}
		}

		#endregion Classes
	}
}