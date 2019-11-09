using System;
using System.Globalization;

namespace RunnersPal.Core.Extensions
{
    public static class StringExtensions
    {
        public static DateTime? ToDateTime(this object obj)
        {
            if (object.ReferenceEquals(null, obj)) return null;
            if (obj is DateTime) return (DateTime)obj;
            if (obj is DateTime?) return (DateTime?)obj;
            var strDate = obj.ToString();
            // assuming we're generally dealing with sqlite dates, try parsing in that format first,
            // then fallback to a more liberal TryParse
            return DateTime.TryParseExact(strDate, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out var date)
                ? (DateTime?)date
                : (DateTime.TryParse(strDate, out date) ? (DateTime?)date : null);
        }

        public static string MaxLength(this string str, int maxLength)
        {
            if (str == null) return str;
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength);
        }
    }
}