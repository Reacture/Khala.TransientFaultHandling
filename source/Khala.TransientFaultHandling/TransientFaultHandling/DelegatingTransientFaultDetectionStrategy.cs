namespace Khala.TransientFaultHandling
{
    using System;

    public class DelegatingTransientFaultDetectionStrategy : TransientFaultDetectionStrategy
    {
        private Func<Exception, bool> _func;

        public DelegatingTransientFaultDetectionStrategy(Func<Exception, bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override bool IsTransientException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return _func.Invoke(exception);
        }
    }
}
