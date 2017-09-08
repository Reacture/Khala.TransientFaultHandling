﻿namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TransientFaultHandlingSpy<T>
    {
        private static readonly Random _random = new Random();

        private readonly Func<CancellationToken, Task<T>> _callback;
        private readonly int _maximumRetryCount;
        private readonly int _transientFaultCount;
        private int _invocationCount;

        public TransientFaultHandlingSpy()
            : this(cancellationToken => Task.FromResult(default(T)))
        {
        }

        public TransientFaultHandlingSpy(Func<CancellationToken, Task<T>> callback)
        {
            _callback = callback;
            _maximumRetryCount = _random.Next(1000, 2000);
            _transientFaultCount = _random.Next(0, _maximumRetryCount);
            _invocationCount = 0;

            Policy = new RetryPolicy<T>(
                _maximumRetryCount,
                new TransientFaultDetectionStrategy<T>(),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero, immediateFirstRetry: true));

            OperationNonCancellable = Operation;
            OperationCancellable = Operation;
        }

        public RetryPolicy<T> Policy { get; }

        public Func<Task<T>> OperationNonCancellable { get; }

        public Func<CancellationToken, Task<T>> OperationCancellable { get; }

        public void Verify()
        {
            if (_invocationCount == _transientFaultCount + 1)
            {
                return;
            }

            throw new InvalidOperationException("It seems that operation did not invoked by retry policy.");
        }

        private Task<T> Operation() => Operation(CancellationToken.None);

        private async Task<T> Operation(CancellationToken cancellationToken)
        {
            _invocationCount++;

            try
            {
                await _callback.Invoke(cancellationToken);
            }
            catch
            {
            }

            if (_invocationCount == _transientFaultCount + 1)
            {
                return default(T);
            }

            throw new InvalidOperationException("Transient fault occured. Try more please.");
        }
    }
}
