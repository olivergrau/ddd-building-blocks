
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8604

#pragma warning disable 1998

namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Event;
    using Xunit;

    [Collection("Collection 1")]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Performance", "CA1802:Use literals where appropriate")]
    public class DomainEventNotifierShould
    {
	    #region test plumbing

#pragma warning disable CA1034
        public class TestSubscriberThatThrowsException : ISubscribe<ProductCreatedEvent>
#pragma warning restore CA1034
        {
            public async Task WhenAsync(ProductCreatedEvent @event)
            {
                throw new Exception("Planned Exception");
            }
        }

        public class TestSubscriberAlpha : ISubscribe<ProductCreatedEvent>, ISubscribe<CategoryCreatedEvent>
        {
            public TestSubscriberAlpha()
            {
                ResetProperties();
            }

            public void ResetProperties()
            {
                WhenAsyncProductCreatedCalls = 0;
                WhenAsyncCategoryCreatedCalls = 0;
            }

            public int WhenAsyncProductCreatedCalls { get; private set; }
            public int WhenAsyncCategoryCreatedCalls { get; private set; }

            public async Task WhenAsync(ProductCreatedEvent @event)
            {
                WhenAsyncProductCreatedCalls++;
            }

            public async Task WhenAsync(CategoryCreatedEvent @event)
            {
                WhenAsyncCategoryCreatedCalls++;
            }
        }

        public class TestSubscriberBeta : ISubscribe<ProductCreatedEvent>
        {
            public TestSubscriberBeta()
            {
                ResetProperties();
            }

            public void ResetProperties()
            {
                WhenAsyncCalls = 0;
            }

            public int WhenAsyncCalls { get; private set; }


            public async Task WhenAsync(ProductCreatedEvent @event)
            {
                WhenAsyncCalls++;
            }
        }

        public class ProductCreatedEvent(
            string serializedAggregateId,
            int version,
            string name,
            string description,
            int state,
            DateTime createdTime
        ) : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
        {
            private static readonly int _currentTypeVersion = 1;

            public string Name { get; } = name;
            public string Description { get; } = description;
            public DateTime CreatedTime { get; } = createdTime;
            public int State { get; } = state;
        }

        public class CategoryCreatedEvent(string serializedAggregateId, int version, string name, string description, DateTime createdTime)
            : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
        {
            private static readonly int _currentTypeVersion = 1;

            public string Name { get; } = name;
            public string Description { get; } = description;
            public DateTime CreatedTime { get; } = createdTime;
        }

        private readonly TestSubscriberAlpha _subscriberAlpha = new TestSubscriberAlpha();
        private readonly TestSubscriberBeta _subscriberBeta = new TestSubscriberBeta();

        private readonly TestSubscriberThatThrowsException _subscriberThatThrowsException =
            new TestSubscriberThatThrowsException();

        #endregion

        [Fact(DisplayName = "Notify subscribers correctly")]
        [Trait("Category", "Unittest")]
        public async Task Notify_subscribers_correctly()
        {
            // Arrange
            ResetSubscribers();
            var resolver = CreateTestResolver();
            var eventNotifier = new DomainEventNotifier("DDD.BuildingBlocks.Core.Tests.Unit");
            eventNotifier.SetDependencyResolver(resolver);

            // Act
            var @event = new ProductCreatedEvent(Guid.NewGuid().ToString(), 1, "Name", "Description", 2, DateTime.UtcNow);
            await eventNotifier.NotifyAsync(@event);

            // Assert
            _subscriberAlpha.WhenAsyncProductCreatedCalls.Should().Be(1);
            _subscriberBeta.WhenAsyncCalls.Should().Be(1);
        }

        [Fact]
        [Trait("Category", "Unittest")]
        public async Task Only_notify_matching_subscribers()
        {
            // Arrange
            ResetSubscribers();
            var resolver = CreateTestResolver();
            var eventNotifier = new DomainEventNotifier("DDD.BuildingBlocks.Core.Tests.Unit");
            eventNotifier.SetDependencyResolver(resolver);

            // Act
            var @event = new CategoryCreatedEvent(Guid.NewGuid().ToString(), 1, "Name", "Description", DateTime.UtcNow);
            await eventNotifier.NotifyAsync(@event);

            // Assert
            _subscriberAlpha.WhenAsyncCategoryCreatedCalls.Should().Be(1);
            _subscriberBeta.WhenAsyncCalls.Should().Be(0);
        }

        [Fact(DisplayName = "Log error when exception occurs in subscriber method")]
        [Trait("Category", "Unittest")]
        public async Task Log_error_when_exception_occurs_in_subscriber_method()
        {
            // Arrange
            var resolver = CreateTestResolverForExceptionTest();
            var eventNotifier = new DomainEventNotifier("DDD.BuildingBlocks.Core.Tests.Unit");
            eventNotifier.SetDependencyResolver(resolver);

            Exception error = null!;

            // Act
            var @event = new ProductCreatedEvent(Guid.NewGuid().ToString(), 1, "Name", "Description", 2, DateTime.UtcNow);

            eventNotifier.OnSubscriberException +=
                (_, args) => error = ((DomainEventNotifierSubscriberExceptionEventArgs) args).Exception;

            await eventNotifier.NotifyAsync(@event);

            // Assert
            error.Should().NotBeNull();
        }

        [Fact(DisplayName = "Not throw exception if no subscribers are found")]
        [Trait("Category", "Unittest")]
        public void Not_throw_exception_if_no_subscribers_are_found()
        {
            // Arrange
            var searchPatternThatYieldsNoResults = "Unknown.Unknown.Unknown";
            var eventNotifier = new DomainEventNotifier(searchPatternThatYieldsNoResults);

            // Act + Assert
            var @event = new ProductCreatedEvent(Guid.NewGuid().ToString(), 1, "Name", "Description", 2, DateTime.UtcNow);
            Func<Task> functor = async () => await eventNotifier.NotifyAsync(@event);
            functor.Should().NotThrowAsync<Exception>();
        }

        private void ResetSubscribers()
        {
            _subscriberAlpha.ResetProperties();
            _subscriberBeta.ResetProperties();
        }

        private TestDependencyResolver CreateTestResolver()
        {
            var resolver = new TestDependencyResolver();
            resolver.AddObject(_subscriberAlpha.GetType().FullName, _subscriberAlpha);
            resolver.AddObject(_subscriberBeta.GetType().FullName, _subscriberBeta);
            return resolver;
        }

        private TestDependencyResolver CreateTestResolverForExceptionTest()
        {
            var resolver = new TestDependencyResolver();
            resolver.AddObject(_subscriberThatThrowsException.GetType().FullName, _subscriberThatThrowsException);
            return resolver;
        }
    }
}
