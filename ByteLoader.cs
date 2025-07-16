using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureView;

class ByteLoader
{
    private (FileSystemImage image, SemaphoreSlim ss) currentTuple;
    private readonly Queue<(FileSystemImage image, SemaphoreSlim ss)> queue;

    public CancellationTokenSource CancelSource { get; }

    public ByteLoader()
    {
        queue = new Queue<(FileSystemImage image, SemaphoreSlim ss)>();
        CancelSource = new CancellationTokenSource();

        Task.Factory.StartNew(BytesLoad, CancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    private async Task BytesLoad()
    {
        while (!CancelSource.Token.IsCancellationRequested)
        {
            lock (queue)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queue);
                }

                currentTuple = queue.Dequeue();
            }

            await currentTuple.image.LoadBytes();
            currentTuple.ss.Release();
        }
    }

    public async Task<bool> Load(FileSystemImage? image)
    {
        if (image == null) return false;

        SemaphoreSlim ss = new SemaphoreSlim(0, 1);

        lock (queue)
        {
            if (Contains(image)) return false;

            queue.Enqueue((image, ss));

            Monitor.Pulse(queue);
        }

        await ss.WaitAsync();

        ss.Dispose();

        return true;
    }

    private bool Contains(FileSystemImage image)
    {
        if (image == currentTuple.image) return true;

        return queue.Any(t => t.image == image);
    }
}