namespace Khala.TransientFaultHandling
{
    public class TransientFaultDetectionStrategy<T> : TransientFaultDetectionStrategy
    {
#pragma warning disable IDE0034 // Ignore "Simplify 'default' expression" warning because Equals() method is non generic.
        public virtual bool IsTransientResult(T result) => default(bool);
#pragma warning restore IDE0034 // Ignore "Simplify 'default' expression" warning because Equals() method is non generic.
    }
}
