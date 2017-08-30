namespace Khala.TransientFaultHandling
{
    using System;

    public class DelegatingRetryIntervalStrategy : RetryIntervalStrategy
    {
        private readonly Func<int, TimeSpan> _func;

        public DelegatingRetryIntervalStrategy(Func<int, TimeSpan> func, bool immediateFirstRetry)
            : base(immediateFirstRetry)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        protected override TimeSpan GetIntervalFromTick(int tick)
        {
            return _func.Invoke(tick);
        }
    }
}
