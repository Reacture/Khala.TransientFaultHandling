namespace Khala.TransientFaultHandling
{
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.TransientFaultHandling.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultHandlingExtensions_specs
    {
        public interface IFunctionProvider
        {
            void Action<TArg>(TArg arg);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(TransientFaultHandlingExtensions));
        }

        [TestMethod]
        public async Task Run_relays_to_retryPolicy_with_none_cancellation_token()
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingActionSpy(functionProvider.Action);

            await spy.Policy.Run(spy.Operation);

            spy.Verify();
            Mock.Get(functionProvider).Verify(x => x.Action(CancellationToken.None));
        }

        [TestMethod]
        public async Task RunT_relays_to_retryPolicy_with_non_cancellation_token()
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingFuncSpy<Result>(functionProvider.Action);

            await spy.Policy.Run(spy.Operation);

            spy.Verify();
            Mock.Get(functionProvider).Verify(x => x.Action(CancellationToken.None));
        }

        public class Result
        {
        }
    }
}
