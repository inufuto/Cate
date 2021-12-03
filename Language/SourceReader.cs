using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Inu.Language
{
    public class SourceReader : IDisposable
    {
        public const char EndOfLine = '\n';
        public const char EndOfFile = '\0';

        public static SourceReader? Current { get; private set; }
        public static SourcePrinter? Printer { private get; set; }

        private SourceReader? parent;
        public string FileName { get; private set; }
        private readonly StreamReader reader;
        public int LineNumber { get; private set; } = 0;
        private string? currentLine = "";
        private string? prevLine;
        private int currentIndex = 1;

        public SourceReader(string fileName)
        {
            FileName = fileName;
            reader = new StreamReader(fileName, Encoding.UTF8);
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public static void OpenFile(string fileName)
        {
            SourceReader sourceReader = new SourceReader(fileName)
            {
                parent = Current
            };
            Current = sourceReader;
        }

        private void Print()
        {
            if (LineNumber > 0 && Printer != null)
            {
                Debug.Assert(prevLine != null);
                Printer.AddSourceLine(prevLine);
            }
        }

        public char GetChar()
        {
            if (currentLine == null || currentIndex >= currentLine.Length) {
                if (string.IsNullOrEmpty(currentLine) && reader.EndOfStream) {
                    prevLine = currentLine;
                    Print();
                    Current = parent;
                    return '\0';
                }
                prevLine = currentLine;
                currentLine = reader.ReadLine() ?? "";
                Print();
                ++LineNumber;
                currentIndex = 0;
                return EndOfLine;
            }
            Debug.Assert(currentLine[currentIndex] != 0);
            return currentLine[currentIndex++];
        }

        public void SkipToEndOfLine()
        {
            if (currentLine != null) currentIndex = currentLine.Length;
        }


        public SourcePosition CurrentPosition => new SourcePosition(FileName, LineNumber);

        public string? Directory => Path.GetDirectoryName(FileName);
    }
}
