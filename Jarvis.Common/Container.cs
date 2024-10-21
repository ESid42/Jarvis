using System.Collections.Concurrent;

namespace Jarvis.Common
{
	public interface IContainer
	{
		void AddScoped<T, TV>() where TV : T;

		void AddSingleton<T>(object obj);

		void AddSingleton<T, TV>() where TV : T;

		void AddSingleton(Type interfaceType, object obj);

		void AddTransient<T, TV>() where TV : T;

		void Clear();

		bool Exist<T>();

		T Get<T>(params object?[] args);

		T? GetExist<T>();
	}

	public interface IScoped : IDisposable
	{
		bool IsDisposed { get; }
	}

	public class Container : IContainer
	{
		#region Definitions

		private readonly ConcurrentDictionary<Type, MapObject> _mapping = new();

		#endregion Definitions

		#region Constructor

		public Container()
		{ }

		#endregion Constructor

		#region Methods

		public void AddScoped<T, TV>() where TV : T
		{
			TryAdd<T, TV>(MapType.Scoped);
		}

		public void AddSingleton<T>(object obj)
		{
			AddSingleton(typeof(T), obj);
		}

		public void AddSingleton(Type interfaceType, object obj)
		{
			TryAdd(interfaceType, MapType.Singleton, null, obj);
		}

		public void AddSingleton<T, TV>() where TV : T
		{
			TryAdd<T, TV>(MapType.Singleton);
		}

		public void AddTransient<T, TV>() where TV : T
		{
			TryAdd<T, TV>(MapType.Transient);
		}

		public void Clear()
		{
			_mapping.Clear();
		}

		public bool Exist<T>()
		{
			return _mapping.ContainsKey(typeof(T));
		}

		[System.Diagnostics.DebuggerStepThrough]
		public T Get<T>(params object?[] args)
		{
			return Get(typeof(T), args) is T obj ? obj : throw new ArgumentException($"Couldn't retrieve object of type {typeof(T).Name}.");
		}

		public T? GetExist<T>()
		{
			return Get(typeof(T)) is T obj ? obj : default;
		}

		private static ArgumentException ExceptionExist(Type type)
		{
			return new($"Interface of type \"{type.Name}\" already exists.");
		}

		private static ArgumentException ExceptionNotAdded(Type type, Type typeValue)
		{
			return new($"Error while adding interface type \"{type.Name}\" with implementation type \"{typeValue.Name}\".");
		}

		private object? Get(Type type, params object?[] args)
		{
			MapType mapType = MapType.Singleton;
			MapObject? objM = GetMap(type);

			if (objM != null) { mapType = objM.MapType; }

			if (mapType == MapType.Singleton)
			{
				if (objM?.Object != null)
				{
					return objM.Object;
				}
			}
			else if (mapType == MapType.Scoped)
			{
				if (objM?.Object is IScoped disposable)
				{
					if (!disposable.IsDisposed)
					{
						return objM.Object;
					}
				}
			}

			Type? target = type;
			if (type.IsInterface) { target = ResolveType(type); }
			if (target == null) { return null; }

			List<System.Reflection.ConstructorInfo>? constructors = target.GetConstructors().ToList();
			if (constructors.Count == 0) { throw new InvalidOperationException($"No constructors were found on type {type.Name}."); }

			System.Reflection.ConstructorInfo? constructor = constructors.First();
			System.Reflection.ParameterInfo[]? parameters = constructor.GetParameters();
			List<object?>? resolvedParameters = new();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (args != null && i < args.Length)
				{
					object? arg = args[i];
					if (arg != null && arg.GetType() == parameters[i].ParameterType)
					{
						resolvedParameters.Add(arg);
						continue;
					}
				}
				object? objP = Get(parameters[i].ParameterType);
				//if(objP != null)
				//{
				//    resolvedParameters.Add(objP);
				//}
				resolvedParameters.Add(objP);
			}
			//if(!resolvedParameters.Any())
			//{
			//    constructor = constructors.Where(x => !x.GetParameters().Any()).FirstOrDefault();
			//    if (constructor == null)
			//    {
			//        throw new InvalidOperationException("No parameterless constructor found.");
			//    }
			//}
			object? obj = constructor.Invoke(resolvedParameters.ToArray());
			if (mapType != MapType.Transient)
			{
				if (type.IsInterface)
				{
					_mapping[type].Object = obj;
				}
				else
				{
					Type[]? interfaces = target.GetInterfaces();
					foreach (Type? inter in interfaces)
					{
						if (_mapping.TryGetValue(inter, out MapObject? value))
						{
							value.Object = obj;
							break;
						}
					}
				}
			}
			return obj;
		}

		private MapObject? GetMap(Type type)
		{
			return _mapping.TryGetValue(type, out MapObject? value) ? value : default;
		}

		private Type? ResolveType(Type type)
		{
			return _mapping.TryGetValue(type, out MapObject? value) ? value.Type : null;
		}

		private void TryAdd<T, TV>(MapType mapType)
		{
			TryAdd<T>(mapType, typeof(TV));
		}

		private void TryAdd<T>(MapType mapType, Type? typeObj = null, object? obj = null)
		{
			TryAdd(typeof(T), mapType, typeObj, obj);
		}

		private void TryAdd(Type interfaceType, MapType mapType, Type? typeObj = null, object? obj = null)
		{
			if (typeObj == null) { typeObj = obj != null ? obj.GetType() : throw new ArgumentException("Type object is null."); }
			if (_mapping.ContainsKey(interfaceType)) { throw ExceptionExist(interfaceType); }
			if (!_mapping.TryAdd(interfaceType, new MapObject(mapType, typeObj) { Object = obj })) { throw ExceptionNotAdded(interfaceType, typeObj); }
		}

		#endregion Methods

		#region Classes

		private enum MapType
		{
			Singleton,
			Scoped,
			Transient
		}

		private record MapObject(MapType MapType, Type Type)
		{
			public object? Object { get; set; }
		}

		#endregion Classes
	}
}