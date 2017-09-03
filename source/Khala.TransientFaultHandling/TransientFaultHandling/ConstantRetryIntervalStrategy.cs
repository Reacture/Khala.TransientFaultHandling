namespace Khala.TransientFaultHandling
{
    using System;

    public class ConstantRetryIntervalStrategy : RetryIntervalStrategy
    {
        public ConstantRetryIntervalStrategy(TimeSpan interval, bool immediateFirstRetry)
            : base(immediateFirstRetry)
        {
            Interval = interval;
        }

        public TimeSpan Interval { get; }

        protected override TimeSpan GetIntervalFromZeroBasedTick(int tick) => Interval;
    }
}
