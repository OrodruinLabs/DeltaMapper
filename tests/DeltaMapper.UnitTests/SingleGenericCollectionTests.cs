using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class SingleGenericCollectionTests
{
    private class Student { public string Name { get; set; } = ""; public int Age { get; set; } }
    private class StudentDto { public string Name { get; set; } = ""; public int Age { get; set; } }

    private class TestProfile : Profile
    {
        public TestProfile()
        {
            CreateMap<Student, StudentDto>();
        }
    }

    private static Abstractions.IMapper CreateMapper() =>
        MapperConfiguration.Create(cfg => cfg.AddProfile<TestProfile>()).CreateMapper();

    [Fact]
    public void Map_IEnumerable_Dest_from_list_source_maps_collection()
    {
        var mapper = CreateMapper();
        var students = new List<Student>
        {
            new() { Name = "Alice", Age = 20 },
            new() { Name = "Bob", Age = 22 }
        };

        var result = mapper.Map<IEnumerable<StudentDto>>(students);

        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Alice");
        result.Last().Name.Should().Be("Bob");
    }

    [Fact]
    public void Map_List_Dest_from_list_source_maps_collection()
    {
        var mapper = CreateMapper();
        var students = new List<Student> { new() { Name = "Charlie", Age = 21 } };

        var result = mapper.Map<List<StudentDto>>(students);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Charlie");
    }

    [Fact]
    public void Map_Array_Dest_from_array_source_maps_collection()
    {
        var mapper = CreateMapper();
        var students = new[] { new Student { Name = "Dave", Age = 23 } };

        var result = mapper.Map<StudentDto[]>(students);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Dave");
    }

    [Fact]
    public void Map_IReadOnlyList_Dest_from_list_source_maps_collection()
    {
        var mapper = CreateMapper();
        var students = new List<Student> { new() { Name = "Eve", Age = 24 } };

        var result = mapper.Map<IReadOnlyList<StudentDto>>(students);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Eve");
    }

    [Fact]
    public void Map_collection_preserves_source_ordering()
    {
        var mapper = CreateMapper();
        var students = new List<Student>
        {
            new() { Name = "Zara", Age = 1 },
            new() { Name = "Alice", Age = 2 },
            new() { Name = "Mia", Age = 3 }
        };

        var result = mapper.Map<List<StudentDto>>(students);

        result.Select(s => s.Name).Should().ContainInOrder("Zara", "Alice", "Mia");
    }

    [Fact]
    public void Map_empty_collection_returns_empty()
    {
        var mapper = CreateMapper();
        var result = mapper.Map<List<StudentDto>>(new List<Student>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void Map_collection_null_source_throws()
    {
        var mapper = CreateMapper();
        var act = () => mapper.Map<List<StudentDto>>((object)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_collection_no_registered_map_falls_through_to_error()
    {
        var mapper = CreateMapper();
        var students = new List<Student> { new() { Name = "test" } };

        // No map from Student → string, so collection detection skips and normal error fires
        var act = () => mapper.Map<List<string>>(students);
        act.Should().Throw<DeltaMapper.Exceptions.DeltaMapperException>();
    }
}
