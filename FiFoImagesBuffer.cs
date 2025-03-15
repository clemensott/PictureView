using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureView;

public class FiFoImagesBuffer
{
    private readonly Dictionary<string, FileSystemImage> dict;
    private readonly Queue<string> queue;

    public FileSystemImage this[string key]
    {
        get => dict[key];
        set
        {
            dict[key] = value;
            MoveToBegin(key);
        }
    }

    public int Count => dict.Count;

    public int MinCount { get; }

    public int MaxCount { get; }

    public int MaxBytes { get; }

    public FiFoImagesBuffer(int minCount, int maxCount, int maxBytes)
    {
        dict = new Dictionary<string, FileSystemImage>(maxCount);
        queue = new Queue<string>(maxCount);

        MinCount = minCount;
        MaxCount = maxCount;
        MaxBytes = maxBytes;
    }

    private void MoveToBegin(string key)
    {
        string[] array = queue.ToArray();

        queue.Clear();

        foreach (string tmp in array.Where(k => !Equals(k, key)))
        {
            queue.Enqueue(tmp);
        }

        queue.Enqueue(key);
    }

    public void Buffer(string key, FileSystemImage value)
    {
        if (dict.ContainsKey(key))
        {
            this[key] = value;
            return;
        }

        while (Count >= MaxCount || (Count > MinCount && dict.Values.Sum(v => v.DataSize ?? 0) > MaxBytes))
        {
            string removeKey = queue.Dequeue();

            if (dict[removeKey] is IDisposable disposable) disposable.Dispose();

            dict.Remove(removeKey);
        }

        queue.Enqueue(key);
        dict.Add(key, value);
    }

    public bool TryGetValue(string key, out FileSystemImage value)
    {
        return dict.TryGetValue(key, out value);
    }
}