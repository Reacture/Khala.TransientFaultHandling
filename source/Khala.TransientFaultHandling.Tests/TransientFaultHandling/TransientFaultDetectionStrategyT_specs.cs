namespace Khala.TransientFaultHandling
{
    using System.Reflection;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TransientFaultDetectionStrategyT_specs
    {
        [TestMethod]
        public void sut_inherits_TransientFaultDetectionStrategy()
        {
            typeof(TransientFaultDetectionStrategy<>).BaseType
                .Should().Be(typeof(TransientFaultDetectionStrategy));
        }

        [TestMethod]
        public void sut_has_IsTransientResult_method()
        {
            var tut = typeof(TransientFaultDetectionStrategy<Result>);
            const string MethodName = "IsTransientResult";
            tut.Should().HaveMethod(MethodName, new[] { typeof(Result) });
            MethodInfo mut = tut.GetMethod(MethodName);
            mut.ReturnType.Should().Be(typeof(bool));
            mut.Should().BeVirtual();
        }

        [TestMethod]
        public void IsTransientResult_returns_false()
        {
            var fixture = new Fixture();
            var result = fixture.Create<Result>();
            var sut = new TransientFaultDetectionStrategy<Result>();

            bool actual = sut.IsTransientResult(result);

            actual.Should().BeFalse();
        }

        public class Result
        {
        }
    }
}
