namespace Khala.TransientFaultHandling
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;

    [TestClass]
    public class LinearRetryIntervalStrategy_specs
    {
        [TestMethod]
        public void sut_inherits_RetryIntervalStrategy()
        {
            typeof(LinearRetryIntervalStrategy).BaseType.Should().Be(typeof(RetryIntervalStrategy));
        }

        [TestMethod]
        public void sut_has_InitialInterval_property()
        {
            typeof(LinearRetryIntervalStrategy)
                .Should().HaveProperty<TimeSpan>("InitialInterval")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void sut_has_Increment_property()
        {
            typeof(LinearRetryIntervalStrategy)
                .Should().HaveProperty<TimeSpan>("Increment")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void sut_has_MaximumInterval_property()
        {
            typeof(LinearRetryIntervalStrategy)
                .Should().HaveProperty<TimeSpan>("MaximumInterval")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void constructor_sets_properties_correctly()
        {
            var fixture = new Fixture();
            var initialInterval = fixture.Create<TimeSpan>();
            var increment = fixture.Create<TimeSpan>();
            var maximumInterval = fixture.Create<TimeSpan>();
            var immediateFirstRetry = true;

            var sut = new LinearRetryIntervalStrategy(
                initialInterval, increment, maximumInterval, immediateFirstRetry);

            sut.InitialInterval.Should().Be(initialInterval);
            sut.Increment.Should().Be(increment);
            sut.MaximumInterval.Should().Be(maximumInterval);
        }

        [TestMethod]
        public void GetIntervalFromTick_returns_linear_growth_result()
        {
            var fixture = new Fixture();
            var initialInterval = fixture.Create<TimeSpan>();
            var increment = fixture.Create<TimeSpan>();
            var sut = new LinearRetryIntervalStrategy(
                initialInterval, increment, TimeSpan.MaxValue, immediateFirstRetry: false);
            var retried = fixture.Create<int>();

            TimeSpan actual = sut.GetInterval(retried);

            actual.Ticks.Should().Be(initialInterval.Ticks + (increment.Ticks * retried));
        }

        [TestMethod]
        public void GetIntervalFromTick_return_value_does_not_exceed_MaximumInterval()
        {
            var initialInterval = TimeSpan.Zero;
            var increment = TimeSpan.FromTicks(10);
            var maximumInterval = TimeSpan.FromTicks(19);
            var sut = new LinearRetryIntervalStrategy(
                initialInterval, increment, maximumInterval, immediateFirstRetry: false);
            int retried = 2;

            TimeSpan actual = sut.GetInterval(retried);

            actual.Should().BeLessOrEqualTo(maximumInterval);
        }
    }
}
