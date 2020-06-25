using System.Collections.Generic;
using Lidgren.Network.Memory;

namespace Lidgren.Network
{
    internal static class ListEnumeratorExtensions
    {
        public static ListEnumerator<T> AsListEnumerator<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is IReadOnlyList<T> roList)
                return new ListEnumerator<T>(roList);
            else if (enumerable is IList<T> list)
                return new ListEnumerator<T>(list);
            return new ListEnumerator<T>(enumerable.GetEnumerator());
        }
    }
}
