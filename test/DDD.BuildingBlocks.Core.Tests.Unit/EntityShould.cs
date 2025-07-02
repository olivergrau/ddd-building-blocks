namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using FluentAssertions;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using Xunit;

    public class EntityShould
	{
		[Fact(DisplayName = "Return false when compared with different entity instance")]
		[Trait("Category", "Unittest")]
		public void Return_false_when_compared_with_different_entity_instance()
		{
			// Arrange
			var entity1 = new Order(Guid.NewGuid().ToString(), "Titel", "Kommentar", OrderState.Open);
			var entity2 = new Order(Guid.NewGuid().ToString(), "Titel", "Kommentar", OrderState.Open);

			// Act
			var result = entity1.Equals(entity2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact(DisplayName = "Returns false when compared with null")]
		[Trait("Category", "Unittest")]
		public void Returns_false_when_compared_with_null()
		{
			// Arrange
			var entity1 = new Order(Guid.NewGuid().ToString(), "Titel", "Kommentar", OrderState.Open);

			// Act
#pragma warning disable CA1508
            var result = entity1.Equals(null);
#pragma warning restore CA1508

            // Assert
			result.Should().BeFalse();
		}

		[Fact(DisplayName = "Return true when compared with entity of same id")]
		[Trait("Category", "Unittest")]
		public void Return_true_when_compared_with_entity_of_same_id()
		{
			// Arrange
			var id = Guid.NewGuid();
			var entity1 = new Order(id.ToString(), "Titel", "Kommentar", OrderState.Open);
			var entity2 = new Order(id.ToString(), "Titel", "Kommentar", OrderState.Open);

			// Act
			var result = entity1.Equals(entity2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact(DisplayName = "Return different hash codes when compared with non equal entity instance")]
		[Trait("Category", "Unittest")]
		public void Return_different_hashcodes_when_compared_with_non_equal_entity_instance()
		{
			// Arrange
			var entity1 = new Order(Guid.NewGuid().ToString(), "Titel", "Kommentar", OrderState.Open);
			var entity2 = new Order(Guid.NewGuid().ToString(), "Titel", "Kommentar", OrderState.Open);

			// Act
			var result = entity1.GetHashCode() != entity2.GetHashCode();

			// Assert
			result.Should().BeTrue();
		}

		[Fact(DisplayName = "Return different hash codes when compared with equal entity instance")]
		[Trait("Category", "Unittest")]
		public void Return_different_hashcodes_when_compared_with_equal_entity_instance()
		{
			// Arrange
			var id = Guid.NewGuid();
			var entity1 = new Order(id.ToString(), "Titel", "Kommentar", OrderState.Open);
			var entity2 = new Order(id.ToString(), "Titel", "Kommentar", OrderState.Open);

			// Act
			var result = entity1.GetHashCode() == entity2.GetHashCode();

			// Assert
			result.Should().BeTrue();
		}
    }
}
