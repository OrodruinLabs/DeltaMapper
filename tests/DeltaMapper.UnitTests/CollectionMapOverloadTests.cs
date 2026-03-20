using Xunit;

namespace DeltaMapper.UnitTests;

public class CollectionMapOverloadTests
{
    private class Student { public string Name { get; set; } = ""; public int Age { get; set; } }
    private class StudentDto { public string Name { get; set; } = ""; public int Age { get; set; } }

    private class TestProfile : Profile
    {
        public TestProfile() { CreateMap<Student, StudentDto>(); }
    }

    private static IMapper CreateMapper() =>
        MapperConfiguration.Create(cfg => cfg.AddProfile<TestProfile>()).CreateMapper();

    [Fact]
    public void Map_list_returns_List_of_destinations()
    {
        var mapper = CreateMapper();
        var students = new List<Student>
        {
            new() { Name = "Alice", Age = 20 },
            new() { Name = "Bob", Age = 22 }
        };

        List<StudentDto> result = mapper.Map<Student, StudentDto>(students);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public void Map_array_returns_List_of_destinations()
    {
        var mapper = CreateMapper();
        var students = new[] { new Student { Name = "Charlie", Age = 21 } };

        List<StudentDto> result = mapper.Map<Student, StudentDto>(students);

        Assert.Single(result);
        Assert.Equal("Charlie", result[0].Name);
    }

    [Fact]
    public void Map_empty_collection_returns_empty_list()
    {
        var mapper = CreateMapper();
        List<StudentDto> result = mapper.Map<Student, StudentDto>(Array.Empty<Student>());
        Assert.Empty(result);
    }

    [Fact]
    public void Map_null_collection_throws_ArgumentNullException()
    {
        var mapper = CreateMapper();
        Assert.Throws<ArgumentNullException>(() =>
            mapper.Map<Student, StudentDto>((IEnumerable<Student>)null!));
    }
}
