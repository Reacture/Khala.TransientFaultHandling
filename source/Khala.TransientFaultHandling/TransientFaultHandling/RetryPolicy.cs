namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading.Tasks;

    public class RetryPolicy
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

        public virtual Task Run(Func<Task> operation)
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
                    await operation.Invoke();
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
    }
}
