namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System;
using System.Threading.Tasks;
using BuildingBlocks.Tests.Abstracts.Model;
using FluentAssertions;
using Xunit;

public class SnapshotCreationServiceShould : MSSQLTestBase
{
    private static string GetUniqueString(string prefix)
    {
        return prefix + "][" + Guid.NewGuid();
    }

    [Fact(DisplayName = "Get a snapshot for a specific version without snapshot supporting repository")]
    [Trait("Category", "Integrationtest")]
    public async Task Get_a_snapshot_for_a_specific_version_without_snapshot_supporting_repository()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), "Titel1", "Kommentar1", OrderState.Deactivated);

        var prefix = GetUniqueString("UniquePrefix");
        var code = "Code";
        order.SetOptionalCertificate(prefix, code);

        for (var i = 0; i < 10; i++)
        {
            order.ChangeTitle($"Title {i+1}");
            order.ChangeComment($"Comment {i+1}");
        }

        var repository = GetRepositoryWithoutSnapshotProvider();

        await repository.SaveAsync(order);

        var sut = GetSnapshotCreationServiceBasedOnANonSnapshotEnabledRepository();

        var snapshot = await sut.CreateSnapshotFrom(orderId.ToString(), 5);

        snapshot!.Version.Should()
            .Be(5);
    }

    [Fact(DisplayName = "Get a snapshot for a specific version with snapshot supporting repository")]
    [Trait("Category", "Integrationtest")]
    public async Task Get_a_snapshot_for_a_specific_version_with_snapshot_supporting_repository()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), "Titel1", "Kommentar1", OrderState.Deactivated);

        var prefix = GetUniqueString("UniquePrefix");
        var code = "Code";
        order.SetOptionalCertificate(prefix, code);

        for (var i = 0; i < 10; i++)
        {
            order.ChangeTitle($"Title {i+1}");
            order.ChangeComment($"Comment {i+1}");
        }

        var repository = GetRepositoryWithActivatedSnapshotProvider(2);

        await repository.SaveAsync(order);

        var sut = GetSnapshotCreationServiceBasedOnASnapshotEnabledRepository(2);

        var snapshot = await sut.CreateSnapshotFrom(orderId.ToString(), 5);

        snapshot!.Version.Should()
            .Be(5);
    }
}
