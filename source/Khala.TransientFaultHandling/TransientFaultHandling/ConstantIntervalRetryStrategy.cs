namespace Khala.TransientFaultHandling
{
    using System;

    public class ConstantIntervalRetryStrategy : RetryStrategy
    {
        public ConstantIntervalRetryStrategy(
            int retryCount, TimeSpan interval, bool immediateFirstRetry)
            : base(retryCount, immediateFirstRetry)
        {
            Interval = interval;
        }

        public TimeSpan Interval { get; }

#pragma warning disable SA1008 // Ignore 'Opening parenthesis must be spaced correctly' rule for value tuple return type.
        public override (bool shouldRetry, TimeSpan interval) ShouldRetry(int retried)
#pragma warning restore SA1008 // Ignore 'Opening parenthesis must be spaced correctly' rule for value tuple return type.
        {
            return (
                shouldRetry: retried < RetryCount,
                interval: ImmediateFirstRetry && retried == 0 ? TimeSpan.Zero : Interval);
        }
    }
}
