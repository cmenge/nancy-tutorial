using System;

namespace CM.Base.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime FromUnixTimeMilliseconds(long unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(unixTimeStamp);
        }

        public static long ToUnixTime(this DateTime date)
        {
            return (date.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}
