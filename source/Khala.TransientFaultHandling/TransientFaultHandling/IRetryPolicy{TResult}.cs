namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRetryPolicy<TResult> : IRetryPolicy
    {
        Task<TResult> Run(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken);

        Task<TResult> Run<T>(
            Func<T, CancellationToken, Task<TResult>> operation,
            T arg,
            CancellationToken cancellationToken);
    }
}
