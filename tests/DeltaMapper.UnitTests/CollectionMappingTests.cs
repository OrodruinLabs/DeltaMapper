using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class CollectionMappingTests
{
    // ── CL-01 ────────────────────────────────────────────────────────────────
    private class CL01_ClassroomProfile : Profile
    {
        public CL01_ClassroomProfile()
        {
            CreateMap<Student, StudentDto>();
            CreateMap<Classroom, ClassroomDto>();
        }
    }

    [Fact]
    public void Map_ListToList_MapsAllElements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL01_ClassroomProfile>());
        var mapper = config.CreateMapper();

        var source = new Classroom
        {
            Name = "Room A",
            Students = new List<Student>
            {
                new Student { Name = "Alice", Grade = 90 },
                new Student { Name = "Bob", Grade = 85 }
            }
        };

        var result = mapper.Map<Classroom, ClassroomDto>(source);

        result.Name.Should().Be("Room A");
        result.Students.Should().HaveCount(2);
        result.Students[0].Name.Should().Be("Alice");
        result.Students[0].Grade.Should().Be(90);
        result.Students[1].Name.Should().Be("Bob");
        result.Students[1].Grade.Should().Be(85);
    }

    // ── CL-02 ────────────────────────────────────────────────────────────────
    private class CL02_ClassroomArrayProfile : Profile
    {
        public CL02_ClassroomArrayProfile()
        {
            CreateMap<Student, StudentDto>();
            CreateMap<Classroom, ClassroomArrayDto>();
        }
    }

    [Fact]
    public void Map_ListToArray_MapsAllElements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL02_ClassroomArrayProfile>());
        var mapper = config.CreateMapper();

        var source = new Classroom
        {
            Name = "Room B",
            Students = new List<Student>
            {
                new Student { Name = "Carol", Grade = 95 },
                new Student { Name = "Dave", Grade = 78 }
            }
        };

        var result = mapper.Map<Classroom, ClassroomArrayDto>(source);

        result.Name.Should().Be("Room B");
        result.Students.Should().HaveCount(2);
        result.Students[0].Name.Should().Be("Carol");
        result.Students[0].Grade.Should().Be(95);
        result.Students[1].Name.Should().Be("Dave");
        result.Students[1].Grade.Should().Be(78);
    }

    // ── CL-03 ────────────────────────────────────────────────────────────────
    private class CL03_StudentProfile : Profile
    {
        public CL03_StudentProfile()
        {
            CreateMap<Student, StudentDto>();
        }
    }

    [Fact]
    public void Map_IEnumerableToList_MapsAllElements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL03_StudentProfile>());
        var mapper = config.CreateMapper();

        IEnumerable<Student> source = new List<Student>
        {
            new Student { Name = "Eve", Grade = 88 },
            new Student { Name = "Frank", Grade = 72 }
        };

        var result = mapper.Map<Student, StudentDto>(source);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Eve");
        result[0].Grade.Should().Be(88);
        result[1].Name.Should().Be("Frank");
        result[1].Grade.Should().Be(72);
    }

    // ── CL-04 ────────────────────────────────────────────────────────────────
    private class CL04_EmptyCollectionProfile : Profile
    {
        public CL04_EmptyCollectionProfile()
        {
            CreateMap<Student, StudentDto>();
            CreateMap<Classroom, ClassroomDto>();
        }
    }

    [Fact]
    public void Map_EmptyCollection_MapsToEmptyCollection()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL04_EmptyCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new Classroom
        {
            Name = "Empty Room",
            Students = new List<Student>()
        };

        var result = mapper.Map<Classroom, ClassroomDto>(source);

        result.Name.Should().Be("Empty Room");
        result.Students.Should().NotBeNull();
        result.Students.Should().BeEmpty();
    }

    // ── CL-05 ────────────────────────────────────────────────────────────────
    private class CL05_NullCollectionProfile : Profile
    {
        public CL05_NullCollectionProfile()
        {
            CreateMap<Student, StudentDto>();
            CreateMap<Classroom, ClassroomDto>();
        }
    }

    [Fact]
    public void Map_NullCollection_MapsToNull()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL05_NullCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new Classroom
        {
            Name = "Null Room",
            Students = null!
        };

        var result = mapper.Map<Classroom, ClassroomDto>(source);

        result.Name.Should().Be("Null Room");
        result.Students.Should().BeNull();
    }

    // ── CL-06 ────────────────────────────────────────────────────────────────
    private class CL06_MapListEmptyProfile : Profile
    {
        public CL06_MapListEmptyProfile()
        {
            CreateMap<Student, StudentDto>();
        }
    }

    [Fact]
    public void MapList_EmptySource_ReturnsEmptyList()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL06_MapListEmptyProfile>());
        var mapper = config.CreateMapper();

        var result = mapper.Map<Student, StudentDto>(Enumerable.Empty<Student>());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ── CL-07 ────────────────────────────────────────────────────────────────
    private class CL07_MapListSingleProfile : Profile
    {
        public CL07_MapListSingleProfile()
        {
            CreateMap<Student, StudentDto>();
        }
    }

    [Fact]
    public void MapList_SingleItem_ReturnsSingleElementList()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL07_MapListSingleProfile>());
        var mapper = config.CreateMapper();

        var source = new List<Student>
        {
            new Student { Name = "Grace", Grade = 100 }
        };

        var result = mapper.Map<Student, StudentDto>(source);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Grace");
        result[0].Grade.Should().Be(100);
    }

    // ── CL-08 ────────────────────────────────────────────────────────────────
    private class CL08_MapListMultipleProfile : Profile
    {
        public CL08_MapListMultipleProfile()
        {
            CreateMap<Student, StudentDto>();
        }
    }

    [Fact]
    public void MapList_MultipleItems_MapsAll()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CL08_MapListMultipleProfile>());
        var mapper = config.CreateMapper();

        var source = new List<Student>
        {
            new Student { Name = "Hank", Grade = 70 },
            new Student { Name = "Ivy", Grade = 80 },
            new Student { Name = "Jack", Grade = 90 }
        };

        var result = mapper.Map<Student, StudentDto>(source);

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Hank");
        result[0].Grade.Should().Be(70);
        result[1].Name.Should().Be("Ivy");
        result[1].Grade.Should().Be(80);
        result[2].Name.Should().Be("Jack");
        result[2].Grade.Should().Be(90);
    }
}
