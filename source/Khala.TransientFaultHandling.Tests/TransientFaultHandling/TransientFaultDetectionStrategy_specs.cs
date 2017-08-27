namespace Khala.TransientFaultHandling
{
    using System;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultDetectionStrategy_specs
    {
        [TestMethod]
        public void sut_has_IsTransientException_method()
        {
            Type tut = typeof(TransientFaultDetectionStrategy);
            const string MethodName = "IsTransientException";
            tut.Should().HaveMethod(MethodName, new[] { typeof(Exception) });
            MethodInfo mut = tut.GetMethod(MethodName);
            mut.ReturnType.Should().Be(typeof(bool), "return type of the method should be bool");
            mut.Should().BeVirtual();
        }

        [TestMethod]
        public void IsTransientException_returns_true()
        {
            var sut = new TransientFaultDetectionStrategy();
            var fixture = new Fixture();
            var exception = fixture.Create<Exception>();

            bool actual = sut.IsTransientException(exception);

            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTransientException_has_guard_clause()
        {
            MethodInfo mut = typeof(TransientFaultDetectionStrategy).GetMethod("IsTransientException");
            new GuardClauseAssertion(new Fixture()).Verify(mut);
        }
    }
}
