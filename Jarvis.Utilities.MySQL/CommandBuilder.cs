using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto;
using System.Linq.Expressions;

namespace Jarvis.Utilities.MySQL
{
    internal static class MySQLCmdBuilder
    {
        /// <summary>
        /// Appends to the command part of an Sql query used to check the string is contains in
        /// the field. in the format " COLUMN1 LIKE '%word2%' "
        /// </summary>
        /// <param name="fieldName">The Column to check.</param>
        /// <param name="value">The filed to check againts</param>
        /// <param name="cmd">The command to which the query will be appended.</param>
        public static void Contains(string fieldName, string value, MySqlCommand cmd)
        {
            if (value.Contains(','))
            {
                value = value.Replace(",", string.Format("%' AND {0} LIKE '%", fieldName)) + "%'";
                cmd.CommandText += string.Format("{0} LIKE '%{1}", fieldName, value);
            }
            else
            {
                cmd.CommandText += string.Format("{0} LIKE '%{1}%'", fieldName, value);
            }
        }

        /// <summary>
        /// Returns part of an Sql query used to convert the current value to another datatype.
        /// <para>"TRY CONVERT ( <paramref name="type"/>, @ <paramref name="fieldName"/>)".</para>
        /// </summary>
        /// <param name="type">Resulting datatype.</param>
        /// <param name="fieldName">"Filed name.</param>
        /// <returns></returns>
        public static string Convert(string type, string fieldName)
        {
            return string.IsNullOrEmpty(type) ? "@" + fieldName : "TRY_CONVERT(" + type + ", @" + fieldName + ")";
        }

        /// <summary>
        /// Checks if <paramref name="fieldName"/> is greater than <paramref
        /// name="value"/><paramref name="cmd"/>.
        /// </summary>
        /// <param name="fieldName">The field to check.</param>
        /// <param name="value">The value to compare against.</param>
        /// <param name="cmd">The <see cref="MySqlCommand"/> to modify.</param>
        /// <param name="convertType">The type to convert to.</param>
        public static void GreaterThan(string fieldName, string value, MySqlCommand cmd, string convertType = "")
        {
            cmd.CommandText += "( " + fieldName + " > " + Convert(convertType, fieldName) + " )";
            _ = cmd.Parameters.AddWithValue(fieldName, value);
        }

        /// <summary>
        /// Adds a range check to <paramref name="cmd"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="lowerValue">The lower bouds of the range.</param>
        /// <param name="upperValue">The upper bounds of a range.</param>
        /// <param name="cmd">The <see cref="MySqlCommand"/> to modify.</param>
        /// <param name="convertType">The type to convert to.</param>
        public static void IsBetween(string fieldName, string lowerValue, string upperValue, MySqlCommand cmd, string convertType = "")
        {
            cmd.CommandText += "( " + fieldName + " BETWEEN " + Convert(convertType, "Min" + fieldName) + " AND " + Convert(convertType, "Max" + fieldName) + ")";
            _ = cmd.Parameters.AddWithValue("@Min" + fieldName, lowerValue);
            _ = cmd.Parameters.AddWithValue("@Max" + fieldName, upperValue);
        }

        /// <summary>
        /// Adds an equality check to <paramref name="cmd"/>.
        /// </summary>
        /// <param name="fieldName">The field to check.</param>
        /// <param name="value">The value to compare against.</param>
        /// <param name="cmd">The <see cref="MySqlCommand"/> to modify.</param>
        /// <param name="convertType">The data type to convert to.</param>
        public static void IsEqual(string fieldName, string value, MySqlCommand cmd, string convertType = "")
        {
            cmd.CommandText += "( " + fieldName + " = " + Convert(convertType, fieldName) + " )";
            _ = cmd.Parameters.AddWithValue(fieldName, value);
        }

        /// <summary>
        /// Adds an inequality check to <paramref name="cmd"/>.
        /// </summary>
        /// <param name="fieldName">The field to check.</param>
        /// <param name="value">The value to compare against.</param>
        /// <param name="cmd">The <see cref="MySqlCommand"/> to modify.</param>
        /// <param name="convertType">The type to convert to.</param>
        public static void IsNotEqual(string fieldName, string value, MySqlCommand cmd, string convertType = "")
        {
            cmd.CommandText += "( " + fieldName + " <> " + Convert(convertType, fieldName) + " )";
            _ = cmd.Parameters.AddWithValue(fieldName, value);
        }

        /// <summary>
        /// Checks if <paramref name="fieldName"/> is smaller than <paramref
        /// name="value"/><paramref name="cmd"/>.
        /// </summary>
        /// <param name="fieldName">The field to check.</param>
        /// <param name="value">The value to compare against.</param>
        /// <param name="cmd">The <see cref="MySqlCommand"/> to modify.</param>
        /// <param name="convertType">The type to convert to.</param>
        public static void LessThan(string fieldName, string value, MySqlCommand cmd, string convertType = "")
        {
            cmd.CommandText += "( " + fieldName + " < " + Convert(convertType, fieldName) + " )";
            _ = cmd.Parameters.AddWithValue(fieldName, value);
        }

        public static string GetSqlQuery<T>(this Expression<Func<T, bool>> expression)
        {
            return "WHERE " + VisitExpression(expression.Body);
        }

        private static string VisitExpression(Expression expr)
        {
            return expr switch
            {
                BinaryExpression be => VisitBinary(be),
                MemberExpression me => me.Member.Name,
                ConstantExpression ce => FormatConstant(ce.Value),
                UnaryExpression ue => VisitExpression(ue.Operand), // e.g., !x.IsActive
                _ => throw new NotSupportedException($"Unsupported expression: {expr.NodeType}")
            };
        }

        private static string VisitBinary(BinaryExpression be)
        {
            var left = VisitExpression(be.Left);
            var right = VisitExpression(be.Right);
            var op = GetSqlOperator(be.NodeType);
            return $"({left} {op} {right})";
        }

        private static string GetSqlOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Operator {type} not supported")
            };
        }

        private static string FormatConstant(object value)
        {
            return value switch
            {
                string s => $"'{s}'",
                bool b => b ? "1" : "0",
                null => "NULL",
                _ => value.ToString()
            };
        }

        public static string GetIdQuery(this IEnumerable<string> ids)
        {
            var formattedIds = ids.Select(id => $"'{id}'");
            var inClause = string.Join(", ", formattedIds);
            return inClause;
        }
    }
}