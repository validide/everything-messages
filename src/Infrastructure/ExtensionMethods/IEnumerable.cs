using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingMessages.Infrastructure.ExtensionMethods;

public static class IEnumerable
{
    public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, CancellationToken, Task> funcBody, int maxDoP = 0, CancellationToken cancellationToken = default)
    {
        if(maxDoP <= 0)
        {
            maxDoP = Environment.ProcessorCount;
        }

        async Task AwaitPartition(IEnumerator<T> partition, CancellationToken ct)
        {
            using (partition)
            {
                while (!ct.IsCancellationRequested && partition.MoveNext())
                {
                    await Task.Yield(); // prevents a sync/hot thread hangup
                    await funcBody(partition.Current, ct).ConfigureAwait(false);
                }
            }
        }

        return Task.WhenAll(
            Partitioner
                .Create(source)
                .GetPartitions(maxDoP)
                .AsParallel()
                .Select(p => AwaitPartition(p, cancellationToken)));
    }
}
