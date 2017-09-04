namespace Khala.TransientFaultHandling
{
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;

    [TestClass]
    public class TransientDefaultDetectionStrategyT_specs
    {
        [TestMethod]
        public void sut_inherits_TransientNullDetectionStrategyT()
        {
            typeof(TransientDefaultDetectionStrategy<>)
                .BaseType.GetGenericTypeDefinition()
                .Should().Be(typeof(TransientFaultDetectionStrategy<>));
        }

        [TestMethod]
        public void given_default_struct_result_IsTransientResult_returns_true()
        {
            var result = default(StructResult);
            var sut = new TransientDefaultDetectionStrategy<StructResult>();

            bool actual = sut.IsTransientResult(result);

            actual.Should().BeTrue();
        }

        [TestMethod]
        public void given_non_default_struct_result_IsTransientResult_returns_false()
        {
            var fixture = new Fixture();
            var result = fixture.Create<StructResult>();
            var sut = new TransientDefaultDetectionStrategy<StructResult>();

            bool actual = sut.IsTransientResult(result);

            actual.Should().BeFalse();
        }

        [TestMethod]
        public void given_default_class_result_IsTransientResult_returns_true()
        {
            var result = default(ClassResult);
            var sut = new TransientDefaultDetectionStrategy<ClassResult>();

            bool actual = sut.IsTransientResult(result);

            actual.Should().BeTrue();
        }

        [TestMethod]
        public void given_non_default_class_result_IsTransientResult_returns_false()
        {
            var result = new ClassResult();
            var sut = new TransientDefaultDetectionStrategy<ClassResult>();

            bool actual = sut.IsTransientResult(result);

            actual.Should().BeFalse();
        }

        public struct StructResult
        {
            public StructResult(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        public class ClassResult
        {
        }
    }
}
