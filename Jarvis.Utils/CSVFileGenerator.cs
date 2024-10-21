using System.Data;
using System.Reflection;

namespace Jarvis.Utils
{
	/// <summary>
	/// </summary>
	public class CSVFileGenerator
	{
		#region Constructor

		/// <summary>
		/// </summary>
		public CSVFileGenerator()
		{
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		/// Creates a CSV File
		/// </summary>
		/// <param name="dt">Data Table containing Rows and Columns Data</param>
		/// <param name="strFilePath">File Path to Save CSV at</param>
		/// <exception cref="IOException">
		/// Thrown when the process cannot access the file because it is being used by another process.
		/// </exception>
		public static void CreateCSVFile(DataTable dt, string strFilePath)
		{
			StreamWriter sw = new(strFilePath, false);

			int iColCount = dt.Columns.Count;
			for (int i = 0; i < iColCount; i++)
			{
				string? str = dt.Columns[i].ToString();
				string cleanedStr;
				if (str != null)
				{
					//Remove New Lines, and Commas, since they mess up with csv table
					cleanedStr = str.Replace("\n", " ").Replace(",", " ");
				}
				else
				{
					cleanedStr = " ";
				}
				sw.Write(cleanedStr);
				if (i < iColCount - 1)
				{
					sw.Write(",");
				}
			}
			sw.Write(sw.NewLine);

			foreach (DataRow? dataRow in dt.Rows)
			{
				if (dataRow != null)
				{
					for (int i = 0; i < iColCount; i++)
					{
						if (!Convert.IsDBNull(dataRow[i]))
						{
							string? str = dataRow[i].ToString();
							string cleanedStr = "";
							if (str != null)
							{
								//Remove New Lines, and Commas, since they mess up with csv table
								cleanedStr = str.Replace("\n", " ").Replace(",", " ");
							}
							else
							{
								cleanedStr = " ";
							}
							sw.Write(cleanedStr);
						}
						if (i < iColCount - 1)
						{
							sw.Write(",");
						}
					}
					sw.Write(sw.NewLine);
				}
				else
				{
					throw new ArgumentException("Datarow is null.");
				}
			}
			sw.Close();
		}

		/// <summary>
		/// Generates CSV format text from <see cref="IEnumerable{T}"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objects"></param>
		/// <returns></returns>
		public static string FormatAsCsv<T>(IEnumerable<T> objects)
		{
			string csvFormat = string.Empty;
			Type type = typeof(T);
			IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
			bool isFirst = true;
			foreach (PropertyInfo prop in props)
			{
				if (isFirst)
				{
					csvFormat = prop.Name;
					isFirst = false;
				}
				else
				{
					csvFormat = $"{csvFormat},{prop.Name}";
				}
			}
			foreach (T obj in objects)
			{
				csvFormat = $"{csvFormat}\n";
				isFirst = true;
				foreach (PropertyInfo prop in props)
				{
					object propValue = prop.GetValue(obj, null);
					if (isFirst)
					{
						csvFormat = $"{csvFormat}{propValue}";
						isFirst = false;
					}
					else
					{
						csvFormat = $"{csvFormat},{propValue}";
					}
				}
			}
			return csvFormat;
		}

		/// <summary>
		/// Get list of items of type of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <param name="res"></param>
		/// <returns></returns>
		public static bool TryParseCsv<T>(string path, out IEnumerable<T> res)
		{
			IEnumerable<string> lines = File.ReadLines(path);
			return TryParseCsv(lines, out res);
		}

		/// <summary>
		/// Get list of items of type of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="lines"></param>
		/// <param name="res"></param>
		/// <returns></returns>
		public static bool TryParseCsv<T>(IEnumerable<string> lines, out IEnumerable<T> res)
		{
			res = new List<T>();
			if (lines.Any())
			{
				IEnumerable<string> propertyNames = lines.ElementAt(0).Split(',');
				foreach (string line in lines.Skip(1))
				{
					IEnumerable<string> values = line.Split(",");
					if (values.Any())
					{
						try
						{
							T obj = Activator.CreateInstance<T>();
							for (int i = 0; i < propertyNames.Count(); i++)
							{
								obj.TrySetValue(propertyNames.ElementAt(i), values.ElementAt(i));
							}
							res = res.Append(obj);
						}
						catch (MissingMethodException)
						{
							return false;
						}
					}
				}
			}
			return res.Any();
		}

		#endregion Methods
	}
}