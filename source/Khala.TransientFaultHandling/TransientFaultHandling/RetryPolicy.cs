namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading.Tasks;

    public class RetryPolicy
    {
        private readonly int _maximumRetryCount;
        private readonly TransientFaultDetectionStrategy _transientFaultDetectionStrategy;
        private readonly RetryIntervalStrategy _retryIntervalStrategy;

        public RetryPolicy(
            int maximumRetryCount,
            TransientFaultDetectionStrategy transientFaultDetectionStrategy,
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
                when (_transientFaultDetectionStrategy.IsTransientException(exception) && retryCount < _maximumRetryCount)
                {
                    await Task.Delay(_retryIntervalStrategy.GetInterval(retryCount));
                    retryCount++;
                    goto Try;
                }
            }

            return Run();
        }
    }
}
