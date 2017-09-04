namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TransientFaultHandlingExtensions
    {
        public static Task Run(this RetryPolicy retryPolicy, Func<Task> operation)
        {
            if (retryPolicy == null)
            {
                throw new ArgumentNullException(nameof(retryPolicy));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return retryPolicy.Run(cancellationToken => operation.Invoke(), CancellationToken.None);
        }

        public static Task<T> Run<T>(this RetryPolicy<T> retryPolicy, Func<Task<T>> operation)
        {
            if (retryPolicy == null)
            {
                throw new ArgumentNullException(nameof(retryPolicy));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return retryPolicy.Run(cancellationToken => operation.Invoke(), CancellationToken.None);
        }
    }
}
