using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainApplication
{
    public class ContinousIterator<T> : IEnumerator<T>
    {
        private T[] _items;
        private int _currentIndex;
        private T _currentItem;

        public ContinousIterator(T[] items)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("Must have at least one item");

            _items = items;
            Reset();
        }

        public int Add(T item)
        {
            if (item == null)
                throw new ArgumentException("Item must not be null");

            int oldSize = _items.Count();

            T[] newItems = new T[oldSize + 1];
            Array.Copy(_items, newItems, oldSize);
            newItems[oldSize] = item;
            _items = newItems;

            return oldSize + 1;
        }

        public int Count()
        {
            return _items.Count();
        }

        private int GetCorrectedIndex(int index)
        {
            int count = _items.Count();

            while (index < 0)
                index += count;

            while (index >= count)
                index -= count;

            return index;
        }

        public T PeekAbsolute(int index)
        {
            return _items[GetCorrectedIndex(index)];
        }

        public T Current => _currentItem;

        object IEnumerator.Current => _currentItem;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public T MoveLast()
        {
            return MoveToIndex(-1);
        }

        public T MoveToIndex(int index)
        {
            _currentIndex = GetCorrectedIndex(index);
            return _currentItem = _items[_currentIndex];
        }

        public T PeekNext()
        {
            return PeekAbsolute(_currentIndex + 1);
        }

        public T PeekPrev()
        {
            return PeekAbsolute(_currentIndex - 1);
        }

        public T PeekRelative(int offset)
        {
            return PeekAbsolute(_currentIndex + offset);
        }

        public bool MoveNext()
        {
            MoveToIndex(_currentIndex + 1);
            return true;
        }

        public bool MovePrev()
        {
            MoveToIndex(_currentIndex - 1);
            return true;
        }

        public void Reset()
        {
            MoveToIndex(0);
        }
    }

}
