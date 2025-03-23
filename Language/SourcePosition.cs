namespace Inu.Language;

public readonly struct SourcePosition(string fileName, int lineNumber)
{
    public readonly string FileName = fileName;
    public readonly int LineNumber = lineNumber;

    public override bool Equals(object? obj)
    {
        if (obj is SourcePosition sourcePosition) {
            return FileName.Equals(sourcePosition.FileName) && LineNumber.Equals(LineNumber);
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return FileName.GetHashCode() + LineNumber.GetHashCode();
    }

    public override string ToString()
    {
        return $"{FileName}({LineNumber:d})";
    }
}