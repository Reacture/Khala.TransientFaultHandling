namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class RetryPolicy<T>
    {
        private readonly int _maximumRetryCount;
        private readonly TransientFaultDetectionStrategy<T> _transientFaultDetectionStrategy;
        private readonly RetryIntervalStrategy _retryIntervalStrategy;

        public RetryPolicy(
            int maximumRetryCount,
            TransientFaultDetectionStrategy<T> transientFaultDetectionStrategy,
            RetryIntervalStrategy retryIntervalStrategy)
        {
            if (maximumRetryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRetryCount), "Value cannot be negative.");
            }

            _maximumRetryCount = maximumRetryCount;
            _transientFaultDetectionStrategy = transientFaultDetectionStrategy ?? throw new ArgumentNullException(nameof(transientFaultDetectionStrategy));
            _retryIntervalStrategy = retryIntervalStrategy ?? throw new ArgumentNullException(nameof(retryIntervalStrategy));
        }

        public Task<T> Run(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            async Task<T> Run()
            {
                int retryCount = 0;
                T result = default;
                Try:
                try
                {
                    result = await operation.Invoke(cancellationToken);
                }
                catch (Exception exception)
                when (_transientFaultDetectionStrategy.IsTransientException(exception) && retryCount < _maximumRetryCount)
                {
                    await Task.Delay(_retryIntervalStrategy.GetInterval(retryCount));
                    retryCount++;
                    goto Try;
                }

                if (_transientFaultDetectionStrategy.IsTransientResult(result) &&
                    retryCount < _maximumRetryCount)
                {
                    await Task.Delay(_retryIntervalStrategy.GetInterval(retryCount));
                    retryCount++;
                    goto Try;
                }

                return result;
            }

            return Run();
        }
    }
}
