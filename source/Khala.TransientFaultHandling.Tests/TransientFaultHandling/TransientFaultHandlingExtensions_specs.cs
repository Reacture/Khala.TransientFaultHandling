namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
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
            TResult Func<T, TResult>(T arg);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(TransientFaultHandlingExtensions));
        }

        [TestMethod]
        public async Task Run_relays_to_retryPolicy_with_none_cancellation_token()
        {
            // Arrange
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingSpy(
                functionProvider.Func<CancellationToken, Task>);
            RetryPolicy retryPolicy = spy.Policy;
            Func<Task> operation = spy.OperationNonCancellable;

            // Act
            await retryPolicy.Run(operation);

            // Assert
            spy.Verify();
            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task>(CancellationToken.None));
        }

        [TestMethod]
        public async Task RunT_relays_to_retryPolicy_with_non_cancellation_token()
        {
            // Arrange
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingSpy<Result>(
                functionProvider.Func<CancellationToken, Task<Result>>);
            RetryPolicy<Result> retryPolicy = spy.Policy;
            Func<Task<Result>> operation = spy.OperationNonCancellable;

            // Act
            await retryPolicy.Run(operation);

            // Assert
            spy.Verify();
            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(CancellationToken.None));
        }

        public class Result
        {
        }
    }
}
