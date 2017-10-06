namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRetryPolicy
    {
        Task Run(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken);

        Task Run<T>(
            Func<T, CancellationToken, Task> operation,
            T arg,
            CancellationToken cancellationToken);
    }
}
