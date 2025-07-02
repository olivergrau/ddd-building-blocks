namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration;

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Core.Persistence.Repository;
using Service;
using Storage;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using Xunit;

public sealed class AggregateInformationServiceShould : IDisposable
{
    private readonly Order _order;

        private readonly EventSourcingRepository _eventSourcingRepository;

        private readonly AggregateInformationService _aggregateInformationService;

        private readonly Guid _identifier;

        private const string _defaultPrefix = "prefix";
        private const string _defaultCode = "code";

        public AggregateInformationServiceShould()
        {
            _identifier = Guid.NewGuid();

            //This path is used to save in memory storage
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data_" + _identifier);

            if (!Directory.Exists(strTempDataFolderPath))
            {
                Directory.CreateDirectory(strTempDataFolderPath);
            }

            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            var orderId = Guid.NewGuid();
            const string title = "Title A";
            const string comment = "Comment A";
            const OrderState orderState = OrderState.Deactivated;

            _order = new Order(orderId.ToString(), title, comment, orderState);
            _order.SetOptionalCertificate(_defaultPrefix, _defaultCode);

            _eventSourcingRepository = new EventSourcingRepository(new FileInMemoryEventStorageProvider(inMemoryEventStorePath),
                new InMemorySnapshotStorageProvider(5, inMemorySnapshotStorePath));

            _aggregateInformationService = new AggregateInformationService(inMemoryEventStorePath);
        }

        public void Dispose()
        {
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data_" + _identifier);
            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            File.Delete(inMemoryEventStorePath);
            File.Delete(inMemorySnapshotStorePath);
            Directory.Delete(strTempDataFolderPath, true);
        }

    [Fact(DisplayName = "Deliver the original type for a valid aggregateId")]
    [Trait("Category", "Integrationtest")]
    public async Task Deliver_the_original_type_for_a_valid_aggregateId()
    {
        // Act
        await _eventSourcingRepository.SaveAsync(_order);

        // Assert
        _order.GetUncommittedChanges().Should().HaveCount(0);

        var type = await _aggregateInformationService.GetTypeForAggregateId(_order.Id.ToString());

        type.Should()
            .NotBeNull();

        type.Should()
            .Be(_order.GetType());
    }
}
