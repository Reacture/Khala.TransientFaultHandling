namespace Khala.TransientFaultHandling.Testing
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TransientFaultHandlingSpy_specs
    {
        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo p in typeof(TransientFaultHandlingSpy).GetProperties())
            {
                p.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void Policy_returns_RetryPolicy_instance()
        {
            var sut = new TransientFaultHandlingSpy();
            RetryPolicy actual = sut.Policy;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public void OperationNonCancellable_returns_Func_Task_instance()
        {
            var sut = new TransientFaultHandlingSpy();
            Func<Task> actual = sut.OperationNonCancellable;
            actual.Should().NotBeNull();
        }

        [TestMethod]
        public async Task given_OperationNonCancellable_invoked_by_Policy_Verify_succeeds()
        {
            var sut = new TransientFaultHandlingSpy();
            await sut.Policy.Run(sut.OperationNonCancellable);

            Action action = sut.Verify;

            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_OperationNonCancellable_not_invoked_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            Action action = sut.Verify;
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public async Task given_OperationNonCancellable_invoked_directly_Verify_throws_InvalidOperationException()
        {
            var sut = new TransientFaultHandlingSpy();
            try
            {
                await sut.OperationNonCancellable.Invoke();
            }
            catch
            {
            }

            Action action = sut.Verify;

            action.ShouldThrow<InvalidOperationException>();
        }
    }
}
