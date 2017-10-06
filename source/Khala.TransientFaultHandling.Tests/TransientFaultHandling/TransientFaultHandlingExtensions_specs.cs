namespace Khala.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultHandlingExtensions_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(TransientFaultHandlingExtensions));
        }

        [TestMethod]
        public void Run_does_not_invoke_operation_directly()
        {
            var retryPolicy = Mock.Of<IRetryPolicy>();
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Task>);

            Mock.Get(functionProvider).Verify(x => x.Func<Task>(), Times.Never());
        }

        [TestMethod]
        public void Run_invokes_Run_with_none_cancellation_token()
        {
            var retryPolicy = Mock.Of<IRetryPolicy>();

            retryPolicy.Run(() => Task.CompletedTask);

            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    CancellationToken.None),
                Times.Once());
        }

        [TestMethod]
        public void Run_invokes_Run_with_wrapper_function()
        {
            Func<CancellationToken, Task> wrapper = null;
            var retryPolicy = Mock.Of<IRetryPolicy>();
            Mock.Get(retryPolicy)
                .Setup(x => x.Run(It.IsAny<Func<CancellationToken, Task>>(), CancellationToken.None))
                .Callback<Func<CancellationToken, Task>, CancellationToken>((f, t) => wrapper = f)
                .Returns(Task.CompletedTask);
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Task>);

            wrapper.Should().NotBeNull();
            wrapper.Invoke(CancellationToken.None);
            Mock.Get(functionProvider).Verify(x => x.Func<Task>(), Times.Once());
        }

        [TestMethod]
        public void RunT_does_not_invokes_operation_directly()
        {
            var retryPolicy = Mock.Of<IRetryPolicy>();
            var functionProvider = Mock.Of<IFunctionProvider>();
            var arg = new Arg();

            retryPolicy.Run(functionProvider.Func<Arg, Task>, arg);

            Mock.Get(functionProvider).Verify(x => x.Func<Arg, Task>(It.IsAny<Arg>()), Times.Never());
        }

        [TestMethod]
        public void RunT_invokes_Run_with_non_cancellation_token()
        {
            var retryPolicy = Mock.Of<IRetryPolicy>();

            retryPolicy.Run(arg => Task.CompletedTask, default(Arg));

            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.IsAny<Func<Arg, CancellationToken, Task>>(),
                    It.IsAny<Arg>(),
                    CancellationToken.None),
                Times.Once());
        }

        [TestMethod]
        public void RunT_invokes_Run_with_arg()
        {
            var retryPolicy = Mock.Of<IRetryPolicy>();
            var arg = new Arg();

            retryPolicy.Run(a => Task.CompletedTask, arg);

            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.IsAny<Func<Arg, CancellationToken, Task>>(),
                    arg,
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public void RunT_invokes_Run_with_wrapper_function()
        {
            Func<Arg, CancellationToken, Task> wrapper = null;
            var arg = new Arg();
            var retryPolicy = Mock.Of<IRetryPolicy>();
            Mock.Get(retryPolicy)
                .Setup(x => x.Run(It.IsAny<Func<Arg, CancellationToken, Task>>(), arg, CancellationToken.None))
                .Callback<Func<Arg, CancellationToken, Task>, Arg, CancellationToken>((f, a, t) => wrapper = f)
                .Returns(Task.CompletedTask);
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Arg, Task>, arg);

            wrapper.Should().NotBeNull();
            wrapper.Invoke(arg, CancellationToken.None);
            Mock.Get(functionProvider).Verify(x => x.Func<Arg, Task>(arg), Times.Once());
        }

        [TestMethod]
        public void RunTResult_does_not_invoke_operation_directly()
        {
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Task<Result>>);

            Mock.Get(functionProvider).Verify(x => x.Func<Task<Result>>(), Times.Never());
        }

        [TestMethod]
        public void RunTResult_invokes_Run_with_none_cancellation_token()
        {
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();

            retryPolicy.Run(() => Task.FromResult<Result>(default));

            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.IsAny<Func<CancellationToken, Task<Result>>>(),
                    CancellationToken.None),
                Times.Once());
        }

        [TestMethod]
        public void RunTResult_invokes_Run_with_wrapper_function()
        {
            Func<CancellationToken, Task<Result>> wrapper = null;
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();
            Mock.Get(retryPolicy)
                .Setup(x => x.Run(It.IsAny<Func<CancellationToken, Task<Result>>>(), CancellationToken.None))
                .Callback<Func<CancellationToken, Task<Result>>, CancellationToken>((f, t) => wrapper = f)
                .Returns(Task.FromResult<Result>(default));
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Task<Result>>);

            wrapper.Should().NotBeNull();
            wrapper.Invoke(CancellationToken.None);
            Mock.Get(functionProvider).Verify(x => x.Func<Task<Result>>(), Times.Once());
        }

        [TestMethod]
        public void RunTResultT_does_not_invokes_operation_directly()
        {
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();
            var arg = new Arg();
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Arg, Task<Result>>, arg);

            Mock.Get(functionProvider).Verify(x => x.Func<Arg, Task<Result>>(It.IsAny<Arg>()), Times.Never());
        }

        [TestMethod]
        public void RunTResultT_invokes_Run_with_none_cancellation_token()
        {
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();

            retryPolicy.Run(arg => Task.FromResult<Result>(default), default(Arg));

            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.IsAny<Func<Arg, CancellationToken, Task<Result>>>(),
                    It.IsAny<Arg>(),
                    CancellationToken.None),
                Times.Once());
        }

        [TestMethod]
        public void RunTResultT_invokes_Run_with_arg()
        {
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();
            var arg = new Arg();

            retryPolicy.Run(a => Task.FromResult<Result>(default), arg);

            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.IsAny<Func<Arg, CancellationToken, Task<Result>>>(),
                    arg,
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public void RunTResultT_invokes_Run_with_wrapper_function()
        {
            Func<Arg, CancellationToken, Task<Result>> wrapper = null;
            var arg = new Arg();
            var retryPolicy = Mock.Of<IRetryPolicy<Result>>();
            Mock.Get(retryPolicy)
                .Setup(x => x.Run(It.IsAny<Func<Arg, CancellationToken, Task<Result>>>(), arg, CancellationToken.None))
                .Callback<Func<Arg, CancellationToken, Task<Result>>, Arg, CancellationToken>((f, a, t) => wrapper = f)
                .Returns(Task.FromResult<Result>(default));
            var functionProvider = Mock.Of<IFunctionProvider>();

            retryPolicy.Run(functionProvider.Func<Arg, Task<Result>>, arg);

            wrapper.Should().NotBeNull();
            wrapper.Invoke(arg, CancellationToken.None);
            Mock.Get(functionProvider).Verify(x => x.Func<Arg, Task<Result>>(arg), Times.Once());
        }

        public interface IFunctionProvider
        {
            TResult Func<TResult>();

            TResult Func<T, TResult>(T arg);
        }

        public class Arg
        {
        }

        public class Result
        {
        }
    }
}
