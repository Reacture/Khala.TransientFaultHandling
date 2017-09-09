namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TransientFaultHandlingActionSpy<T> : TransientFaultHandlingActionSpyBase
    {
        private readonly Action<T, CancellationToken> _callback;

        public TransientFaultHandlingActionSpy(Action<T, CancellationToken> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public TransientFaultHandlingActionSpy()
            : this((arg, cancellationToken) => { })
        {
        }

        public virtual Task Operation(T arg, CancellationToken cancellationToken)
        {
            OnInvoked();

            try
            {
                _callback.Invoke(arg, cancellationToken);
            }
            catch
            {
            }

            return Task.FromResult(true);
        }
    }
}
