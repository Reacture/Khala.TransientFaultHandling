namespace Khala.TransientFaultHandling
{
    using System;

    public class DelegatingTransientFaultDetectionStrategy<T> : TransientFaultDetectionStrategy<T>
    {
        private Func<Exception, bool> _exceptionFunc;
        private Func<T, bool> _resultFunc;

        public DelegatingTransientFaultDetectionStrategy(
            Func<Exception, bool> exceptionFunc,
            Func<T, bool> resultFunc)
        {
            _exceptionFunc = exceptionFunc ?? throw new ArgumentNullException(nameof(exceptionFunc));
            _resultFunc = resultFunc ?? throw new ArgumentNullException(nameof(resultFunc));
        }

        public override bool IsTransientException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return _exceptionFunc.Invoke(exception);
        }

        public override bool IsTransientResult(T result)
        {
            return _resultFunc.Invoke(result);
        }
    }
}
