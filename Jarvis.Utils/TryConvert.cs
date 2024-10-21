using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Utils
{
    public static class TryConvert
    {
        public static decimal? ToDecimal(this object obj)
        {
			try
			{
				return Convert.ToDecimal(obj);
			}
			catch (Exception)
			{
				return null;
			}
        }
        public static long? ToInt64(this object obj)
        {
            try
            {
                return Convert.ToInt64(obj);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static int? ToInt32(this object obj)
        {
            try
            {
                return Convert.ToInt32(obj);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
