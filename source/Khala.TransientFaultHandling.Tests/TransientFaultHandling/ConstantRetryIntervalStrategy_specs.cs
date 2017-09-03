namespace Khala.TransientFaultHandling
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;

    [TestClass]
    public class ConstantRetryIntervalStrategy_specs
    {
        [TestMethod]
        public void sut_inherits_RetryIntervalStrategy()
        {
            typeof(ConstantRetryIntervalStrategy).BaseType.Should().Be(typeof(RetryIntervalStrategy));
        }

        [TestMethod]
        public void sut_has_Interval_property()
        {
            typeof(ConstantRetryIntervalStrategy)
                .Should().HaveProperty<TimeSpan>("Interval")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void constructor_sets_Interval_correctly()
        {
            var fixture = new Fixture();
            var interval = fixture.Create<TimeSpan>();

            var sut = new ConstantRetryIntervalStrategy(interval, false);

            sut.Interval.Should().Be(interval);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetIntervalFromZeroBasedTick_returns_Interval_always(bool zeroRetried)
        {
            var fixture = new Fixture();
            var interval = fixture.Create<TimeSpan>();
            var sut = new ConstantRetryIntervalStrategy(interval, immediateFirstRetry: false);
            int retried = zeroRetried ? 0 : fixture.Create<int>();

            TimeSpan actual = sut.GetInterval(retried);

            actual.Should().Be(interval);
        }
    }
}
