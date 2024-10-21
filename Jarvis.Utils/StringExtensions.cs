using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Jarvis.Utils
{
	public static class StringExtensions
	{
		private static readonly Regex sWhitespace = new Regex(@"\s+");
		public static string RemoveWhitespace(this string input)
		{
			return sWhitespace.Replace(input, "");
		}
		public static bool IsNotEqual(this string str , string otherStr)
		{
			return !str.Equals(otherStr);
		}
		public static string Format(this string str, params object[] args)
		{
			return string.Format(str, args);
		}

		public static bool IsNotNullOrEmpty(this string str)
		{
			return !string.IsNullOrEmpty(str);
		}

		public static bool IsNotNullOrWhiteSpace(this string str)
		{
			return !string.IsNullOrWhiteSpace(str);
		}

		public static bool IsNullOrEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

		public static bool IsNullOrWhiteSpace(this string str)
		{
			return string.IsNullOrWhiteSpace(str);
		}

		public static void NullCheck(this string str, string argumentName = "")
		{
			if (str.IsNullOrWhiteSpace())
			{
				throw new ArgumentNullException(argumentName);
			}
		}

		public static Expression<Func<T, object?>> ToExpression<T>(this string propName)
		{
			ParameterExpression arg = Expression.Parameter(typeof(T), "x");
			MemberExpression property = Expression.Property(arg, propName);
			UnaryExpression conv = Expression.Convert(property, typeof(object));
			Expression<Func<T, object?>> exp = Expression.Lambda<Func<T, object?>>(conv, new ParameterExpression[] { arg });
			return exp;
		}
	}
}