using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Core.Parallels
{
    public static class ParallelExecution
    {
        public static IReadOnlyCollection<Exception> MapReduce<T, T2>(
            IEnumerable<T> items,
            Func<T, T2> mapper,
            Action<T2> reducer,
            DegreeOfParallelism? degreeOfParallelism = default,
            CancellationToken cancellationToken = default)
        {
            var exceptions = MapReduceParallelExecution.MapReduce(items, mapper, reducer, degreeOfParallelism, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                exceptions.Add(new OperationCanceledException(cancellationToken));
            }
            return exceptions;
        }

        public static IReadOnlyCollection<Exception> ForEach<T>(
            IEnumerable<T> items,
            Action<T> handleItem,
            DegreeOfParallelism? degreeOfParallelism = default,
            CancellationToken cancellationToken = default)
        {
            var exceptions = ForEachParallelExecution.ForEach(items, handleItem, degreeOfParallelism, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                exceptions.Add(new OperationCanceledException(cancellationToken));
            }
            return exceptions;
        }

    }
}
