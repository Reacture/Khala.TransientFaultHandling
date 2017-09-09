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
    public class TransientFaultHandlingActionSpyT_specs
    {
        public interface IFunctionProvider
        {
            void Action<T1, T2>(T1 arg1, T2 arg2);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(TransientFaultHandlingActionSpy<>));
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo p in typeof(TransientFaultHandlingActionSpy<>).GetProperties())
            {
                p.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void Policy_returns_RetryPolicy_instance()
        {
            var sut = new TransientFaultHandlingActionSpy<Arg>();
            RetryPolicy actual = sut.Policy;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public async Task given_Operation_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingActionSpy<Arg>();
            var arg = new Arg();
            await sut.Policy.Run(cancellationToken => sut.Operation(arg, cancellationToken), CancellationToken.None);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_Operation_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingActionSpy<Arg>();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_Operation_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingActionSpy<Arg>();
            var arg = new Arg();
            try
            {
                await sut.Operation(arg, CancellationToken.None);
            }
            catch
            {
            }

            Action action = sut.Verify;

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Operation_invokes_callback(bool canceled)
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingActionSpy<Arg>(functionProvider.Action);
            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            await sut.Operation(arg, cancellationToken);

            Mock.Get(functionProvider).Verify(x => x.Action(arg, cancellationToken), Times.Once());
        }

        [TestMethod]
        public void Operation_absorbs_callback_exception()
        {
            var sut = new TransientFaultHandlingActionSpy<Arg>((arg, cancellationToken) => throw new InvalidOperationException());
            Func<Task> action = () => sut.Operation(new Arg(), CancellationToken.None);
            action.ShouldNotThrow();
        }

        [TestMethod]
        public async Task modest_Operation_relays_with_none_cancellation_token()
        {
            var sut = Mock.Of<TransientFaultHandlingActionSpy<Arg>>();
            var arg = new Arg();
            await sut.Operation(arg);
            Mock.Get(sut).Verify(x => x.Operation(arg, CancellationToken.None), Times.Once());
        }

        public class Arg
        {
        }
    }
}
