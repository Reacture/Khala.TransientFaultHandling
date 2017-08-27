namespace Khala.TransientFaultHandling
{
    using System;

    public class LinearRetryIntervalStrategy : RetryIntervalStrategy
    {
        public LinearRetryIntervalStrategy(
            TimeSpan initialInterval,
            TimeSpan increment,
            TimeSpan maximumInterval,
            bool immediateFirstRetry)
            : base(immediateFirstRetry)
        {
            InitialInterval = initialInterval;
            Increment = increment;
            MaximumInterval = maximumInterval;
        }

        public TimeSpan InitialInterval { get; }

        public TimeSpan Increment { get; }

        public TimeSpan MaximumInterval { get; }

        protected override TimeSpan GetIntervalFromTick(int tick) =>
            TimeSpan.FromTicks(
                Math.Min(
                    MaximumInterval.Ticks,
                    InitialInterval.Ticks + (Increment.Ticks * (tick - 1))));
    }
}
