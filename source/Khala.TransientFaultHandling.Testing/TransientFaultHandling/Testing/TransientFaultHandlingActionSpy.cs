namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TransientFaultHandlingActionSpy : TransientFaultHandlingActionSpyBase
    {
        private readonly Action<CancellationToken> _callback;

        public TransientFaultHandlingActionSpy(Action<CancellationToken> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public TransientFaultHandlingActionSpy()
            : this(cancellationToken => { })
        {
        }

        public virtual Task Operation(CancellationToken cancellationToken)
        {
            OnInvoked();

            try
            {
                _callback.Invoke(cancellationToken);
            }
            catch
            {
            }

            return Task.FromResult(true);
        }

        public Task Operation() => Operation(CancellationToken.None);
    }
}
