namespace Khala.TransientFaultHandling
{
    using System;

    public abstract class RetryStrategy
    {
        protected RetryStrategy(int retryCount, bool immediateFirstRetry)
        {
            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Value cannot be negative.");
            }

            RetryCount = retryCount;
            ImmediateFirstRetry = immediateFirstRetry;
        }

        public int RetryCount { get; }

        public bool ImmediateFirstRetry { get; }

#pragma warning disable SA1008 // Ignore 'Opening parenthesis must be spaced correctly' rule for value tuple return type.
        public abstract (bool shouldRetry, TimeSpan interval) ShouldRetry(int retried);
#pragma warning restore SA1008 // Ignore 'Opening parenthesis must be spaced correctly' rule for value tuple return type.
    }
}
