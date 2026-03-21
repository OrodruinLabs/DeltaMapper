using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class CircularReferenceTests
{
    // ── CR-01 ─────────────────────────────────────────────────────────────────
    private class CR01_ParentChildProfile : Profile
    {
        public CR01_ParentChildProfile()
        {
            CreateMap<Parent, ParentDto>();
            CreateMap<Child, ChildDto>();
        }
    }

    [Fact]
    public void Map_DirectCircularReference_DoesNotStackOverflow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR01_ParentChildProfile>());
        var mapper = config.CreateMapper();

        var parent = new Parent { Name = "Alice" };
        var child = new Child { Name = "Bob", Parent = parent };
        parent.Child = child;

        FluentActions.Invoking(() => mapper.Map<Parent, ParentDto>(parent))
            .Should().NotThrow();
    }

    // ── CR-02 ─────────────────────────────────────────────────────────────────
    private class CR02_ParentChildProfile : Profile
    {
        public CR02_ParentChildProfile()
        {
            CreateMap<Parent, ParentDto>();
            CreateMap<Child, ChildDto>();
        }
    }

    [Fact]
    public void Map_CircularReference_ReturnsPreviouslyMappedInstance()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR02_ParentChildProfile>());
        var mapper = config.CreateMapper();

        var parent = new Parent { Name = "Alice" };
        var child = new Child { Name = "Bob", Parent = parent };
        parent.Child = child;

        var mappedParent = mapper.Map<Parent, ParentDto>(parent);

        mappedParent.Child.Should().NotBeNull();
        mappedParent.Child!.Parent.Should().BeSameAs(mappedParent);
    }

    // ── CR-03 ─────────────────────────────────────────────────────────────────
    private class CR03_TreeNodeProfile : Profile
    {
        public CR03_TreeNodeProfile()
        {
            CreateMap<TreeNode, TreeNodeDto>();
        }
    }

    [Fact]
    public void Map_SelfReferencing_DoesNotStackOverflow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR03_TreeNodeProfile>());
        var mapper = config.CreateMapper();

        var root = new TreeNode { Value = 1 };
        var left = new TreeNode { Value = 2, Left = root };
        root.Left = left;

        FluentActions.Invoking(() => mapper.Map<TreeNode, TreeNodeDto>(root))
            .Should().NotThrow();
    }

    // ── CR-04 ─────────────────────────────────────────────────────────────────
    private class CR04_ParentChildProfile : Profile
    {
        public CR04_ParentChildProfile()
        {
            CreateMap<Parent, ParentDto>();
            CreateMap<Child, ChildDto>();
        }
    }

    [Fact]
    public void Map_DeepCircularChain_A_B_C_A_Resolves()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR04_ParentChildProfile>());
        var mapper = config.CreateMapper();

        // Chain: parentA -> child1 -> parentA (back-reference closing the cycle)
        // child1 also has parentA as its Parent, and parentA has child1 as its Child
        var parentA = new Parent { Name = "ParentA" };
        var child1 = new Child { Name = "Child1" };
        var parentB = new Parent { Name = "ParentB" };
        var child2 = new Child { Name = "Child2", Parent = parentA };
        parentB.Child = child2;
        child1.Parent = parentB;
        parentA.Child = child1;

        FluentActions.Invoking(() => mapper.Map<Parent, ParentDto>(parentA))
            .Should().NotThrow();

        var result = mapper.Map<Parent, ParentDto>(parentA);

        result.Name.Should().Be("ParentA");
        result.Child.Should().NotBeNull();
        result.Child!.Name.Should().Be("Child1");
        result.Child.Parent.Should().NotBeNull();
        result.Child.Parent!.Name.Should().Be("ParentB");
    }
}
