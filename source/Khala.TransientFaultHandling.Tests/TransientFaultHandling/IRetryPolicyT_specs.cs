namespace Khala.TransientFaultHandling
{
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IRetryPolicyT_specs
    {
        [TestMethod]
        public void sut_inherits_IRetryPolicy()
        {
            typeof(IRetryPolicy<>).Should().Implement<IRetryPolicy>();
        }
    }
}
