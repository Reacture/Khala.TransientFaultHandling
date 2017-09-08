namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultHandlingSpy_specs
    {
        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(TransientFaultHandlingSpy));
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo p in typeof(TransientFaultHandlingSpy).GetProperties())
            {
                p.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void Policy_returns_RetryPolicy_instance()
        {
            var sut = new TransientFaultHandlingSpy();
            RetryPolicy actual = sut.Policy;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public void OperationNonCancellable_returns_Func_Task_instance()
        {
            var sut = new TransientFaultHandlingSpy();
            Func<Task> actual = sut.OperationNonCancellable;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public void OperationCancellable_returns_Func_CancellationToken_Task_instance()
        {
            var sut = new TransientFaultHandlingSpy();
            Func<CancellationToken, Task> actual = sut.OperationCancellable;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public async Task given_OperationNonCancellable_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingSpy();
            await sut.Policy.Run(sut.OperationNonCancellable);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_OperationNonCancellable_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_OperationNonCancellable_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            try
            {
                await sut.OperationNonCancellable.Invoke();
            }
            catch
            {
            }

            Action action = sut.Verify;

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_OperationCancellable_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingSpy();
            await sut.Policy.Run(sut.OperationCancellable, CancellationToken.None);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_OperationCancellable_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_OperationCancellable_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            try
            {
                await sut.OperationCancellable.Invoke(CancellationToken.None);
            }
            catch
            {
            }

            Action action = sut.Verify;

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task OperationNonCancellable_invokes_callback_with_none_cancellation_token()
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingSpy(
                functionProvider.Func<CancellationToken, Task>);

            try
            {
                await sut.OperationNonCancellable.Invoke();
            }
            catch
            {
            }

            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task>(CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public void OperationNonCancellable_consumes_callback_exception()
        {
            var sut = new TransientFaultHandlingSpy(
                cancellationToken => throw new NotImplementedException());

            Func<Task> action = () => sut.OperationNonCancellable.Invoke();

            action.ShouldNotThrow<NotImplementedException>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task OperationCancellable_invokes_callback(bool canceled)
        {
            var cancellationToken = new CancellationToken(canceled);
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingSpy(
                functionProvider.Func<CancellationToken, Task>);

            try
            {
                await sut.OperationCancellable.Invoke(cancellationToken);
            }
            catch
            {
            }

            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task>(cancellationToken), Times.Once());
        }

        [TestMethod]
        public void OperationCancellable_consumes_callback_exception()
        {
            var sut = new TransientFaultHandlingSpy(
                cancellationToken => throw new NotImplementedException());

            Func<Task> action = () => sut.OperationCancellable.Invoke(CancellationToken.None);

            action.ShouldNotThrow<NotImplementedException>();
        }
    }
}
