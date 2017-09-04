namespace Khala.TransientFaultHandling
{
    public class TransientFaultDetectionStrategy<T> : TransientFaultDetectionStrategy
    {
        public virtual bool IsTransientResult(T result) => default;
    }
}
