namespace Khala.TransientFaultHandling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class RetryPolicyT_specs
    {
        public interface IFunctionProvider
        {
            TResult Func<TResult>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(RetryPolicy<>));
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

            action.ShouldThrow<ArgumentOutOfRangeException>().Where(x => x.ParamName == "maximumRetryCount");
        }

        [TestMethod]
        public void Run_is_virtual()
        {
            typeof(RetryPolicy<>).GetMethod("Run").Should().BeVirtual();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        public async Task Run_invokes_operation_at_least_once(int maximumRetryCount)
        {
            // Arrange
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    exception => false,
                    result => false),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            var expected = new Result();
            var functionProvider = Mock.Of<IFunctionProvider>(
                x => x.Func<Task<Result>>() == Task.FromResult(expected));
            Func<Task<Result>> operation = functionProvider.Func<Task<Result>>;

            // Act
            Result actual = await sut.Run(operation);

            // Assert
            actual.Should().BeSameAs(expected);
            Mock.Get(functionProvider).Verify(x => x.Func<Task<Result>>(), Times.Once());
        }

        [TestMethod]
        public async Task Run_invokes_operation_repeatedly_until_succeeds()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var failTimes = generator.First(x => x > 0);
            var maximumRetryCount = failTimes + generator.First(x => x > 0);
            var result = new Result();
            var oper = new EventualSuccessOperatorWithTransientException(Enumerable.Repeat(new Exception(), failTimes), result);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x != result),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            // Act
            Result actual = await sut.Run(oper.Operation);

            // Assert
            oper.InvocationCount.Should().Be(failTimes + 1);
            actual.Should().BeSameAs(result);
        }

        [TestMethod]
        public async Task Run_invokes_operation_repeatedly_until_returns_non_transient_result()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var failTimes = generator.First(x => x > 0);
            var maximumRetryCount = failTimes + generator.First(x => x > 0);
            var result = new Result();
            var oper = new EventualSuccessOperatorWithTransientResult(Enumerable.Repeat(new TransientResult(), failTimes), result);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            // Act
            Result actual = await sut.Run(oper.Operation);

            // Assert
            oper.InvocationCount.Should().Be(failTimes + 1);
            actual.Should().BeSameAs(result);
        }

        [TestMethod]
        public void Run_throws_exception_if_retry_count_reaches_maximumRetryCount()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var failTimes = maximumRetryCount + generator.First(x => x > 0);
            Exception[] exceptions = Enumerable.Range(0, failTimes).Select(_ => new Exception()).ToArray();
            var result = new Result();
            var oper = new EventualSuccessOperatorWithTransientException(exceptions, result);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x != result),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            // Act
            Func<Task> action = () => sut.Run(oper.Operation);

            // Assert
            action.ShouldThrow<Exception>().Which.Should().BeSameAs(exceptions[maximumRetryCount]);
            oper.InvocationCount.Should().Be(maximumRetryCount + 1);
        }

        [TestMethod]
        public async Task Run_returns_transient_result_if_retry_count_reaches_maximumRetryCount()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var failTimes = maximumRetryCount + generator.First(x => x > 0);
            TransientResult[] transientResults = Enumerable.Range(0, failTimes).Select(_ => new TransientResult()).ToArray();
            var result = new Result();
            var oper = new EventualSuccessOperatorWithTransientResult(transientResults, result);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            // Act
            Result actual = await sut.Run(oper.Operation);

            // Assert
            actual.Should().BeSameAs(transientResults[maximumRetryCount]);
            oper.InvocationCount.Should().Be(maximumRetryCount + 1);
        }

        [TestMethod]
        public void Run_throws_exception_immediately_if_not_transient()
        {
            // Arrange
            var exception = new Exception();
            var maximumRetryCount = 2;
            var result = new Result();
            var oper = new EventualSuccessOperatorWithTransientException(new[] { exception }, result);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(
                    x => x != exception,
                    x => x is TransientResult),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));

            // Act
            Func<Task> action = () => sut.Run(oper.Operation);

            // Assert
            action.ShouldThrow<Exception>().Which.Should().BeSameAs(exception);
            oper.InvocationCount.Should().Be(1);
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
            var spy = new OperationSpy(transientCount: maximumRetryCount - 1);
            var sut = new RetryPolicy<Result>(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy<Result>(x => true, x => x is TransientResult),
                new DelegatingRetryIntervalStrategy(t => delays[t], false));

            // Act
            await sut.Run(spy.Operation);

            // Assert
            for (int i = 0; i < spy.Laps.Count - 1; i++)
            {
                TimeSpan actual = spy.Laps[i + 1] - spy.Laps[i];
                TimeSpan expected = delays[i];
                actual.Should().BeGreaterOrEqualTo(expected);
                actual.Should().BeCloseTo(expected, precision: 20);
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

        public class EventualSuccessOperatorWithTransientException
        {
            private readonly Queue<Exception> _exceptions;
            private readonly Result _result;
            private int _invocationCount;

            public EventualSuccessOperatorWithTransientException(IEnumerable<Exception> exceptions, Result result)
            {
                _exceptions = new Queue<Exception>(exceptions);
                _result = result;
                _invocationCount = 0;
            }

            public int InvocationCount => _invocationCount;

            public Task<Result> Operation()
            {
                try
                {
                    if (_exceptions.Any())
                    {
                        throw _exceptions.Dequeue();
                    }
                }
                finally
                {
                    _invocationCount++;
                }

                return Task.FromResult(_result);
            }
        }

        public class EventualSuccessOperatorWithTransientResult
        {
            private readonly Queue<TransientResult> _transientResults;
            private readonly Result _result;
            private int _invocationCount;

            public EventualSuccessOperatorWithTransientResult(IEnumerable<TransientResult> transientResults, Result result)
            {
                _transientResults = new Queue<TransientResult>(transientResults);
                _result = result;
                _invocationCount = 0;
            }

            public int InvocationCount => _invocationCount;

            public Task<Result> Operation()
            {
                try
                {
                    if (_transientResults.Any())
                    {
                        return Task.FromResult<Result>(_transientResults.Dequeue());
                    }
                }
                finally
                {
                    _invocationCount++;
                }

                return Task.FromResult(_result);
            }
        }

        public class OperationSpy
        {
            private readonly List<DateTime> _laps;
            private readonly int _transientCount;

            public OperationSpy(int transientCount)
            {
                _laps = new List<DateTime>();
                _transientCount = transientCount;
            }

            public IReadOnlyList<DateTime> Laps => _laps;

            public Task<Result> Operation()
            {
                var now = DateTime.Now;

                try
                {
                    if (_laps.Count < _transientCount)
                    {
                        if (now.Millisecond % 2 == 0)
                        {
                            throw new InvalidOperationException();
                        }
                        else
                        {
                            return Task.FromResult<Result>(new TransientResult());
                        }
                    }

                    return Task.FromResult(new Result());
                }
                finally
                {
                    _laps.Add(now);
                }
            }
        }
    }
}
