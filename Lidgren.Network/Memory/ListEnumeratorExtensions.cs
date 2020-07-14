using System.Collections.Generic;

namespace Lidgren.Network
{
    internal static class ListEnumeratorExtensions
    {
        /// <summary>
        /// Creates a struct enumerator over the enumerable, trying to iterate over it using list indexing
        /// if the enumerable implements <see cref="IReadOnlyList{T}"/> or <see cref="IList{T}"/>,
        /// falling back to <see cref="IEnumerable{T}.GetEnumerator"/>.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection.</typeparam>
        /// <param name="enumerable">The collection to be enumerated.</param>
        /// <returns>The struct enumerator.</returns>
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
