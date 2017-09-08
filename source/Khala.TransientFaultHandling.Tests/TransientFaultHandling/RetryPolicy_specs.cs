namespace Khala.TransientFaultHandling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class RetryPolicy_specs
    {
        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var sut = typeof(RetryPolicy);
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(fixture).Verify(sut);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-10)]
        public void constructor_has_guard_clause_against_negative_maximumRetryCount(int maximumRetryCount)
        {
            TransientFaultDetectionStrategy transientFaultDetectionStrategy =
                new DelegatingTransientFaultDetectionStrategy(exception => true);
            RetryIntervalStrategy retryIntervalStrategy =
                new DelegatingRetryIntervalStrategy(
                    retried => TimeSpan.FromMilliseconds(1),
                    immediateFirstRetry: false);

            Action action = () => new RetryPolicy(maximumRetryCount, transientFaultDetectionStrategy, retryIntervalStrategy);

            action.ShouldThrow<ArgumentOutOfRangeException>().Where(x => x.ParamName == "maximumRetryCount");
        }

        [TestMethod]
        public void Run_is_virtual()
        {
            typeof(RetryPolicy).GetMethod("Run").Should().BeVirtual();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        public async Task Run_invokes_operation_at_least_once(int maximumRetryCount)
        {
            // Arrange
            TransientFaultDetectionStrategy transientFaultDetectionStrategy =
                new DelegatingTransientFaultDetectionStrategy(exception => true);

            RetryIntervalStrategy retryIntervalStrategy =
                new DelegatingRetryIntervalStrategy(
                    retried => TimeSpan.FromMilliseconds(1),
                    immediateFirstRetry: false);

            var sut = new RetryPolicy(
                maximumRetryCount,
                transientFaultDetectionStrategy,
                retryIntervalStrategy);

            var cancellationToken = default(CancellationToken);
            var functionProvider = Mock.Of<IFunctionProvider>(
                x => x.Func<CancellationToken, Task>(cancellationToken) == Task.FromResult(true));
            Func<CancellationToken, Task> operation = functionProvider.Func<CancellationToken, Task>;

            // Act
            await sut.Run(operation, cancellationToken);

            // Assert
            Mock.Get(functionProvider).Verify(x => x.Func<CancellationToken, Task>(cancellationToken), Times.Once());
        }

        [TestMethod]
        public async Task Run_invokes_operation_repeatedly_until_succeeds()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var failTimes = generator.First(x => x > 0);
            var maximumRetryCount = failTimes + generator.First(x => x > 0);
            var oper = new EventualSuccessOperator(Enumerable.Repeat(new Exception(), failTimes));
            var sut = new RetryPolicy(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero, true));

            // Act
            await sut.Run(oper.Operation, CancellationToken.None);

            // Assert
            oper.Laps.Count.Should().Be(failTimes + 1);
        }

        [TestMethod]
        public void Run_throws_exception_if_retry_count_reaches_maximumRetryCount()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x > 0);
            var failTimes = maximumRetryCount + generator.First(x => x > 0);
            Exception[] exceptions = Enumerable.Range(0, failTimes).Select(_ => new Exception()).ToArray();
            var oper = new EventualSuccessOperator(exceptions);
            var sut = new RetryPolicy(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero, true));

            // Act
            Func<Task> action = () => sut.Run(oper.Operation, CancellationToken.None);

            // Assert
            action.ShouldThrow<Exception>().Which.Should().BeSameAs(exceptions[maximumRetryCount]);
            oper.Laps.Count.Should().Be(maximumRetryCount + 1);
        }

        [TestMethod]
        public void Run_throws_exception_immediately_if_not_transient()
        {
            // Arrange
            var exception = new Exception();
            var maximumRetryCount = 2;
            var oper = new EventualSuccessOperator(new[] { exception });
            var sut = new RetryPolicy(
                maximumRetryCount,
                new DelegatingTransientFaultDetectionStrategy(x => x != exception),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero, true));

            // Act
            Func<Task> action = () => sut.Run(oper.Operation, CancellationToken.None);

            // Assert
            action.ShouldThrow<Exception>().Which.Should().BeSameAs(exception);
            oper.Laps.Count.Should().Be(1);
        }

        [TestMethod]
        public async Task Run_delays_before_retry()
        {
            // Arrange
            var generator = new Generator<int>(new Fixture());
            var maximumRetryCount = generator.First(x => x < 10);
            var exceptions = Enumerable.Repeat(new Exception(), maximumRetryCount);
            TimeSpan[] delays = Enumerable
                .Range(0, maximumRetryCount)
                .Select(_ => TimeSpan.FromMilliseconds(new Fixture().Create<int>()))
                .ToArray();
            var spy = new EventualSuccessOperator(exceptions);
            var sut = new RetryPolicy(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new DelegatingRetryIntervalStrategy(t => delays[t], false));

            // Act
            await sut.Run(spy.Operation, CancellationToken.None);

            // Assert
            for (int i = 0; i < spy.Laps.Count - 1; i++)
            {
                TimeSpan actual = spy.Laps[i + 1] - spy.Laps[i];
                TimeSpan expected = delays[i];
                actual.Should().BeGreaterOrEqualTo(expected);
                actual.Should().BeCloseTo(expected, precision: 20);
            }
        }

        public class EventualSuccessOperator
        {
            private readonly List<DateTime> _laps;
            private readonly Queue<Exception> _exceptions;

            public EventualSuccessOperator(IEnumerable<Exception> exceptions)
            {
                _laps = new List<DateTime>();
                _exceptions = new Queue<Exception>(exceptions);
            }

            public IReadOnlyList<DateTime> Laps => _laps;

            public Task Operation(CancellationToken cancellationToken)
            {
                var now = DateTime.Now;

                try
                {
                    if (_exceptions.Any())
                    {
                        throw _exceptions.Dequeue();
                    }

                    return Task.FromResult(true);
                }
                finally
                {
                    _laps.Add(now);
                }
            }
        }
    }
}
