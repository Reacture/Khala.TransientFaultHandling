namespace Khala.TransientFaultHandling
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class DelegatingRetryIntervalStrategy_specs
    {
        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);
        }

        [TestMethod]
        public void sut_inherits_RetryIntervalStrategy()
        {
            typeof(DelegatingRetryIntervalStrategy).BaseType.Should().Be(typeof(RetryIntervalStrategy));
        }

        [TestMethod]
        public void GetInterval_relays_to_function()
        {
            var fixture = new Fixture();
            var retried = fixture.Create<int>();
            var expected = fixture.Create<TimeSpan>();
            var functionProvider = Mock.Of<IFunctionProvider>(
                x => x.Func<int, TimeSpan>(retried + 1) == expected);
            var sut = new DelegatingRetryIntervalStrategy(functionProvider.Func<int, TimeSpan>, false);

            TimeSpan actual = sut.GetInterval(retried);

            actual.Should().Be(expected);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var sut = typeof(DelegatingRetryIntervalStrategy);
            new GuardClauseAssertion(new Fixture()).Verify(sut);
        }
    }
}
