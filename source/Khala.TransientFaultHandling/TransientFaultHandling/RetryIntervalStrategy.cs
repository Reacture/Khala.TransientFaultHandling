namespace Khala.TransientFaultHandling
{
    using System;

    public abstract class RetryIntervalStrategy
    {
        protected RetryIntervalStrategy(bool immediateFirstRetry)
        {
            ImmediateFirstRetry = immediateFirstRetry;
        }

        public bool ImmediateFirstRetry { get; }

        public TimeSpan GetInterval(int retried)
        {
            if (retried < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retried), "Value cannot be negative.");
            }

            return ImmediateFirstRetry
                ? (retried == 0 ? TimeSpan.Zero : GetIntervalFromZeroBasedTick(retried - 1))
                : GetIntervalFromZeroBasedTick(retried);
        }

        protected abstract TimeSpan GetIntervalFromZeroBasedTick(int tick);
    }
}
