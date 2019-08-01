using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureView
{
    class FiFoBuffer<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> dict;
        private readonly Queue<TKey> queue;

        public TValue this[TKey key]
        {
            get => dict[key];
            set
            {
                dict[key] = value;
                MoveToBegin(key);
            }
        }

        public int Count => dict.Count;

        public int Size { get; }

        public FiFoBuffer(int size)
        {
            dict = new Dictionary<TKey, TValue>(size);
            queue = new Queue<TKey>(size);

            Size = size;
        }

        private void MoveToBegin(TKey key)
        {
            TKey[] array = queue.ToArray();

            queue.Clear();

            foreach (TKey tmp in array.Where(k => !Equals(k, key)))
            {
                queue.Enqueue(tmp);
            }

            queue.Enqueue(key);
        }

        public void Buffer(TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                this[key] = value;
                return;
            }

            if (Count == Size)
            {
                TKey removeKey = queue.Dequeue();

                if (dict[removeKey] is IDisposable disposable) disposable.Dispose();

                dict.Remove(removeKey);
            }

            queue.Enqueue(key);
            dict.Add(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dict.TryGetValue(key, out value);
        }
    }
}
