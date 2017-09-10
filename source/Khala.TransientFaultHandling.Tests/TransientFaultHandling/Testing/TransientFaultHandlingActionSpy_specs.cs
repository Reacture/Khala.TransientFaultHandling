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
    public class TransientFaultHandlingActionSpy_specs
    {
        public interface IFunctionProvider
        {
            void Action<T>(T arg);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(TransientFaultHandlingActionSpy));
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo p in typeof(TransientFaultHandlingActionSpy).GetProperties())
            {
                p.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void Policy_returns_RetryPolicy_instance()
        {
            var sut = new TransientFaultHandlingActionSpy();
            RetryPolicy actual = sut.Policy;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public async Task given_Operation_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingActionSpy();
            await sut.Policy.Run(() => sut.Operation(CancellationToken.None));

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_Operation_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingActionSpy();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_Operation_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingActionSpy();
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
        [DataRow(true)]
        [DataRow(false)]
        public async Task Operation_invokes_callback(bool canceled)
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingActionSpy(functionProvider.Action);
            var cancellationToken = new CancellationToken(canceled);

            await sut.Operation(cancellationToken);

            Mock.Get(functionProvider).Verify(x => x.Action(cancellationToken), Times.Once());
        }

        [TestMethod]
        public void Operation_absorbs_callback_exception()
        {
            var sut = new TransientFaultHandlingActionSpy(cancellationToken => throw new InvalidOperationException());
            Func<Task> action = () => sut.Operation(CancellationToken.None);
            action.ShouldNotThrow();
        }
    }
}
