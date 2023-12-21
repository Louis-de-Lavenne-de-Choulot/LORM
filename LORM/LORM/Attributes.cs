[AttributeUsage(AttributeTargets.Property)]
public class ColumnNameAttribute : Attribute
{
    public string Name { get; }

    public ColumnNameAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class TableNameAttribute : Attribute
{
    public string Name { get; }

    public TableNameAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class NullableAttribute : Attribute
{
    public bool IsNullable { get; }

    public NullableAttribute(bool isNullable)
    {
        IsNullable = isNullable;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class StringLengthAttribute : Attribute
{
    public int MaxLength { get; }

    public StringLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }
}