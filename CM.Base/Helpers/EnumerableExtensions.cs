using System.Linq;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Where(item => item != null);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
            where T : struct
        {
            return enumerable.Where(item => item != null).Select(item => item.Value);
        }
    }
}
