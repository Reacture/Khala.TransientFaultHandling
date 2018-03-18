namespace Khala.TransientFaultHandling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class RetryPolicyT_specs
    {
        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);

            TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(RetryPolicy<>));
        }

        [TestMethod]
        public void sut_implements_IRetryPolicy()
        {
            typeof(RetryPolicy<Result>).Should().Implement<IRetryPolicy>();
        }

        [TestMethod]
        public void sut_implements_IRetryPolicy_TResult()
        {
            typeof(RetryPolicy<Result>).Should().Implement<IRetryPolicy<Result>>();
        }

        [TestMethod]
        public void constructor_sets_properties_correctly()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var maximumRetryCount = fixture.Create<int>();
            var transientFaultDetectionStrategy = fixture.Create<TransientDefaultDetectionStrategy<Result>>();
            var retryIntervalStrategy = fixture.Create<RetryIntervalStrategy>();

            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                transientFaultDetectionStrategy,
                retryIntervalStrategy);

            sut.MaximumRetryCount.Should().Be(maximumRetryCount);
            sut.TransientFaultDetectionStrategy.Should().BeSameAs(transientFaultDetectionStrategy);
            sut.RetryIntervalStrategy.Should().BeSameAs(retryIntervalStrategy);
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo property in typeof(RetryPolicy<>).GetProperties())
            {
                property.Should().NotBeWritable();
            }
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-10)]
        public void constructor_has_guard_clause_against_negative_maximumRetryCount(int maximumRetryCount)
        {
            TransientFaultDetectionStrategy<Result> transientFaultDetectionStrategy =
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    exception => true,
                    result => true);
            RetryIntervalStrategy retryIntervalStrategy =
                new DelegatingRetryIntervalStrategy(
                    retried => TimeSpan.FromMilliseconds(1),
                    immediateFirstRetry: false);

            Action action = () => new RetryPolicy<Result>(maximumRetryCount, transientFaultDetectionStrategy, retryIntervalStrategy);

            action.Should().Throw<ArgumentOutOfRangeException>().Where(x => x.ParamName == "maximumRetryCount");
        }

        [TestMethod]
        [DataRow(0, true)]
        [DataRow(1, true)]
        [DataRow(10, true)]
        [DataRow(0, false)]
        [DataRow(1, false)]
        [DataRow(10, false)]
        public async Task Run_invokes_operation_at_least_once(int maximumRetryCount, bool canceled)
        {
            // Arrange
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    exception => false,
                    result => false),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var cancellationToken = new CancellationToken(canceled);

            var expected = new Result();
            var functionProvider = Mock.Of<IFunctionProvider>(
                x => x.Func<CancellationToken, Task<Result>>(cancellationToken) == Task.FromResult(expected));

            // Act
            Result actual = await sut.Run(functionProvider.Func<CancellationToken, Task<Result>>, cancellationToken);

            // Assert
            actual.Should().BeSameAs(expected);
            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(cancellationToken), Times.AtLeastOnce());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Run_invokes_operation_repeatedly_until_succeeds(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var transientFaultCount = generator.First(x => x > 0);
            var maximumRetryCount = transientFaultCount + generator.First(x => x > 0);
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                () => new Exception());
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x != result),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var cancellationToken = new CancellationToken(canceled);

            // Act
            Result actual = await sut.Run(oper.Operation, cancellationToken);

            // Assert
            actual.Should().BeSameAs(result);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(cancellationToken), Times.Exactly(transientFaultCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Run_invokes_operation_repeatedly_until_returns_non_transient_result(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var transientFaultCount = generator.First(x => x > 0);
            var maximumRetryCount = transientFaultCount + generator.First(x => x > 0);
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                () => new TransientResult());
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var cancellationToken = new CancellationToken(canceled);

            // Act
            Result actual = await sut.Run(oper.Operation, cancellationToken);

            // Assert
            actual.Should().BeSameAs(result);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(cancellationToken), Times.Exactly(transientFaultCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Run_throws_exception_if_retry_count_reaches_maximumRetryCount(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var transientFaultCount = maximumRetryCount + generator.First(x => x > 0);
            Exception[] exceptions = Enumerable.Range(0, transientFaultCount).Select(_ => new Exception()).ToArray();
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                triedTimes => exceptions[triedTimes]);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x != result),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var cancellationToken = new CancellationToken(canceled);

            // Act
            Func<Task> action = () => sut.Run(oper.Operation, cancellationToken);

            // Assert
            action.Should().Throw<Exception>().Which.Should().BeSameAs(exceptions[maximumRetryCount]);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(cancellationToken), Times.Exactly(maximumRetryCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Run_returns_transient_result_if_retry_count_reaches_maximumRetryCount(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var transientFaultCount = maximumRetryCount + generator.First(x => x > 0);
            TransientResult[] transientResults = Enumerable.Range(0, transientFaultCount).Select(_ => new TransientResult()).ToArray();
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                triedTimes => transientResults[triedTimes]);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var cancellationToken = new CancellationToken(canceled);

            // Act
            Result actual = await sut.Run(oper.Operation, cancellationToken);

            // Assert
            actual.Should().BeSameAs(transientResults[maximumRetryCount]);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(cancellationToken), Times.Exactly(maximumRetryCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Run_throws_exception_immediately_if_not_transient(bool canceled)
        {
            // Arrange
            var exception = new Exception();
            var maximumRetryCount = 2;
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount: 1,
                transientExceptionFactory: () => exception);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    x => x != exception,
                    x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));
            var cancellationToken = new CancellationToken(canceled);

            // Act
            Func<Task> action = () => sut.Run(oper.Operation, cancellationToken);

            // Assert
            action.Should().Throw<Exception>().Which.Should().BeSameAs(exception);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<CancellationToken, Task<Result>>(cancellationToken), Times.Once());
        }

        [TestMethod]
        public async Task Run_delays_before_retry()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x < 10);
            TimeSpan[] delays = Enumerable
                .Range(0, maximumRetryCount)
                .Select(_ => TimeSpan.FromMilliseconds(generator.First()))
                .ToArray();
            var spy = new EventualSuccessOperator<Result>(
                new Result(),
                transientFaultCount: maximumRetryCount);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new DelegatingRetryIntervalStrategy(t => delays[t], false));

            // Act
            await sut.Run(spy.Operation, CancellationToken.None);

            // Assert
            for (int i = 0; i < spy.Invocations.Count - 1; i++)
            {
                TimeSpan actual = spy.Invocations[i + 1] - spy.Invocations[i];
                TimeSpan expected = delays[i];
                actual.Should().BeGreaterOrEqualTo(expected);
                int precision =
#if DEBUG
                    100;
#else
                    20;
#endif
                actual.Should().BeCloseTo(expected, precision);
            }
        }

        [TestMethod]
        [DataRow(0, true)]
        [DataRow(1, true)]
        [DataRow(10, true)]
        [DataRow(0, false)]
        [DataRow(1, false)]
        [DataRow(10, false)]
        public async Task RunT_invokes_operation_at_least_once(int maximumRetryCount, bool canceled)
        {
            // Arrange
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    exception => false,
                    result => false),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            var expected = new Result();
            var functionProvider = Mock.Of<IFunctionProvider>(
                x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken) == Task.FromResult(expected));

            // Act
            Result actual = await sut.Run(functionProvider.Func<Arg, CancellationToken, Task<Result>>, arg, cancellationToken);

            // Assert
            actual.Should().BeSameAs(expected);
            Mock.Get(functionProvider).Verify(x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken), Times.AtLeastOnce());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task RunT_invokes_operation_repeatedly_until_succeeds(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var transientFaultCount = generator.First(x => x > 0);
            var maximumRetryCount = transientFaultCount + generator.First(x => x > 0);
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                () => new Exception());
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x != result),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            // Act
            Result actual = await sut.Run(oper.Operation, arg, cancellationToken);

            // Assert
            actual.Should().BeSameAs(result);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken), Times.Exactly(transientFaultCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task RunT_invokes_operation_repeatedly_until_returns_non_transient_result(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var transientFaultCount = generator.First(x => x > 0);
            var maximumRetryCount = transientFaultCount + generator.First(x => x > 0);
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                () => new TransientResult());
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            // Act
            Result actual = await sut.Run(oper.Operation, arg, cancellationToken);

            // Assert
            actual.Should().BeSameAs(result);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken), Times.Exactly(transientFaultCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RunT_throws_exception_if_retry_count_reaches_maximumRetryCount(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var transientFaultCount = maximumRetryCount + generator.First(x => x > 0);
            Exception[] exceptions = Enumerable.Range(0, transientFaultCount).Select(_ => new Exception()).ToArray();
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                triedTimes => exceptions[triedTimes]);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x != result),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            // Act
            Func<Task> action = () => sut.Run(oper.Operation, arg, cancellationToken);

            // Assert
            action.Should().Throw<Exception>().Which.Should().BeSameAs(exceptions[maximumRetryCount]);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken), Times.Exactly(maximumRetryCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task RunT_returns_transient_result_if_retry_count_reaches_maximumRetryCount(bool canceled)
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var transientFaultCount = maximumRetryCount + generator.First(x => x > 0);
            TransientResult[] transientResults = Enumerable.Range(0, transientFaultCount).Select(_ => new TransientResult()).ToArray();
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount,
                triedTimes => transientResults[triedTimes]);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            // Act
            Result actual = await sut.Run(oper.Operation, arg, cancellationToken);

            // Assert
            actual.Should().BeSameAs(transientResults[maximumRetryCount]);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken), Times.Exactly(maximumRetryCount + 1));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RunT_throws_exception_immediately_if_not_transient(bool canceled)
        {
            // Arrange
            var exception = new Exception();
            var maximumRetryCount = 2;
            var result = new Result();
            var oper = new EventualSuccessOperator<Result>(
                result,
                transientFaultCount: 1,
                transientExceptionFactory: () => exception);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    x => x != exception,
                    x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));
            var arg = new Arg();
            var cancellationToken = new CancellationToken(canceled);

            // Act
            Func<Task> action = () => sut.Run(oper.Operation, arg, cancellationToken);

            // Assert
            action.Should().Throw<Exception>().Which.Should().BeSameAs(exception);
            Mock.Get(oper.FunctionProvider).Verify(x => x.Func<Arg, CancellationToken, Task<Result>>(arg, cancellationToken), Times.Once());
        }

        [TestMethod]
        public async Task RunT_delays_before_retry()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x < 10);
            TimeSpan[] delays = Enumerable
                .Range(0, maximumRetryCount)
                .Select(_ => TimeSpan.FromMilliseconds(generator.First()))
                .ToArray();
            var spy = new EventualSuccessOperator<Result>(
                new Result(),
                transientFaultCount: maximumRetryCount);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new DelegatingRetryIntervalStrategy(t => delays[t], false));

            // Act
            await sut.Run(spy.Operation, new Arg(), CancellationToken.None);

            // Assert
            for (int i = 0; i < spy.Invocations.Count - 1; i++)
            {
                TimeSpan actual = spy.Invocations[i + 1] - spy.Invocations[i];
                TimeSpan expected = delays[i];
                actual.Should().BeGreaterOrEqualTo(expected);
                int precision =
#if DEBUG
                    100;
#else
                    20;
#endif
                actual.Should().BeCloseTo(expected, precision);
            }
        }

        [TestMethod]
        public void LinearTransientDefault_assembles_policy_correctly()
        {
            var fixture = new Fixture();
            var maximumRetryCount = fixture.Create<int>();
            var increment = fixture.Create<TimeSpan>();

            var actual = RetryPolicy<Result>.LinearTransientDefault(maximumRetryCount, increment);

            actual.Should().NotBeNull();
            actual.MaximumRetryCount.Should().Be(maximumRetryCount);
            actual.TransientFaultDetectionStrategy.Should().BeOfType<TransientDefaultDetectionStrategy<Result>>();
            actual.RetryIntervalStrategy.Should().Match<LinearRetryIntervalStrategy>(
                x =>
                x.MaximumInterval == TimeSpan.MaxValue &&
                x.InitialInterval == TimeSpan.Zero &&
                x.Increment == increment &&
                x.ImmediateFirstRetry == false);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ConstantTransientDefault_assembles_policy_correctly(bool immediateFirstRetry)
        {
            var fixture = new Fixture();
            var maximumRetryCount = fixture.Create<int>();
            var interval = fixture.Create<TimeSpan>();

            var actual = RetryPolicy<Result>.ConstantTransientDefault(maximumRetryCount, interval, immediateFirstRetry);

            actual.Should().NotBeNull();
            actual.MaximumRetryCount.Should().Be(maximumRetryCount);
            actual.TransientFaultDetectionStrategy.Should().BeOfType<TransientDefaultDetectionStrategy<Result>>();
            actual.RetryIntervalStrategy.Should().Match<ConstantRetryIntervalStrategy>(
                x =>
                x.Interval == interval &&
                x.ImmediateFirstRetry == immediateFirstRetry);
        }

        public class Result
        {
        }

        public class TransientResult : Result
        {
        }

        public class Arg
        {
        }

        public class EventualSuccessOperator<TResult>
        {
            private readonly TResult _result;
            private readonly int _transientFaultCount;
            private readonly Func<int, object> _transientFaultFactory;
            private readonly List<DateTime> _invocations;
            private readonly IFunctionProvider _functionProvider;

            public EventualSuccessOperator(TResult result, int transientFaultCount, Func<int, object> transientFaultFactory)
            {
                _result = result;
                _transientFaultCount = transientFaultCount;
                _transientFaultFactory = transientFaultFactory;
                _invocations = new List<DateTime>();
                _functionProvider = Mock.Of<IFunctionProvider>();
            }

            public EventualSuccessOperator(TResult result, int transientFaultCount, Func<object> transientExceptionFactory)
                : this(result, transientFaultCount, triedTimes => transientExceptionFactory.Invoke())
            {
            }

            public EventualSuccessOperator(TResult result, int transientFaultCount)
                : this(result, transientFaultCount, triedTimes => new Exception())
            {
            }

            public IReadOnlyList<DateTime> Invocations => _invocations;

            public IFunctionProvider FunctionProvider => _functionProvider;

            public async Task<TResult> Operation(CancellationToken cancellationToken)
            {
                var now = DateTime.Now;

                try
                {
                    await _functionProvider.Func<CancellationToken, Task<TResult>>(cancellationToken);

                    int triedTimes = _invocations.Count;
                    if (triedTimes < _transientFaultCount)
                    {
                        switch (_transientFaultFactory.Invoke(triedTimes))
                        {
                            case Exception transientExcepption:
                                throw transientExcepption;

                            case TResult transientResult:
                                return transientResult;
                        }
                    }

                    return _result;
                }
                finally
                {
                    _invocations.Add(now);
                }
            }

            public async Task<TResult> Operation<T>(T arg, CancellationToken cancellationToken)
            {
                var now = DateTime.Now;

                try
                {
                    await _functionProvider.Func<T, CancellationToken, Task<TResult>>(arg, cancellationToken);

                    int triedTimes = _invocations.Count;
                    if (triedTimes < _transientFaultCount)
                    {
                        switch (_transientFaultFactory.Invoke(triedTimes))
                        {
                            case Exception transientExcepption:
                                throw transientExcepption;

                            case TResult transientResult:
                                return transientResult;
                        }
                    }

                    return _result;
                }
                finally
                {
                    _invocations.Add(now);
                }
            }
        }
    }
}
