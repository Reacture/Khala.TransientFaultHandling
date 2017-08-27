namespace Khala.TransientFaultHandling
{
    using System;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Moq.Protected;
    using Ploeh.AutoFixture;

    [TestClass]
    public class RetryIntervalStrategy_specs
    {
        [TestMethod]
        public void sut_is_abstract()
        {
            typeof(RetryIntervalStrategy).IsAbstract.Should().BeTrue(because: "the class should be abstract");
        }

        [TestMethod]
        public void sut_has_ImmediateFirstRetry_property()
        {
            typeof(RetryIntervalStrategy).Should()
                .HaveProperty<bool>("ImmediateFirstRetry")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void constructor_sets_ImmediateFirstRetry_correctly(bool immediateFirstRetry)
        {
            var sut = new Mock<RetryIntervalStrategy>(immediateFirstRetry).Object;
            sut.ImmediateFirstRetry.Should().Be(immediateFirstRetry);
        }

        [TestMethod]
        public void sut_has_GetInterval_method()
        {
            const string MethodName = "GetInterval";
            typeof(RetryIntervalStrategy).Should()
                .HaveMethod(MethodName, new[] { typeof(int) });
            MethodInfo mut = typeof(RetryIntervalStrategy).GetMethod(MethodName);
            mut.ReturnType.Should().Be(typeof(TimeSpan));
        }

        [TestMethod]
        public void given_true_ImmediateFirstRetry_and_zero_retried_GetInterval_returns_zero()
        {
            var sut = new Mock<RetryIntervalStrategy>(true).Object;
            TimeSpan actual = sut.GetInterval(retried: 0);
            actual.Should().Be(TimeSpan.Zero);
        }

        [TestMethod]
        public void given_true_ImmediateFirstRetry_GetInterval_relays_to_GetIntervalFromTick_with_retried()
        {
            const bool ImmediateFirstRetry = true;
            var mock = new Mock<RetryIntervalStrategy>(ImmediateFirstRetry);
            var sut = mock.Object;
            var fixture = new Fixture();
            var retried = fixture.Create<int>();
            var expected = fixture.Create<TimeSpan>();
            mock.Protected().Setup<TimeSpan>("GetIntervalFromTick", retried).Returns(expected);

            TimeSpan actual = sut.GetInterval(retried);

            actual.Should().Be(expected);
            mock.Protected().Verify<TimeSpan>("GetIntervalFromTick", Times.Once(), retried);
        }

        [TestMethod]
        public void given_true_ImmediateFirstRetry_and_zero_retried_GetInterval_does_not_call_GetIntervalFromTick()
        {
            const bool ImmediateFirstRetry = true;
            var mock = new Mock<RetryIntervalStrategy>(ImmediateFirstRetry);
            var sut = mock.Object;
            int retried = 0;

            sut.GetInterval(retried);

            mock.Protected().Verify<TimeSpan>("GetIntervalFromTick", Times.Never(), ItExpr.IsAny<int>());
        }

        [TestMethod]
        public void given_false_ImmediateFirstRetry_GetInterval_relays_to_GetIntervalFromTick_with_retried_plus_one()
        {
            const bool ImmediateFirstRetry = false;
            var mock = new Mock<RetryIntervalStrategy>(ImmediateFirstRetry);
            var sut = mock.Object;
            var fixture = new Fixture();
            var retried = fixture.Create<int>();
            var expected = fixture.Create<TimeSpan>();
            mock.Protected().Setup<TimeSpan>("GetIntervalFromTick", retried + 1).Returns(expected);

            TimeSpan actual = sut.GetInterval(retried);

            actual.Should().Be(expected);
            mock.Protected().Verify<TimeSpan>("GetIntervalFromTick", Times.Once(), ItExpr.IsAny<int>());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetInterval_has_guard_clause_against_negative_retried(bool immediateFirstRetry)
        {
            var sut = new Mock<RetryIntervalStrategy>(immediateFirstRetry).Object;
            Action action = () => sut.GetInterval(-1);
            action.ShouldThrow<ArgumentOutOfRangeException>(
                because: "negative retried is not allowed")
                .Where(x => x.ParamName == "retried");
        }
    }
}
