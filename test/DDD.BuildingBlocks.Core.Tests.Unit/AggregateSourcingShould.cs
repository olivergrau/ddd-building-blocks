
// ReSharper disable MemberCanBePrivate.Global

namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Commanding;
    using Domain;
    using Persistence;
    using Persistence.Repository;
    using Xunit;

    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class AggregateSourcingShould
    {
        private readonly IEventSourcingRepository _repository;
        private readonly AggregateSourcing _sourcing;

        public AggregateSourcingShould()
        {
            _repository = new RepositoryMock();
            _sourcing = new AggregateSourcing(_repository);
        }

        public class TestAggregateCommand(string serializedAggregateId, int targetVersion) : Command(serializedAggregateId, targetVersion);

        public class RepositoryMock : IEventSourcingRepository
        {
#pragma warning disable CA1051
            public readonly List<TestAggregate> Aggregates = [];
#pragma warning restore CA1051

            public Task<object?> GetByIdAsync(string id, Type type, int version = -1)
            {
                throw new NotImplementedException();
                //return Task.FromResult(Aggregates.SingleOrDefault(q => Equals(q.Id, id)));
            }

            public Task<T?> GetByIdAsync<T, TKey>(TKey id) where T : AggregateRoot<TKey> where TKey : EntityId<TKey>
            {
                return Task.FromResult(Aggregates.SingleOrDefault(q => Equals(q.Id, id)) as T);
            }

            public Task SaveAsync(IEventSourcingBasedAggregate aggregate)
            {
                throw new NotImplementedException();
            }
        }

#pragma warning disable CA1034
        public class TestAggregateId : EntityId<TestAggregateId>
#pragma warning restore CA1034
        {
            public Guid Id { get; }

            public TestAggregateId(string serializedId)
            {
                Id = Guid.Parse(serializedId);
            }

            public TestAggregateId(Guid id)
            {
                Id = id;
            }

#pragma warning disable CS0659
            public override bool Equals(object? other)
#pragma warning restore CS0659
            {
                if (other == null || GetType() != other.GetType())
                {
                    return false;
                }

                return Id == ((TestAggregateId) other).Id;
            }

            protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
            {
                return new List<object> { Id };
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return Id.ToString();
            }
        }

#pragma warning disable CA1034
        public class TestAggregate : AggregateRoot<TestAggregateId>
#pragma warning restore CA1034
        {
            public TestAggregate() : base(null!)
            {

            }

            public TestAggregate(TestAggregateId id) : base(id)
            {
            }

            protected override EntityId<TestAggregateId> GetIdFromStringRepresentation(string value)
            {
                return new TestAggregateId(Guid.ParseExact(value, "D"));
            }
        }

        [Fact(DisplayName = "Work correctly")]
        [Trait("Category", "Unittest")]
        public void Work_correctly()
        {
            // arrange
            var id = new TestAggregateId(Guid.NewGuid());
            var command = new TestAggregateCommand(id.ToString(), -1);

            ((RepositoryMock)_repository).Aggregates.Add(new TestAggregate(id));

            TestAggregate? aggregate = null;

            Func<Task> functor = async () => { aggregate = await _sourcing.Source<TestAggregate, TestAggregateId>(command); };

            // act && assert
            functor.Should().NotThrowAsync();

            aggregate!.Id.Should().Be(id);
        }
    }
}
