namespace Khala.TransientFaultHandling
{
    using System;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;

    [TestClass]
    public class RetryStrategy_specs
    {
        [TestMethod]
        public void sut_is_abstract()
        {
            typeof(RetryStrategy).IsAbstract.Should().BeTrue(because: "the class should be abstract");
        }

        [TestMethod]
        public void sut_has_ShouldRetry_abstract_method()
        {
            typeof(RetryStrategy).Should().HaveMethod("ShouldRetry", new[] { typeof(int) });
            MethodInfo mut = typeof(RetryStrategy).GetMethod("ShouldRetry");
            mut.IsAbstract.Should().BeTrue(because: "the method should be abstract");
            mut.ReturnType.Should().Be(typeof(ValueTuple<bool, TimeSpan>));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void constructor_sets_properties_correctly(bool immediateFirstRetry)
        {
            var fixture = new Fixture();
            var retryCount = fixture.Create<int>();

            var sut = new Mock<RetryStrategy>(retryCount, immediateFirstRetry).Object;

            sut.RetryCount.Should().Be(retryCount);
            sut.ImmediateFirstRetry.Should().Be(immediateFirstRetry);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-10)]
        [DataRow(-4096)]
        public void constructor_has_guard_clause_against_negative_retryCount(int retryCount)
        {
            RetryStrategy sut = null;
            bool fastFirstRetry = true;

            Action action = () => sut = new Mock<RetryStrategy>(retryCount, fastFirstRetry).Object;

            action.ShouldThrow<TargetInvocationException>(
                because: "negative retryCount is not allowed")
                .Which.InnerException.Should().BeOfType<ArgumentOutOfRangeException>()
                .Which.ParamName.Should().Be("retryCount");
        }
    }
}
