using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text.Json;
using System.Transactions;

namespace Jarvis.Utils
{
    public static class Utils
    {
        #region Resources

        public static string? ReadResourceText(string resourceName)
        {
            Assembly? assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                string[]? resourceNames = assembly.GetManifestResourceNames();
                if (resourceNames != null)
                {
                    foreach (string? resourceN in resourceNames)
                    {
                        if (resourceN.Contains(resourceName))
                        {
                            using Stream? stream = assembly.GetManifestResourceStream(resourceN);
                            if (stream != null)
                            {
                                using StreamReader reader = new(stream);
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }

            return null;
        }

        [return: NotNull]
        public static T ThrowIfNull<T>(this T? obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException($"{typeof(T)} is null.");
            }
            return obj;
        }

        public static T? Clone<T>(this T toClone)
        {
            if (toClone is IQueryable)
            {
                throw new ArgumentException($"Passed object is a Linq {nameof(IQueryable)}, please complete query before cloning.");
            }
            string ser = JsonSerializer.Serialize(toClone);
            try
            {
                return JsonSerializer.Deserialize<T>(ser);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception.StackTrace);
                throw;
            }
        }

        public static bool TryClone<T>(this T? obj, out T? result)
        {
            try
            {
                if (obj != null)
                {
                    result = Clone(obj);
                }
                else
                {
                    result = default;
                }
                return true;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }

        public static bool TrySetValue(this object? obj, string key, object? value)
        {
            if (obj is null || value is null)
            {
                return false;
            }
            var prop = obj.GetType().GetProperty(key);
            return obj.TrySetValue(prop, value);
        }

        public static bool TrySetValue(this object? obj, PropertyInfo? prop, object? value)
        {
            if (obj is null || value is null || prop is null || prop.CanWrite == false)
			{
				return false;
			}
            try
            {
                if (prop.PropertyType == typeof(double))
                {
                    double newVal = Convert.ToDouble(value);
					prop?.SetValue(obj, newVal);
					return true;
				}
				prop?.SetValue(obj, value);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
		}

        public static double TakeDecimal(this double number, int decimalPlaces)
        {
            return (int)(number * Math.Pow(10, decimalPlaces)) / Math.Pow(10, decimalPlaces);
        }
        #endregion Resources

        public static bool TryRestartService(string serviceName)
        {
            if (OperatingSystem.IsWindows())
            {
                using ServiceController serviceController = new(serviceName);
                try
                {
                    if ((serviceController.Status.Equals(ServiceControllerStatus.Running)) || (serviceController.Status.Equals(ServiceControllerStatus.StartPending)))
                    {
                        serviceController.Stop();
                    }
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running);
                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

		public static Task<TResult> TryRunWithTimeout<TResult>(Func<Task<TResult>> taskFunc, TimeSpan timeout, object? args = null)
        {
            return taskFunc.RunWithTimeout(timeout,args);
        }
		public static async Task<TResult> RunWithTimeout<TResult>(this Func<Task<TResult>> taskFunc, TimeSpan timeout, object? args = null)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var task = taskFunc();
			var timeoutTask = Task.Delay(timeout, cancellationTokenSource.Token);

			if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
			{
				cancellationTokenSource.Cancel();
				throw new System.TimeoutException("The operation has timed out.");
			}

			return await task;
		}
	}
}