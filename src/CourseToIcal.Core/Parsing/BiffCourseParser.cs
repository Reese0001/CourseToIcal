using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CourseToIcal.Core.Models;

namespace CourseToIcal.Core.Parsing
{
    internal static class BiffCourseParser
    {
        public static List<Course> Parse(string path)
        {
            byte[] file;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var memory = new MemoryStream()) { stream.CopyTo(memory); file = memory.ToArray(); }
            if (file.Length < 512 || BitConverter.ToUInt16(file, 30) != 9) throw new InvalidDataException("暂不支持该 Excel 文件的内部格式。\n文件：" + path);
            return ParseBytes(file, path);
        }

        private static List<Course> ParseBytes(byte[] file, string path)
        {
            int sectorSize = 1 << BitConverter.ToUInt16(file, 30);
            Func<int, byte[]> sector = index =>
            {
                int offset = (index + 1) * sectorSize;
                if (offset < 0 || offset + sectorSize > file.Length) throw new InvalidDataException("Excel OLE 扇区超出文件范围。");
                byte[] bytes = new byte[sectorSize];
                Buffer.BlockCopy(file, offset, bytes, 0, sectorSize);
                return bytes;
            };
            var fatSectorIds = new List<uint>();
            for (int i = 0; i < 109 && 76 + i * 4 + 4 <= file.Length; i++)
            {
                uint id = BitConverter.ToUInt32(file, 76 + i * 4);
                if (id != 0xFFFFFFFF) fatSectorIds.Add(id);
            }
            var fatEntries = new List<uint>();
            foreach (uint id in fatSectorIds)
            {
                byte[] bytes = sector((int)id);
                for (int offset = 0; offset < bytes.Length; offset += 4) fatEntries.Add(BitConverter.ToUInt32(bytes, offset));
            }
            uint directoryStart = BitConverter.ToUInt32(file, 48);
            List<byte> directory = ReadChain(directoryStart, fatEntries, sector, int.MaxValue);
            uint workbookStart = 0, workbookSize = 0;
            for (int offset = 0; offset + 128 <= directory.Count; offset += 128)
            {
                ushort nameLength = BitConverter.ToUInt16(directory.ToArray(), offset + 64);
                if (nameLength < 2) continue;
                string name = Encoding.Unicode.GetString(directory.Skip(offset).Take(nameLength - 2).ToArray());
                if (name == "Workbook" || name == "Book")
                {
                    workbookStart = BitConverter.ToUInt32(directory.ToArray(), offset + 116);
                    workbookSize = BitConverter.ToUInt32(directory.ToArray(), offset + 120);
                    break;
                }
            }
            if (workbookSize == 0) throw new InvalidDataException("找不到 Excel 工作簿数据。\n文件：" + path);
            byte[] raw = ReadChain(workbookStart, fatEntries, sector, (int)Math.Min(workbookSize, int.MaxValue)).Take((int)workbookSize).ToArray();
            var sharedStrings = new List<string>();
            var cells = new Dictionary<string, string>();
            int position = 0;
            while (position + 4 <= raw.Length)
            {
                ushort type = BitConverter.ToUInt16(raw, position);
                ushort length = BitConverter.ToUInt16(raw, position + 2);
                position += 4;
                if (position + length > raw.Length) break;
                if (type == 0x00FC && length >= 8)
                {
                    uint unique = BitConverter.ToUInt32(raw, position + 4);
                    uint cursor = (uint)(position + 8);
                    uint end = (uint)(position + length);
                    for (uint i = 0; i < unique && cursor + 3 <= end; i++)
                    {
                        ushort chars = BitConverter.ToUInt16(raw, (int)cursor); cursor += 2;
                        byte flags = raw[cursor++];
                        int richCount = (flags & 0x08) != 0 ? BitConverter.ToUInt16(raw, (int)cursor) : 0;
                        if ((flags & 0x08) != 0) cursor += 2;
                        int extensionLength = (flags & 0x04) != 0 ? BitConverter.ToInt32(raw, (int)cursor) : 0;
                        if ((flags & 0x04) != 0) cursor += 4;
                        int bytesPerCharacter = (flags & 1) != 0 ? 2 : 1;
                        int byteCount = chars * bytesPerCharacter;
                        if (cursor + byteCount > end) break;
                        string value = (flags & 1) != 0 ? Encoding.Unicode.GetString(raw, (int)cursor, byteCount) : Encoding.Default.GetString(raw, (int)cursor, byteCount);
                        cursor += (uint)(byteCount + richCount * 4 + extensionLength);
                        sharedStrings.Add(value);
                    }
                }
                else if (type == 0x00FD && length >= 10)
                {
                    ushort row = BitConverter.ToUInt16(raw, position);
                    ushort column = BitConverter.ToUInt16(raw, position + 2);
                    uint index = BitConverter.ToUInt32(raw, position + 6);
                    if (index < sharedStrings.Count) cells[row + ":" + column] = sharedStrings[(int)index];
                }
                position += length;
            }
            var output = new List<Course>();
            foreach (KeyValuePair<string, string> cell in cells)
            {
                string[] coordinates = cell.Key.Split(':');
                int row = int.Parse(coordinates[0]);
                int column = int.Parse(coordinates[1]);
                if (row < 3 || column < 1 || column > 7) continue;
                output.AddRange(CourseParser.ParseCourseCell(cell.Value, column, path));
            }
            return output;
        }

        private static List<byte> ReadChain(uint start, List<uint> fatEntries, Func<int, byte[]> sector, int maxBytes)
        {
            var result = new List<byte>();
            uint current = start;
            var visited = new HashSet<uint>();
            while (current != 0xFFFFFFFE && current != 0xFFFFFFFF && current < fatEntries.Count && visited.Add(current) && result.Count < maxBytes)
            {
                result.AddRange(sector((int)current));
                current = fatEntries[(int)current];
            }
            return result;
        }
    }
}
