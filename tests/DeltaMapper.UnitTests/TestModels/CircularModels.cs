namespace DeltaMapper.UnitTests.TestModels;

public class Parent
{
    public string Name { get; set; } = string.Empty;
    public Child? Child { get; set; }
}

public class Child
{
    public string Name { get; set; } = string.Empty;
    public Parent? Parent { get; set; }
}

public class ParentDto
{
    public string Name { get; set; } = string.Empty;
    public ChildDto? Child { get; set; }
}

public class ChildDto
{
    public string Name { get; set; } = string.Empty;
    public ParentDto? Parent { get; set; }
}

public class TreeNode
{
    public int Value { get; set; }
    public TreeNode? Left { get; set; }
    public TreeNode? Right { get; set; }
}

public class TreeNodeDto
{
    public int Value { get; set; }
    public TreeNodeDto? Left { get; set; }
    public TreeNodeDto? Right { get; set; }
}
