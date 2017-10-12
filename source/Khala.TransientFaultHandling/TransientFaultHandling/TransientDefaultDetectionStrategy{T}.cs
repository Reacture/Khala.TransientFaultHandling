namespace Khala.TransientFaultHandling
{
    public class TransientDefaultDetectionStrategy<T> : TransientFaultDetectionStrategy<T>
    {
#pragma warning disable IDE0034 // Ignore "Simplify 'default' expression" warning because Equals() method is non-generic.
        public override bool IsTransientResult(T result) => Equals(result, default(T));
#pragma warning restore IDE0034 // Ignore "Simplify 'default' expression" warning because Equals() method is non-generic.
    }
}
