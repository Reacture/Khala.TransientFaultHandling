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
        public async Task given_Operation_non_cancellable_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingSpy();
            await sut.Policy.Run(sut.Operation);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_Operation_non_cancellable_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_Operation_non_cancellable_invoked_directly_Verify_throws_InvalidOperationException()
        {
            // Arrange
            var sut = new TransientFaultHandlingSpy();
            try
            {
                await sut.Operation();
            }
            catch
            {
            }

            await sut.Policy.Run(sut.Operation);

            // Act
            Action action = sut.Verify;

            // Assert
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_Operation_cancellable_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingSpy();
            await sut.Policy.Run(sut.Operation, CancellationToken.None);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_Operation_cancellable_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_Operation_cancellable_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            try
            {
                await sut.Operation(CancellationToken.None);
            }
            catch
            {
            }

            Action action = sut.Verify;

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task Operation_non_cancellable_invokes_callback_with_none_cancellation_token()
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingSpy(
                functionProvider.Func<CancellationToken, Task>);

            try
            {
                await sut.Operation();
            }
            catch
            {
            }

            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task>(CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public void Operation_non_cancellable_consumes_callback_exception()
        {
            var sut = new TransientFaultHandlingSpy(
                cancellationToken => throw new NotImplementedException());

            Func<Task> action = () => sut.Operation();

            action.ShouldNotThrow<NotImplementedException>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Operation_cancellable_invokes_callback(bool canceled)
        {
            var cancellationToken = new CancellationToken(canceled);
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingSpy(
                functionProvider.Func<CancellationToken, Task>);

            try
            {
                await sut.Operation(cancellationToken);
            }
            catch
            {
            }

            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task>(cancellationToken), Times.Once());
        }

        [TestMethod]
        public void Operation_cancellable_consumes_callback_exception()
        {
            var sut = new TransientFaultHandlingSpy(
                cancellationToken => throw new NotImplementedException());

            Func<Task> action = () => sut.Operation(CancellationToken.None);

            action.ShouldNotThrow<NotImplementedException>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task OperationT1_relays_to_Operation_cancellable(bool canceled)
        {
            var fixture = new Fixture();
            var sut = Mock.Of<TransientFaultHandlingSpy>();
            var cancellationToken = new CancellationToken(canceled);

            await sut.Operation(fixture.Create<int>(), cancellationToken);

            Mock.Get(sut).Verify(x => x.Operation(cancellationToken), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task OperationT2_relays_to_Operation_cancellable(bool canceled)
        {
            var fixture = new Fixture();
            var sut = Mock.Of<TransientFaultHandlingSpy>();
            var cancellationToken = new CancellationToken(canceled);

            await sut.Operation(fixture.Create<int>(), fixture.Create<uint>(), cancellationToken);

            Mock.Get(sut).Verify(x => x.Operation(cancellationToken), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task OperationT3_relays_to_Operation_cancellable(bool canceled)
        {
            var fixture = new Fixture();
            var sut = Mock.Of<TransientFaultHandlingSpy>();
            var cancellationToken = new CancellationToken(canceled);

            await sut.Operation(fixture.Create<int>(), fixture.Create<uint>(), fixture.Create<long>(), cancellationToken);

            Mock.Get(sut).Verify(x => x.Operation(cancellationToken), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task OperationT4_relays_to_Operation_cancellable(bool canceled)
        {
            var fixture = new Fixture();
            var sut = Mock.Of<TransientFaultHandlingSpy>();
            var cancellationToken = new CancellationToken(canceled);

            await sut.Operation(fixture.Create<int>(), fixture.Create<uint>(), fixture.Create<long>(), fixture.Create<ulong>(), cancellationToken);

            Mock.Get(sut).Verify(x => x.Operation(cancellationToken), Times.Once());
        }
    }
}
