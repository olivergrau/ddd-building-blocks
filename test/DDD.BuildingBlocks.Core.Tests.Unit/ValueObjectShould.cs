namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using FluentAssertions;
    using Domain;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using Xunit;

    public class SubValueObject(int number, string text) : ValueObject<SubValueObject>
    {
		public int Number { get; } = number;
        public string Text { get; } = text;

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
		{
			return new object[] { Number, Text };
		}
	}

    public class ValueObjectContainingValueObject(string string1, SubValueObject SubValueObject) : ValueObject<ValueObjectContainingValueObject>
    {
        public string String1 { get; } = string1;
        public SubValueObject SubValueObject { get; } = SubValueObject;


        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<object> {String1, SubValueObject};
        }
    }

	public class ValueObjectShould
	{
        [Fact(DisplayName = "Return true when compared with another nested value object but with the same values")]
        [Trait("Category", "Unittest")]
        public void Return_true_when_compared_with_another_nested_value_object_but_with_the_same_values()
        {
            Guid.NewGuid();

            var array1 = new[] { "a", "b" };
            var array2 = new[] { "a", "b" };

            (array1 == array2).Should().BeFalse();

            StructuralComparisons.StructuralEqualityComparer.Equals(array1, array2).Should().BeTrue();

            // Arrange
            var valueObject1 = new ValueObjectContainingValueObject("string", new SubValueObject(3, "a"));
            var valueObject2 = new ValueObjectContainingValueObject("string", new SubValueObject(3, "a"));

			// Act
			var result = valueObject1 == valueObject2;

            // Assert
            result.Should().BeTrue();
        }

		[Fact(DisplayName = "Return false when compared with value object with different values")]
		[Trait("Category", "Unittest")]
		public void Return_false_when_compared_with_value_object_with_different_values()
		{
			// Arrange
			var valueObject1 = new Certificate("prefix1", "code");
			var valueObject2 = new Certificate("prefix2", "code2");

			// Act
			var result = valueObject1.Equals(valueObject2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact(DisplayName = "Return false when compared with null")]
		[Trait("Category", "Unittest")]
		public void Return_false_when_compared_with_null()
		{
			// Arrange
			var valueObject = new Certificate("prefix1", "code");

			// Act
			var result = valueObject.Equals();

			// Assert
			result.Should().BeFalse();
		}

		[Fact(DisplayName = "Return true when compared with another instance but with the same values")]
		[Trait("Category", "Unittest")]
		public void Return_true_when_compared_with_another_instance_but_with_the_same_values()
		{
			// Arrange
			var valueObject1 = new Certificate("prefix1", "code1");
			var valueObject2 = new Certificate("prefix1", "code1");

			// Act
			var result = valueObject1.Equals(valueObject2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact(DisplayName = "Return true when compared with another instance but with the same values")]
		[Trait("Category", "Unittest")]
		public void Return_false_when_compared_with_different_value_object()
		{
			// Arrange
			var valueObject1 = new Certificate("prefix1", "code");
			var valueObject2 = (object)(new Certificate("prefix2", "code2"));

			// Act
			var result = valueObject1.Equals(valueObject2);

			// Assert
			result.Should().BeFalse();
		}

		[Theory(DisplayName = "Return different hash code as a different value object")]
		[Trait("Category", "Unittest")]
		[InlineData("prefix", "code", "prefix1", "code")]
		[InlineData("prefix", "code", "prefix", "code1")]
		public void Return_different_hash_code_as_a_different_value_object(string prefix1, string code1, string prefix2, string code2)
		{
			// Arrange
			var valueObject1 = new Certificate(prefix1, code1);
			var valueObject2 = new Certificate(prefix2, code2);

			// Act
			var result = valueObject1.GetHashCode() != valueObject2.GetHashCode();

			// Assert
			result.Should().BeTrue();
		}

		[Fact(DisplayName = "Return same hash code for two instances with same values")]
		[Trait("Category", "Unittest")]
		public void Return_same_hash_code_for_two_instances_with_same_values()
		{
			// Arrange
			var valueObject1 = new Certificate("prefix", "code");
			var valueObject2 = new Certificate("prefix", "code");

			// Act
			var result = valueObject1.GetHashCode() == valueObject2.GetHashCode();

			// Assert
			result.Should().BeTrue();
		}

		[Theory(DisplayName = "Return false when compared with different value object under usage of equality operator")]
		[Trait("Category", "Unittest")]
		[InlineData("prefix", "code", "prefix1", "code")]
		[InlineData("prefix", "code", "prefix", "code1")]
		public void Return_false_when_compared_with_different_valueObject_under_usage_of_equality_operator(string prefix1, string code1, string prefix2, string code2)
		{
			// Arrange
			var valueObject1 = new Certificate(prefix1, code1);
			var valueObject2 = new Certificate(prefix2, code2);

			// Act
			var result = valueObject1 == valueObject2;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		[Trait("Category", "Unittest")]
		public void Return_true_when_compared_with_different_valueObject_but_same_values_under_usage_of_equality_operator()
		{
			// Arrange
			var valueObject1 = new Certificate("prefix", "code");
			var valueObject2 = new Certificate("prefix", "code");

			// Act
			var result = valueObject1 == valueObject2;

			// Assert
			result.Should().BeTrue();
		}
	}
}
