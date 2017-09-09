namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TransientFaultHandlingFuncSpy_specs
    {
        public interface IFunctionProvider
        {
            void Action<T>(T arg);
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo p in typeof(TransientFaultHandlingFuncSpy<>).GetProperties())
            {
                p.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void Policy_returns_RetryPolicy_instance()
        {
            var sut = new TransientFaultHandlingFuncSpy<Result>(new Result());
            RetryPolicy<Result> actual = sut.Policy;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public async Task given_Operation_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingFuncSpy<Result>(new Result());
            await sut.Policy.Run(sut.Operation, CancellationToken.None);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_Operation_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingFuncSpy<Result>(new Result());
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_Operation_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingFuncSpy<Result>(new Result());
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
        public async Task Operation_returns_result_correctly()
        {
            var result = new Result();
            var sut = new TransientFaultHandlingFuncSpy<Result>(result);

            Result actual = await sut.Operation(CancellationToken.None);

            actual.Should().BeSameAs(result);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Operation_invokes_callback(bool canceled)
        {
            var cancellationToken = new CancellationToken(canceled);
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new TransientFaultHandlingFuncSpy<Result>(
                new Result(),
                functionProvider.Action);

            await sut.Operation(cancellationToken);

            Mock.Get(functionProvider).Verify(x => x.Action(cancellationToken), Times.Once());
        }

        [TestMethod]
        public void Operation_absorbs_callback_exception()
        {
            var sut = new TransientFaultHandlingFuncSpy<Result>(
                new Result(),
                cancellationToken => throw new NotImplementedException());

            Func<Task> action = () => sut.Operation(CancellationToken.None);

            action.ShouldNotThrow<NotImplementedException>();
        }

        public class Result
        {
        }
    }
}
