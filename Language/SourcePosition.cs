using System.Collections.Generic;

namespace Inu.Language
{
    public readonly struct SourcePosition
    {
        public readonly string FileName;
        public readonly int LineNumber;

        public SourcePosition(string fileName, int lineNumber)
        {
            FileName = fileName;
            LineNumber = lineNumber;
        }

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
}
