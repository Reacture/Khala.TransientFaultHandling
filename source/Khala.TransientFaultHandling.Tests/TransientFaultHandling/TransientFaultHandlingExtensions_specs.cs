namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Khala.TransientFaultHandling.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultHandlingExtensions_specs
    {
        public interface IFunctionProvider
        {
            void Action<TArg>(TArg arg);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(TransientFaultHandlingExtensions));
        }

        [TestMethod]
        public async Task Run_relays_to_retryPolicy()
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingActionSpy(functionProvider.Action);

            await spy.Policy.Run(() => spy.Operation(CancellationToken.None));

            spy.Verify();
        }

        [TestMethod]
        public async Task Run_relays_to_retryPolicy_with_none_cancellation_token()
        {
            var maximumRetryCount = 1;
            var retryPolicyMock = new Mock<RetryPolicy>(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new ConstantRetryIntervalStrategy(
                    TimeSpan.Zero,
                    immediateFirstRetry: true));
            var retryPolicy = retryPolicyMock.Object;

            await retryPolicy.Run(() => Task.FromResult(true));

            retryPolicyMock.Verify(x => x.Run(It.IsAny<Func<CancellationToken, Task>>(), CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task RunT_relays_to_retryPolicy()
        {
            var result = new Result();
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingFuncSpy<Result>(result, functionProvider.Action);

            Result actual = await spy.Policy.Run(() => spy.Operation(CancellationToken.None));

            spy.Verify();
            actual.Should().BeSameAs(result);
        }

        [TestMethod]
        public async Task RunT_relays_to_retryPolicy_with_none_cancellation_token()
        {
            var maximumRetryCount = 1;
            var retryPolicyMock = new Mock<RetryPolicy<Result>>(
                maximumRetryCount,
                new TransientFaultDetectionStrategy<Result>(),
                new ConstantRetryIntervalStrategy(
                    TimeSpan.Zero,
                    immediateFirstRetry: true));
            var retryPolicy = retryPolicyMock.Object;
            var result = new Result();

            await retryPolicy.Run(() => Task.FromResult(result));

            retryPolicyMock.Verify(x => x.Run(It.IsAny<Func<CancellationToken, Task<Result>>>(), CancellationToken.None), Times.Once());
        }

        public class Result
        {
        }
    }
}
