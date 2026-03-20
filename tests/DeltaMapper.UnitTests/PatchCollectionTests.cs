using DeltaMapper.Configuration;
using DeltaMapper.Diff;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class PatchCollectionTests
{
    // PC-01: Item added — after list is longer than before
    [Fact]
    public void Patch_CollectionItemAdded_EmitsAddedChange()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<TeamProfile>());
        var mapper = config.CreateMapper();

        var source = new Team
        {
            Name = "Alpha",
            Players =
            [
                new Player { Name = "Alice", Score = 10 },
                new Player { Name = "Bob", Score = 20 },
                new Player { Name = "Charlie", Score = 30 },
            ]
        };
        var destination = new TeamDto
        {
            Name = "Alpha",
            Players =
            [
                new PlayerDto { Name = "Alice", Score = 10 },
                new PlayerDto { Name = "Bob", Score = 20 },
            ]
        };

        var diff = mapper.Patch<Team, TeamDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().Contain(c => c.PropertyName == "Players[2]" && c.Kind == ChangeKind.Added);
    }

    // PC-02: Item removed — after list is shorter than before
    [Fact]
    public void Patch_CollectionItemRemoved_EmitsRemovedChange()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<TeamProfile>());
        var mapper = config.CreateMapper();

        var source = new Team
        {
            Name = "Alpha",
            Players = [new Player { Name = "Alice", Score = 10 }]
        };
        var destination = new TeamDto
        {
            Name = "Alpha",
            Players =
            [
                new PlayerDto { Name = "Alice", Score = 10 },
                new PlayerDto { Name = "Bob", Score = 20 },
            ]
        };

        var diff = mapper.Patch<Team, TeamDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().Contain(c => c.PropertyName == "Players[1]" && c.Kind == ChangeKind.Removed);
    }

    // PC-03: Item modified — same index, different property values
    [Fact]
    public void Patch_CollectionItemModified_EmitsModifiedWithDotNotation()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<TeamProfile>());
        var mapper = config.CreateMapper();

        var source = new Team
        {
            Name = "Alpha",
            Players = [new Player { Name = "Alice", Score = 99 }]
        };
        var destination = new TeamDto
        {
            Name = "Alpha",
            Players = [new PlayerDto { Name = "Alice", Score = 10 }]
        };

        var diff = mapper.Patch<Team, TeamDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().Contain(c =>
            c.PropertyName == "Players[0].Score"
            && Equals(c.From, 10)
            && Equals(c.To, 99)
            && c.Kind == ChangeKind.Modified);
    }

    private class TeamProfile : Profile
    {
        public TeamProfile()
        {
            CreateMap<Player, PlayerDto>();
            CreateMap<Team, TeamDto>();
        }
    }
}
