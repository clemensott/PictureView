using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PictureView;

class AsyncEnumerator<T> : IDisposable
{
    private readonly IEnumerator<T> source;
    private readonly SemaphoreSlim moveNextSem, asyncTaskSem;

    public bool MovedNext { get; private set; }

    public T Current => source.Current;

    public AsyncEnumerator(IEnumerable<T> enumerable)
    {
        moveNextSem = new SemaphoreSlim(0);
        asyncTaskSem = new SemaphoreSlim(0);
        source = enumerable.GetEnumerator();

        Task.Run(MoveNextHandle);
    }

    private async Task MoveNextHandle()
    {
        do
        {
            await asyncTaskSem.WaitAsync();

            MovedNext = source.MoveNext();

            moveNextSem.Release();

        } while (MovedNext);
    }

    public void Dispose()
    {
        source.Dispose();
    }

    public async Task<bool> MoveNext()
    {
        asyncTaskSem.Release();
        await moveNextSem.WaitAsync();

        return MovedNext;
    }

    public void Reset()
    {
        source.Reset();
    }
}