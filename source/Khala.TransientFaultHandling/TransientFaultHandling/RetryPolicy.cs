namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class RetryPolicy : IRetryPolicy
    {
        public RetryPolicy(
            int maximumRetryCount,
            TransientFaultDetectionStrategy transientFaultDetectionStrategy,
            RetryIntervalStrategy retryIntervalStrategy)
        {
            if (maximumRetryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRetryCount), "Value cannot be negative.");
            }

            MaximumRetryCount = maximumRetryCount;
            TransientFaultDetectionStrategy = transientFaultDetectionStrategy ?? throw new ArgumentNullException(nameof(transientFaultDetectionStrategy));
            RetryIntervalStrategy = retryIntervalStrategy ?? throw new ArgumentNullException(nameof(retryIntervalStrategy));
        }

        public int MaximumRetryCount { get; }

        public TransientFaultDetectionStrategy TransientFaultDetectionStrategy { get; }

        public RetryIntervalStrategy RetryIntervalStrategy { get; }

        public static RetryPolicy Linear(int maximumRetryCount, TimeSpan increment)
        {
            return new RetryPolicy(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new LinearRetryIntervalStrategy(
                    TimeSpan.Zero,
                    increment,
                    maximumInterval: TimeSpan.MaxValue,
                    immediateFirstRetry: false));
        }

        public static RetryPolicy Constant(int maximumRetryCount, TimeSpan interval, bool immediateFirstRetry)
        {
            return new RetryPolicy(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new ConstantRetryIntervalStrategy(interval, immediateFirstRetry));
        }

        public Task Run(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            async Task Run()
            {
                int retryCount = 0;
                Try:
                try
                {
                    await operation.Invoke(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                when (TransientFaultDetectionStrategy.IsTransientException(exception) && retryCount < MaximumRetryCount)
                {
                    await Task.Delay(RetryIntervalStrategy.GetInterval(retryCount));
                    retryCount++;
                    goto Try;
                }
            }

            return Run();
        }

        public Task Run<T>(
            Func<T, CancellationToken, Task> operation,
            T arg,
            CancellationToken cancellationToken)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return Run(ct => operation.Invoke(arg, ct), cancellationToken);
        }
    }
}
