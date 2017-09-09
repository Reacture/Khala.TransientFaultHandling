namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TransientFaultHandlingFuncSpy<TResult>
    {
        private static readonly Random _random = new Random();

        private readonly Action<CancellationToken> _callback;
        private readonly int _maximumRetryCount;
        private readonly int _transientFaultCount;
        private int _intercepted;
        private int _invocations;

        public TransientFaultHandlingFuncSpy()
            : this(cancellationToken => Task.FromResult(default(TResult)))
        {
        }

        public TransientFaultHandlingFuncSpy(Action<CancellationToken> callback)
        {
            _callback = callback;
            _maximumRetryCount = _random.Next(1000, 2000);
            _transientFaultCount = _random.Next(0, _maximumRetryCount);
            _intercepted = 0;
            _invocations = 0;

            Policy = new SpyRetryPolicy(
                _maximumRetryCount,
                new TransientFaultDetectionStrategy<TResult>(),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero, immediateFirstRetry: true),
                Interceptor);
        }

        public RetryPolicy<TResult> Policy { get; }

        public virtual Task<TResult> Operation(CancellationToken cancellationToken)
        {
            _invocations++;

            try
            {
                _callback.Invoke(cancellationToken);
            }
            catch
            {
            }

            return Task.FromResult(default(TResult));
        }

        public Task<TResult> Operation() => Operation(CancellationToken.None);

        public void Verify()
        {
            if (_invocations == _transientFaultCount + 1 &&
                _invocations == _intercepted)
            {
                return;
            }

            throw new InvalidOperationException("It seems that the operation did not invoked by retry policy or invoked directly.");
        }

        private Func<CancellationToken, Task<TResult>> Interceptor(Func<CancellationToken, Task<TResult>> operation)
        {
            async Task<TResult> Intercept(CancellationToken cancellationToken)
            {
                _intercepted++;

                TResult result = await operation.Invoke(cancellationToken);

                if (_invocations == _transientFaultCount + 1)
                {
                    return result;
                }

                throw new InvalidOperationException("Transient fault occured. Try more please.");
            }

            return Intercept;
        }

        private class SpyRetryPolicy : RetryPolicy<TResult>
        {
            private readonly Func<Func<CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>> _interceptor;

            public SpyRetryPolicy(
                int maximumRetryCount,
                TransientFaultDetectionStrategy<TResult> transientFaultDetectionStrategy,
                RetryIntervalStrategy retryIntervalStrategy,
                Func<Func<CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>> interceptor)
                : base(maximumRetryCount, transientFaultDetectionStrategy, retryIntervalStrategy)
            {
                _interceptor = interceptor;
            }

            public override Task<TResult> Run(
                Func<CancellationToken, Task<TResult>> operation,
                CancellationToken cancellationToken)
            {
                return base.Run(_interceptor.Invoke(operation), cancellationToken);
            }
        }
    }
}
