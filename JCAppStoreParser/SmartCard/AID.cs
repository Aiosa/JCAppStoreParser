using System;
using System.Linq;

namespace JCAppStore_Parser
{
    /// <summary>
    /// AID javacard representation
    /// 
    /// source implementation:
    /// https://github.com/martinpaljak/capfile/tree/7b93239f574270d0d556c420de5b003fa8d78cf8
    /// file AID.java
    /// </summary>
    public sealed class AID
    {
        private readonly byte[] bytes;

        public AID(byte[] bytes) : this(bytes, 0, bytes.Length) { }

        public AID(string str) : this(BitConverter.GetBytes(uint.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier))) { }

        public AID(byte[] bytes, int offset, int length)
        {
            if ((length < 5) || (length > 16))
            {
                throw new Exception("AID must be between 5 and 16 bytes: " + length);
            }
            this.bytes = bytes.Skip(offset).Take(length).ToArray();
        }

        public static bool Valid(string aid)
        {
            return aid.All(c => "0123456789abcdefABCDEF".Contains(c)) && aid.Length > 9 && aid.Length < 33;
        }

        public byte[] getBytes()
        {
            return bytes;
        }

        public int getLength()
        {
            return bytes.Length;
        }

        public override string ToString()
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public override int GetHashCode()
        {
            return bytes.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o is AID) {
                return bytes.Equals(((AID)o).bytes);
            }
            return false;
        }
    }
}
