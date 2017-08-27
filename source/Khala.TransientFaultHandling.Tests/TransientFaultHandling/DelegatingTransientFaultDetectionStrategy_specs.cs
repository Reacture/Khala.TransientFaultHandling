namespace Khala.TransientFaultHandling
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class DelegatingTransientFaultDetectionStrategy_specs
    {
        public interface IFunctionProvider
        {
            bool Func(Exception exception);
        }

        [TestMethod]
        public void sut_inherits_TransientFaultDetectionStrategy()
        {
            typeof(DelegatingTransientFaultDetectionStrategy)
                .BaseType.Should().Be(typeof(TransientFaultDetectionStrategy));
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var tut = typeof(DelegatingTransientFaultDetectionStrategy);
            new GuardClauseAssertion(new Fixture()).Verify(tut);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsTransientException_relays_to_function(bool expected)
        {
            var fixture = new Fixture();
            var exception = fixture.Create<Exception>();
            var functionProvider = Mock.Of<IFunctionProvider>(
                x => x.Func(exception) == expected);
            Func<Exception, bool> func = functionProvider.Func;
            var sut = new DelegatingTransientFaultDetectionStrategy(func);

            bool actual = sut.IsTransientException(exception);

            actual.Should().Be(expected);
        }
    }
}
