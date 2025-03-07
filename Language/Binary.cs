using System.IO;
using System.Text;

namespace Inu.Language;

public static class Binary
{
    public static void WriteByte(this Stream stream, int value)
    {
        stream.WriteByte((byte)value);
    }

    public static void WriteBytes(this Stream stream, int value, int count)
    {
        for (var i = 0; i < count; ++i) {
            stream.WriteByte((byte)value);
        }
    }

    public static void WriteByteArray(this Stream stream, params  int[] values)
    {
        foreach (var value in values)
        {
            stream.WriteByte((byte)value);
        }
    }

    public static void WriteWord(this Stream stream, int value)
    {
        stream.WriteByte(value);
        stream.WriteByte(value >> 8);
    }

    public static void WriteDWord(this Stream stream, uint value)
    {
        stream.WriteWord((ushort)value);
        stream.WriteWord((ushort)(value >> 16));
    }

    public static void WriteString(this Stream stream, string s)
    {
        var bytes = Encoding.ASCII.GetBytes(s);
        stream.WriteWord(bytes.Length);
        foreach (var b in bytes) {
            stream.WriteByte(b);
        }
    }

    public static int ReadWord(this Stream stream)
    {
        var l = stream.ReadByte();
        var h = stream.ReadByte();
        return l | (h << 8);
    }

    public static string ReadString(this Stream stream)
    {
        var n = stream.ReadWord();
        var s = new StringBuilder();
        for (var i = 0; i < n; ++i) {
            var c = (char)(stream.ReadByte());
            s.Append(c);
        }
        return s.ToString();
    }
}