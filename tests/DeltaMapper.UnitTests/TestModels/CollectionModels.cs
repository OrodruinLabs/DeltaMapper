namespace DeltaMapper.UnitTests.TestModels;

public class Student
{
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; }
}

public class StudentDto
{
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; }
}

public class Classroom
{
    public string Name { get; set; } = string.Empty;
    public List<Student> Students { get; set; } = new();
}

public class ClassroomDto
{
    public string Name { get; set; } = string.Empty;
    public List<StudentDto> Students { get; set; } = new();
}

// For array destination tests
public class ClassroomArrayDto
{
    public string Name { get; set; } = string.Empty;
    public StudentDto[] Students { get; set; } = Array.Empty<StudentDto>();
}
