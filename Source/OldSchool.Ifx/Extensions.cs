using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OldSchool.Ifx
{
    public static class Extensions
    {
        private static readonly int[] m_Empty = new int[0];
        private static readonly byte[] m_EmptyBytes = new byte[0];

        public static void Set<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key, TValue value)
        {
            if (collection.ContainsKey(key))
                collection[key] = value;
            else
                collection.Add(key, value);
        }

        public static int[] Locate(this byte[] self, byte candidate)
        {
            if (IsEmpty(self))
                return m_Empty;

            var list = new List<int>();

            for (var i = 0; i < self.Length; i++)
            {
                if (self[i] != candidate)
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? m_Empty : list.ToArray();
        }

        public static byte[] RemoveBackspaces(this byte[] self)
        {
            var indexes = self.Locate(8);
            if (indexes.Length == 0)
                return self;

            var newBufferLength = self.Length - indexes.Length * 2;
            if (self[0] == 8) // If the first character is a backspace, we're only deleting one character
                newBufferLength++;

            // New Buffer, same length, minus the # of deletes * 2 (The delete character and the character deleted)
            var newBuffer = new byte[newBufferLength];

            var y = 0;
            for (var x = 0; x < self.Length; x++)
            {
                // This would mean the backspace is the last character.
                if (newBufferLength <= y)
                    break;

                if (y < 0)
                    y = 0;

                if (self[x] == 8)
                {
                    y -= 1;
                    continue;
                }

                newBuffer[y++] = self[x];
            }

            return newBuffer;
        }

        public static byte[] ExpandBackspaces(this byte[] self)
        {
            var indexes = self.Locate(8);
            if (indexes.Length == 0)
                return self;

            var newBufferLength = self.Length + indexes.Length * 2;
            var newBuffer = new byte[newBufferLength];

            var y = 0;
            for (var x = 0; x < self.Length; x++)
            {
                if (self[x] == 8)
                {
                    newBuffer[y++] = 8;
                    newBuffer[y++] = 32;
                    newBuffer[y++] = 8;
                    continue;
                }

                newBuffer[y++] = self[x];
            }

            return newBuffer;
        }

        public static int? LocateFirst(this byte[] self, byte candidate)
        {
            if (IsEmpty(self))
                return null;

            for (var i = 0; i < self.Length; i++)
            {
                if (self[i] != candidate)
                    continue;

                return i;
            }

            return null;
        }

        public static byte[] Trim(this byte[] data, int index)
        {
            if (index >= data.Length)
                return m_EmptyBytes;

            var newValue = new byte[data.Length - index];
            Buffer.BlockCopy(data, index, newValue, 0, newValue.Length);
            return newValue;
        }

        private static bool IsEmpty(byte[] array)
        {
            return (array == null) || (array.Length == 0);
        }

        public static byte[] Append(this byte[] source, string stringData)
        {
            var data = Encoding.ASCII.GetBytes(stringData);
            return Append(source, data);
        }

        public static byte[] Append(this byte[] source, byte[] data)
        {
            var newBuffer = new byte[source.Length + data.Length];
            Buffer.BlockCopy(source, 0, newBuffer, 0, source.Length);
            Buffer.BlockCopy(data, 0, newBuffer, source.Length, data.Length);
            return newBuffer;
        }

        public static byte[] Substring(this byte[] source, int start, int length)
        {
            var data = new byte[length];
            Buffer.BlockCopy(source, start, data, 0, length);
            return data;
        }

        public static byte[] Substring(this byte[] source, int index)
        {
            return source.Substring(index, source.Length - index);
        }

        public static async Task<byte[]> GetBytes(this Stream stream)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}