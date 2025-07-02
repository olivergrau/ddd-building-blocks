namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Commanding;
    using Xunit;

    #pragma warning disable CA1863 // Use cached composite format

    [Collection("Collection 1")]
    public class DefaultCommandProcessorShould
    {
        // ReSharper disable once NotAccessedField.Local

	    private sealed class TestCommand(string serializedAggregateId, int targetVersion) : Command(serializedAggregateId, targetVersion);

        private sealed class TestCommandHandler : ICommandHandler<TestCommand>
        {
            public Task HandleCommandAsync(TestCommand command)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FailingTestCommandHandler : ICommandHandler<TestCommand>
        {
            public Task HandleCommandAsync(TestCommand command)
            {
                throw new InvalidOperationException();
            }
        }

        [Fact(DisplayName = "Not execute command if no command handler was registered before")]
        [Trait("Category", "Unittest")]
		public async Task Not_execute_command_if_no_command_handler_was_registered_before()
        {
			// Arrange
            var target = new DefaultCommandProcessor();

			// Act
            var result = await target.ExecuteAsync(new TestCommand(Guid.NewGuid().ToString(), 0));

			// Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            var expectedFailReason = string.Format(CultureInfo.InvariantCulture, CoreErrors.NoCommandHandlerFound, typeof(DefaultCommandProcessorShould))+"+TestCommand";
            result.FailReason.Should().Be(expectedFailReason);
            result.ResultException.Should().NotBeNull();
        }

		[Fact(DisplayName = "Catch exception if commandHandler throws exception and commandHandler was registered with factory")]
        [Trait("Category", "Unittest")]
		public async Task Catch_exception_if_commandHandler_throws_exception_and_command_handler_was_registered_with_factory()
        {
			// Arrange
            var target = new DefaultCommandProcessor();
            target.RegisterHandlerFactory(() => new FailingTestCommandHandler());

            var errorSet = false;
            target.OnError += (_, _) => errorSet = true;

			// Act
            var result = await target.ExecuteAsync(new TestCommand( Guid.NewGuid().ToString(), 0));

			// Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.FailReason.Should().NotBeNullOrWhiteSpace();
            var expectedFailReason = string.Format(CultureInfo.InvariantCulture, CoreErrors.CommandExecutionFailed, typeof(DefaultCommandProcessor));
            result.FailReason.Should().Be(expectedFailReason);

            errorSet.Should()
                .BeTrue();
        }

        [Fact(DisplayName = "Catch exception if command handler throws exception and command handler was registered as instance")]
        [Trait("Category", "Unittest")]
		public async Task Catch_exception_if_commandHandler_throws_exception_and_command_handler_was_registered_as_instance()
        {
			// Arrange
            var target = new DefaultCommandProcessor();
            target.RegisterHandlerInstance(new FailingTestCommandHandler());

			// Act
            var result = await target.ExecuteAsync(new TestCommand( Guid.NewGuid().ToString(), 0));

			// Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.FailReason.Should().NotBeNullOrWhiteSpace();
            var expectedFailReason = string.Format(CultureInfo.InvariantCulture, CoreErrors.CommandExecutionFailed, typeof(DefaultCommandProcessor));
            result.FailReason.Should().Be(expectedFailReason);
        }

		[Fact(DisplayName = "Execute a command with registered handler instance and returns success status and no fail reason")]
        [Trait("Category", "Unittest")]
		public async Task Execute_a_command_with_registered_handler_instance_and_returns_success_status_and_no_fail_reason()
        {
			// Arrange
            var target = new DefaultCommandProcessor();
            target.RegisterHandlerInstance(new TestCommandHandler());

			// Act
            var result = await target.ExecuteAsync(new TestCommand(Guid.NewGuid().ToString(), 0));

			// assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.FailReason.Should().BeNullOrWhiteSpace();
            result.ResultException.Should().BeNull();
        }

        [Fact(DisplayName = "Execute a command with registered handler factory and returns success status and no fail reason")]
        [Trait("Category", "Unittest")]
		public async Task Execute_a_command_with_registered_handler_factory_and_returns_success_status_and_no_fail_reason()
        {
			// Arrange
            var target = new DefaultCommandProcessor();
            target.RegisterHandlerFactory(() => new TestCommandHandler());

			// Act
            var result = await target.ExecuteAsync(new TestCommand(Guid.NewGuid().ToString(), 0));

			// Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.FailReason.Should().BeNullOrWhiteSpace();
            result.ResultException.Should().BeNull();
        }

        [Fact(DisplayName = "Correctly pre execute code if provided when instantiate it")]
        [Trait("Category", "Unittest")]
		public async Task Correctly_pre_execute_code_if_provided_when_instantiate_it()
        {
			// Arrange
            var preExecution = false;
            var target = new DefaultCommandProcessor(new List<Action<ICommand>>
            {
                _ => { preExecution = true; }
            });

            target.RegisterHandlerFactory(() => new TestCommandHandler());

			// Act
            var result = await target.ExecuteAsync(new TestCommand(Guid.NewGuid().ToString(), 0));

			// Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.FailReason.Should().BeNullOrWhiteSpace();
            result.ResultException.Should().BeNull();
            preExecution.Should().BeTrue();
        }
    }
}
