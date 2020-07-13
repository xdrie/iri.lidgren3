/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lidgren.Network
{
    /// <summary>
    /// Thread-safe (blocking) expanding queue with TryDequeue() and EnqueueFirst()
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class NetQueue<T> : IDisposable
    {
        // Example:
        // m_capacity = 8
        // m_size = 6
        // m_head = 4
        //
        // [0] item
        // [1] item (tail = ((head + size - 1) % capacity)
        // [2] 
        // [3] 
        // [4] item (head)
        // [5] item
        // [6] item 
        // [7] item

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private T[] _items;
        private int _head;

        /// <summary>
        /// Gets the number of items in the queue.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the current capacity of the queue.
        /// </summary>
        public int Capacity => _items.Length;

        internal string DebuggerDisplay => $"Count = {Count}, Capacity = {Capacity}";

        /// <summary>
        /// Constructs the queue with an initial capacity.
        /// </summary>
        public NetQueue(int initialCapacity)
        {
            _items = new T[initialCapacity];
        }

        /// <summary>
        /// Constructs the queue with a default capacity.
        /// </summary>
        public NetQueue()
        {
            _items = Array.Empty<T>();
        }

        private void AddCapacity()
        {
            SetCapacity(_items.Length + 64);
        }

        /// <summary>
        /// Adds an item last/tail of the queue
        /// </summary>
        public void Enqueue(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                if (Count == _items.Length)
                    AddCapacity();

                int slot = (_head + Count) % _items.Length;
                _items[slot] = item;
                Count++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds an item last/tail of the queue
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public void Enqueue(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _lock.EnterWriteLock();
            try
            {
                int expectedCount = Count;
                if (items is ICollection<T> coll)
                    expectedCount += coll.Count;
                else if (items is IReadOnlyCollection<T> roColl)
                    expectedCount += roColl.Count;

                if (expectedCount > Capacity)
                    SetCapacity(expectedCount + 64);

                foreach (var item in items.AsListEnumerator())
                {
                    // we leave this here even if we may have an expected count
                    // just to be safe
                    if (Count == _items.Length)
                        AddCapacity();

                    int slot = (_head + Count) % _items.Length;
                    _items[slot] = item;
                    Count++;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Places an item first, at the head of the queue
        /// </summary>
        public void EnqueueFirst(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                if (Count >= _items.Length)
                    AddCapacity();

                _head--;
                if (_head < 0)
                    _head = _items.Length - 1;
                _items[_head] = item;
                Count++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        // must be called from within a write locked m_lock!
        private void SetCapacity(int newCapacity)
        {
            if (_items.Length == 0)
            {
                _items = new T[newCapacity];
                _head = 0;
                return;
            }

            var newItems = new T[newCapacity];

            if (_head + Count - 1 < _items.Length)
            {
                Array.Copy(_items, _head, newItems, 0, Count);
            }
            else
            {
                Array.Copy(_items, _head, newItems, 0, _items.Length - _head);
                Array.Copy(_items, 0, newItems, _items.Length - _head, Count - (_items.Length - _head));
            }

            _items = newItems;
            _head = 0;
        }

        /// <summary>
        /// Gets an item from the head of the queue, or returns default if empty.
        /// </summary>
        public bool TryDequeue([MaybeNullWhen(false)] out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            _lock.EnterWriteLock();
            try
            {
                if (Count == 0)
                {
                    item = default;
                    return false;
                }

                item = _items[_head];

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    _items[_head] = default!;

                _head = (_head + 1) % _items.Length;
                Count--;

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Dequeues as many items as possible, appending them to a collection.
        /// </summary>
        /// <returns>The number of items dequeued.</returns>
        public int TryDrain(ICollection<T> destination, Action<T>? onItem = null)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (Count == 0)
                return 0;

            _lock.EnterWriteLock();
            try
            {
                int count = Count;
                while (Count > 0)
                {
                    var slice = _items.AsSpan(_head, Math.Min(Count, Count - _head));
                    foreach (var item in slice)
                    {
                        destination.Add(item);
                        onItem?.Invoke(item);
                    }
                    slice.Clear();

                    _head = 0;
                    Count -= slice.Length;
                }
                return count;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Tries to get a value without dequeuing it at a given offset.
        /// </summary>
        /// <param name="offset">The offset of the item.</param>
        /// <param name="value">The peek result.</param>
        /// <returns>Whether the peek returned a value.</returns>
        public bool TryPeek(int offset, [MaybeNullWhen(false)] out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }

            _lock.EnterReadLock();
            try
            {
                if (Count == 0)
                {
                    value = default;
                    return false;
                }

                value = _items[(_head + offset) % _items.Length];
                return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Tries to get a value without dequeuing, returning <see langword="default"/> if queue is empty.
        /// </summary>
        public T Peek(int offset)
        {
            if (TryPeek(offset, out var value))
                return value;
            return default!;
        }

        /// <summary>
        /// Determines whether an item is in the queue
        /// </summary>
        public bool Contains(T item, IEqualityComparer<T>? comparer)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            _lock.EnterReadLock();
            try
            {
                int left = Count;
                int offset = _head;
                while (left > 0)
                {
                    var slice = _items.AsSpan(offset, Math.Min(left, Count - _head));
                    foreach (var other in slice)
                    {
                        if (comparer.Equals(item, other))
                            return true;
                    }

                    left -= slice.Length;
                    offset = 0;
                }
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Determines whether an item is in the queue
        /// </summary>
        public bool Contains(T item)
        {
            return Contains(item, null);
        }

        /// <summary>
        /// Copies the queue items into a given span.
        /// </summary>
        public void CopyTo(Span<T> destination)
        {
            _lock.EnterReadLock();
            try
            {
                int left = Count;
                int offset = _head;
                while (left > 0)
                {
                    var slice = _items.AsSpan(offset, Math.Min(left, Count - _head));
                    slice.CopyTo(destination);

                    destination = destination.Slice(slice.Length);
                    left -= slice.Length;
                    offset = 0;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes all objects from the queue.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                Array.Clear(_items, 0, _items.Length);
                _head = 0;
                Count = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
