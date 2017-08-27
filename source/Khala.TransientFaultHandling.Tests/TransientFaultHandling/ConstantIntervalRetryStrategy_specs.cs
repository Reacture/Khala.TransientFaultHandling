namespace Khala.TransientFaultHandling
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;

    [TestClass]
    public class ConstantIntervalRetryStrategy_specs
    {
        [TestMethod]
        public void sut_inherits_RetryStrategy()
        {
            typeof(ConstantIntervalRetryStrategy).BaseType.Should().Be(typeof(RetryStrategy));
        }

        [TestMethod]
        public void sut_has_Interval_property()
        {
            typeof(ConstantIntervalRetryStrategy)
                .Should().HaveProperty<TimeSpan>("Interval")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void constructor_sets_Interval_correctly()
        {
            var fixture = new Fixture();
            var interval = fixture.Create<TimeSpan>();

            var sut = new ConstantIntervalRetryStrategy(1, interval, true);

            sut.Interval.Should().Be(interval);
        }

        [TestMethod]
        public void given_retried_less_than_RetryCount_ShouldRetry_returns_tuple_with_true_shouldRetry()
        {
            int retryCount = 1;
            var sut = new ConstantIntervalRetryStrategy(
                retryCount, TimeSpan.FromMilliseconds(1), true);

            (bool shouldRetry, TimeSpan interval) =
                sut.ShouldRetry(retried: retryCount - 1);

            shouldRetry.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(1, 1)]
        [DataRow(1, 2)]
        public void given_retried_greater_than_or_equal_to_RetryCount_ShouldRetry_returns_tuple_with_false_shouldRetry(int retryCount, int retried)
        {
            var sut = new ConstantIntervalRetryStrategy(
                retryCount, TimeSpan.FromMilliseconds(1), true);

            (bool shouldRetry, TimeSpan interval) = sut.ShouldRetry(retried);

            shouldRetry.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(1, 0)]
        [DataRow(1, 1)]
        public void given_immediateFirstRetry_is_false_ShouldRetry_returns_tuple_with_contant_interval(int retryCount, int retried)
        {
            var fixture = new Fixture();
            var interval = fixture.Create<TimeSpan>();
            var sut = new ConstantIntervalRetryStrategy(
                retryCount, interval, immediateFirstRetry: false);

            (bool shouldRetry, TimeSpan actual) = sut.ShouldRetry(retried);

            actual.Should().Be(interval);
        }

        [TestMethod]
        public void given_immediateFirstRetry_is_true_and_retried_is_zero_ShouldRetry_returns_tuple_with_zero_interval()
        {
            bool immediateFirstRetry = true;
            int retried = 0;
            var fixture = new Fixture();
            var interval = fixture.Create<TimeSpan>();
            var sut = new ConstantIntervalRetryStrategy(1, interval, immediateFirstRetry);

            (bool shouldRetry, TimeSpan actual) = sut.ShouldRetry(retried);

            actual.Should().Be(TimeSpan.Zero);
        }
    }
}
