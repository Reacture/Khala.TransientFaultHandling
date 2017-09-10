namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading.Tasks;

    public class RetryPolicy<T>
    {
        public RetryPolicy(
            int maximumRetryCount,
            TransientFaultDetectionStrategy<T> transientFaultDetectionStrategy,
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

        public TransientFaultDetectionStrategy<T> TransientFaultDetectionStrategy { get; }

        public RetryIntervalStrategy RetryIntervalStrategy { get; }

        public static RetryPolicy<T> LinearTransientDefault(int maximumRetryCount, TimeSpan increment)
        {
            return new RetryPolicy<T>(
                maximumRetryCount,
                new TransientDefaultDetectionStrategy<T>(),
                new LinearRetryIntervalStrategy(
                    TimeSpan.Zero,
                    increment,
                    maximumInterval: TimeSpan.MaxValue,
                    immediateFirstRetry: false));
        }

        public static RetryPolicy<T> ConstantTransientDefault(int maximumRetryCount, TimeSpan interval, bool immediateFirstRetry)
        {
            return new RetryPolicy<T>(
                maximumRetryCount,
                new TransientDefaultDetectionStrategy<T>(),
                new ConstantRetryIntervalStrategy(interval, immediateFirstRetry));
        }

        public virtual Task<T> Run(Func<Task<T>> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            async Task<T> Run()
            {
                int retryCount = 0;
                T result = default(T);
                Try:
                try
                {
                    result = await operation.Invoke();
                }
                catch (Exception exception)
                when (TransientFaultDetectionStrategy.IsTransientException(exception) && retryCount < MaximumRetryCount)
                {
                    await Task.Delay(RetryIntervalStrategy.GetInterval(retryCount));
                    retryCount++;
                    goto Try;
                }

                if (TransientFaultDetectionStrategy.IsTransientResult(result) &&
                    retryCount < MaximumRetryCount)
                {
                    await Task.Delay(RetryIntervalStrategy.GetInterval(retryCount));
                    retryCount++;
                    goto Try;
                }

                return result;
            }

            return Run();
        }
    }
}
