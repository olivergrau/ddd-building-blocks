// ReSharper disable DisposeOnUsingVariable
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Util;
    using Xunit;

	public class CorrelatedScopeShould
	{
        private static async Task AsyncFunction()
        {
            var innerGuid = Guid.NewGuid();

            using var inner = new CorrelatedScope(innerGuid.ToString());

            await Task.Delay(1000);

            CorrelatedScope.Current.Should()
                .Be(innerGuid.ToString());

            inner.Dispose();
        }

        [Fact(DisplayName = "Work correctly in a nested async environment")]
        [Trait("Category", "Unittest")]
        public async Task Work_correctly_in_nested_async_environment()
        {
            var outerGuid = Guid.NewGuid();

            using var outer = new CorrelatedScope(outerGuid.ToString());

            await AsyncFunction();

            CorrelatedScope.Current.Should()
                .Be(outerGuid.ToString());
        }

        [Fact(DisplayName = "Work correctly in a nested single thread environment")]
        [Trait("Category", "Unittest")]
        public void Work_correctly_in_nested_single_thread_environment()
        {
            var outerGuid = Guid.NewGuid();

            using var outer = new CorrelatedScope(outerGuid.ToString());
            using var inner = new CorrelatedScope();

            CorrelatedScope.Current.Should()
                .NotBe(outerGuid.ToString());

            inner.Dispose();

            CorrelatedScope.Current.Should()
                .Be(outerGuid.ToString());
        }

        [Fact(DisplayName = "Work correctly with a complex type")]
        [Trait("Category", "Unittest")]
        public void Work_correctly_with_a_complex_type()
        {
            using var scope = new AmbientContext<ScopeValues>(new ScopeValues
            {
                Number = 1,
                Name = "One"
            });

            using var innerScope = new AmbientContext<ScopeValues>(new ScopeValues
            {
                Number = 2,
                Name = "Two"
            });

            AmbientContext<ScopeValues>.Current.Should()
                .BeEquivalentTo(new ScopeValues
                {
                    Number = 2,
                    Name = "Two"
                });

            innerScope.Dispose();

            AmbientContext<ScopeValues>.Current.Should()
                .BeEquivalentTo(new ScopeValues
                {
                    Number = 1,
                    Name = "One"
                });
        }

        private sealed class ScopeValues
        {
            public string Name { get; set; }

            public int Number { get; set; }
        }
    }
}
