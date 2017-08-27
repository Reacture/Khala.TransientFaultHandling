namespace Khala.TransientFaultHandling
{
    using System;

    public class TransientFaultDetectionStrategy
    {
        public virtual bool IsTransientException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return true;
        }
    }
}
