using System;
using System.Collections;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink
{
    /*
     * Adds Paging to an IEnumerable.
     * Recommend NOT using ForEach, as it will only iterate the first page.
     * Behaviour is undefined with pageSize less than 1.
     */
    public class PagedEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _wrappedEnumerable;
        private readonly int _pageSize;
        private readonly Func<T, int> _itemToSize;

        public PagedEnumerable(IEnumerable<T> toWrap, int pageSize, Func<T, int> itemToSize)
        {
            _wrappedEnumerable = toWrap;
            _pageSize = pageSize;
            _itemToSize = itemToSize;
        }

        public PagedEnumerator<T> GetPagedEnumerator()
        {
            return new PagedEnumerator<T>(_wrappedEnumerable.GetEnumerator(), _pageSize, _itemToSize);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetPagedEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    public class PagedEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T> _wrappedEnumerator;
        private int _pageSize;
        private Func<T, int> _itemToSize;

        private int _count = 0;
        private T _previous;

        public PagedEnumerator(IEnumerator<T> toWrap, int pageSize, Func<T, int> itemToSize)
        {
            _wrappedEnumerator = toWrap;
            _pageSize = pageSize;
            _itemToSize = itemToSize;
        }

        public void Dispose()
        {
            _wrappedEnumerator.Dispose();
        }

        //Returns true while there's an item available that fits in the current page.
        //Returns false when the next item wouldn't fit in the current page.
        //Starts a new page the next time it's called after "false".
        public bool MoveNext()
        {
            //Don't advance if we've exceeded the page size on the last move - treat the element as the start of a
            //new page.
            if (_count <= _pageSize || _pageSize <= 0)
            {
                if (!_wrappedEnumerator.MoveNext())
                {
                    _hasMorePages = false;
                    return false;
                }
            }
            else { _count = 0; }

            _count += _itemToSize(_wrappedEnumerator.Current);

            if (_count > _pageSize)
            {
                _previous = _wrappedEnumerator.Current;
                _hasMorePages = true;
                return false;
            }

            return true;
        }

        public void Reset()
        {
            _wrappedEnumerator.Reset();
            _count = 0;
            _previous = default(T);
        }

        public T Current => _count > _pageSize ? _previous : _wrappedEnumerator.Current;

        object IEnumerator.Current => Current;

        private bool _hasMorePages = true;
        public bool HasMorePages => _hasMorePages;

        public void ForEachInPage(Action<T> action)
        {
            while (MoveNext()) { action(Current); }
        }
    }
}