﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SoulsFormats
{
    /// <summary>
    /// Miscellaneous utility functions for SoulsFormats, mostly for internal use.
    /// </summary>
    public static class SFUtil
    {
        /// <summary>
        /// Guesses the extension of a file based on its contents.
        /// </summary>
        public static string GuessExtension(byte[] bytes, bool bigEndian = false)
        {
            bool dcx = false;
            if (DCX.Is(bytes))
            {
                dcx = true;
                bytes = DCX.Decompress(bytes);
            }

            bool checkMsb(BinaryReaderEx br)
            {
                if (br.Length < 8)
                    return false;

                int offset = br.GetInt32(4);
                if (offset < 0 || offset >= br.Length - 1)
                    return false;

                try
                {
                    return br.GetASCII(offset) == "MODEL_PARAM_ST";
                }
                catch
                {
                    return false;
                }
            }

            bool checkParam(BinaryReaderEx br)
            {
                if (br.Length < 0x2C)
                    return false;

                string param = br.GetASCII(0xC, 0x20);
                return Regex.IsMatch(param, "^[^\0]+\0 *$");
            }

            bool checkTdf(BinaryReaderEx br)
            {
                if (br.Length < 4)
                    return false;

                if (br.GetASCII(0, 1) != "\"")
                    return false;

                for (int i = 1; i < br.Length; i++)
                {
                    if (br.GetASCII(i, 1) == "\"")
                    {
                        return i < br.Length - 2 && br.GetASCII(i + 1, 2) == "\r\n";
                    }
                }
                return false;
            }

            string ext = "";
            using (var ms = new MemoryStream(bytes))
            {
                var br = new BinaryReaderEx(bigEndian, ms);
                if (br.Length >= 4 && br.GetASCII(0, 4) == "AISD")
                    ext = ".aisd";
                else if (br.Length >= 4 && (br.GetASCII(0, 4) == "BDF3" || br.GetASCII(0, 4) == "BDF4"))
                    ext = ".bdt";
                else if (br.Length >= 4 && (br.GetASCII(0, 4) == "BHF3" || br.GetASCII(0, 4) == "BHF4"))
                    ext = ".bhd";
                else if (br.Length >= 4 && (br.GetASCII(0, 4) == "BND3" || br.GetASCII(0, 4) == "BND4"))
                    ext = ".bnd";
                else if (br.Length >= 4 && br.GetASCII(0, 4) == "DDS ")
                    ext = ".dds";
                // ESD or FFX
                else if (br.Length >= 4 && br.GetASCII(0, 4).ToUpper() == "DLSE")
                    ext = ".dlse";
                else if (br.Length >= 4 && (bigEndian && br.GetASCII(0, 4) == "\0BRD" || !bigEndian && br.GetASCII(0, 4) == "DRB\0"))
                    ext = ".drb";
                else if (br.Length >= 4 && br.GetASCII(0, 4) == "ENFL")
                    ext = ".entryfilelist";
                else if (br.Length >= 4 && br.GetASCII(0, 4).ToUpper() == "FSSL")
                    ext = ".esd";
                else if (br.Length >= 3 && br.GetASCII(0, 3) == "FEV" || br.Length >= 0x10 && br.GetASCII(8, 8) == "FEV FMT ")
                    ext = ".fev";
                else if (br.Length >= 6 && br.GetASCII(0, 6) == "FLVER\0")
                    ext = ".flver";
                else if (br.Length >= 3 && br.GetASCII(0, 3) == "FSB")
                    ext = ".fsb";
                else if (br.Length >= 3 && br.GetASCII(0, 3) == "GFX")
                    ext = ".gfx";
                else if (br.Length >= 0x19 && br.GetASCII(0xC, 0xE) == "ITLIMITER_INFO")
                    ext = ".itl";
                else if (br.Length >= 4 && br.GetASCII(1, 3) == "Lua")
                    ext = ".lua";
                else if (checkMsb(br))
                    ext = ".msb";
                else if (br.Length >= 0x30 && br.GetASCII(0x2C, 4) == "MTD ")
                    ext = ".mtd";
                else if (checkParam(br))
                    ext = ".param";
                else if (br.Length >= 4 && br.GetASCII(1, 3) == "PNG")
                    ext = ".png";
                else if (br.Length >= 0x2C && br.GetASCII(0x28, 4) == "SIB ")
                    ext = ".sib";
                else if (br.Length >= 4 && br.GetASCII(0, 4) == "TAE ")
                    ext = ".tae";
                else if (checkTdf(br))
                    ext = ".tdf";
                else if (br.Length >= 4 && br.GetASCII(0, 4) == "TPF\0")
                    ext = ".tpf";
                else if (br.Length >= 4 && br.GetASCII(0, 4) == "#BOM")
                    ext = ".txt";
                else if (br.Length >= 5 && br.GetASCII(0, 5) == "<?xml")
                    ext = ".xml";
                // This is pretty sketchy
                else if (br.Length >= 0xC && br.GetByte(0) == 0 && br.GetByte(3) == 0 && br.GetInt32(4) == br.Length && br.GetInt16(0xA) == 0)
                    ext = ".fmg";
            }

            if (dcx)
                return ext + ".dcx";
            else
                return ext;
        }

        /// <summary>
        /// Reverses the order of bits in a byte, probably very inefficiently.
        /// </summary>
        public static byte ReverseBits(byte value)
        {
            return (byte)(
                ((value & 0b00000001) << 7) |
                ((value & 0b00000010) << 5) |
                ((value & 0b00000100) << 3) |
                ((value & 0b00001000) << 1) |
                ((value & 0b00010000) >> 1) |
                ((value & 0b00100000) >> 3) |
                ((value & 0b01000000) >> 5) |
                ((value & 0b10000000) >> 7)
                );
        }

        /// <summary>
        /// Makes a backup of a file if not already found, and returns the backed-up path.
        /// </summary>
        public static string Backup(string file)
        {
            string bak = file + ".bak";
            if (!File.Exists(bak))
                File.Copy(file, bak);
            return bak;
        }

        /// <summary>
        /// Returns the extension of the specified file path, removing .dcx if present.
        /// </summary>
        public static string GetRealExtension(string path)
        {
            string extension = Path.GetExtension(path);
            if (extension == ".dcx")
                extension = Path.GetExtension(Path.GetFileNameWithoutExtension(path));
            return extension;
        }

        /// <summary>
        /// Returns the file name of the specified path, removing both .dcx if present and the actual extension.
        /// </summary>
        public static string GetRealFileName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (Path.GetExtension(path) == ".dcx")
                name = Path.GetFileNameWithoutExtension(name);
            return name;
        }

        /// <summary>
        /// Decompresses data and returns a new BinaryReaderEx if necessary.
        /// </summary>
        public static BinaryReaderEx GetDecompressedBR(BinaryReaderEx br, out DCX.Type compression)
        {
            if (DCX.Is(br))
            {
                byte[] bytes = DCX.Decompress(br, out compression);
                return new BinaryReaderEx(false, bytes);
            }
            else
            {
                compression = DCX.Type.None;
                return br;
            }
        }

        /// <summary>
        /// FromSoft's basic filename hashing algorithm, used in some BND and BXF formats.
        /// </summary>
        public static uint FromPathHash(string text)
        {
            string hashable = text.ToLowerInvariant().Replace('\\', '/');
            if (!hashable.StartsWith("/"))
                hashable = '/' + hashable;
            return hashable.Aggregate(0u, (i, c) => i * 37u + c);
        }

        /// <summary>
        /// Determines whether a number is prime or not.
        /// </summary>
        public static bool IsPrime(uint candidate)
        {
            if (candidate < 2)
                return false;
            if (candidate == 2)
                return true;
            if (candidate % 2 == 0)
                return false;

            for (int i = 3; i * i <= candidate; i += 2)
            {
                if (candidate % i == 0)
                    return false;
            }

            return true;
        }

        private static readonly Regex timestampRx = new Regex(@"(\d\d)(\w)(\d+)(\w)(\d+)");

        /// <summary>
        /// Converts a BND/BXF timestamp string to a DateTime object.
        /// </summary>
        public static DateTime BinderTimestampToDate(string timestamp)
        {
            Match match = timestampRx.Match(timestamp);
            if (!match.Success)
                throw new InvalidDataException("Unrecognized timestamp format.");

            int year = Int32.Parse(match.Groups[1].Value) + 2000;
            int month = match.Groups[2].Value[0] - 'A';
            int day = Int32.Parse(match.Groups[3].Value);
            int hour = match.Groups[4].Value[0] - 'A';
            int minute = Int32.Parse(match.Groups[5].Value);

            return new DateTime(year, month, day, hour, minute, 0);
        }

        /// <summary>
        /// Converts a DateTime object to a BND/BXF timestamp string.
        /// </summary>
        public static string DateToBinderTimestamp(DateTime dateTime)
        {
            int year = dateTime.Year - 2000;
            if (year < 0 || year > 99)
                throw new InvalidDataException("BND timestamp year must be between 2000 and 2099 inclusive.");

            char month = (char)(dateTime.Month + 'A');
            int day = dateTime.Day;
            char hour = (char)(dateTime.Hour + 'A');
            int minute = dateTime.Minute;

            return $"{year:D2}{month}{day}{hour}{minute}".PadRight(8, '\0');
        }

        /// <summary>
        /// Compresses data and writes it to a BinaryWriterEx with Zlib wrapper.
        /// </summary>
        public static int WriteZlib(BinaryWriterEx bw, byte formatByte, byte[] input)
        {
            long start = bw.Position;
            bw.WriteByte(0x78);
            bw.WriteByte(formatByte);

            using (var deflateStream = new DeflateStream(bw.Stream, CompressionMode.Compress, true))
            {
                deflateStream.Write(input, 0, input.Length);
            }

            bw.WriteUInt32(Adler32(input));
            return (int)(bw.Position - start);
        }

        /// <summary>
        /// Reads a Zlib block from a BinaryReaderEx and returns the uncompressed data.
        /// </summary>
        public static byte[] ReadZlib(BinaryReaderEx br, int compressedSize)
        {
            br.AssertByte(0x78);
            br.AssertByte(0x01, 0x9C, 0xDA);
            byte[] compressed = br.ReadBytes(compressedSize - 2);

            using (var decompressedStream = new MemoryStream())
            {
                using (var compressedStream = new MemoryStream(compressed))
                using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
                return decompressedStream.ToArray();
            }
        }

        /// <summary>
        /// Computes an Adler32 checksum used by Zlib.
        /// </summary>
        public static uint Adler32(byte[] data)
        {
            uint adlerA = 1;
            uint adlerB = 0;

            foreach (byte b in data)
            {
                adlerA = (adlerA + b) % 65521;
                adlerB = (adlerB + adlerA) % 65521;
            }

            return (adlerB << 16) | adlerA;
        }

        /// <summary>
        /// Concatenates multiple collections into one list.
        /// </summary>
        public static List<T> ConcatAll<T>(params IEnumerable<T>[] lists)
        {
            IEnumerable<T> all = new List<T>();
            foreach (IEnumerable<T> list in lists)
                all = all.Concat(list);
            return all.ToList();
        }

        /// <summary>
        /// Convert a list to a dictionary with indices as keys.
        /// </summary>
        public static Dictionary<int, T> Dictionize<T>(List<T> items)
        {
            var dict = new Dictionary<int, T>(items.Count);
            for (int i = 0; i < items.Count; i++)
                dict[i] = items[i];
            return dict;
        }

        private static byte[] ds3RegulationKey = Encoding.ASCII.GetBytes("ds3#jn/8_7(rsY9pg55GFN7VFL#+3n/)");

        /// <summary>
        /// Decrypts and unpacks DS3's regulation BND4 from the specified path.
        /// </summary>
        public static BND4 DecryptDS3Regulation(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bytes = DecryptByteArray(ds3RegulationKey, bytes);
            return BND4.Read(bytes);
        }

        /// <summary>
        /// Repacks and encrypts DS3's regulation BND4 to the specified path.
        /// </summary>
        public static void EncryptDS3Regulation(string path, BND4 bnd)
        {
            byte[] bytes = bnd.Write();
            bytes = EncryptByteArray(ds3RegulationKey, bytes);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }

        private static byte[] EncryptByteArray(byte[] key, byte[] secret)
        {
            using (MemoryStream ms = new MemoryStream())
            using (AesManaged cryptor = new AesManaged())
            {
                cryptor.Mode = CipherMode.CBC;
                cryptor.Padding = PaddingMode.PKCS7;
                cryptor.KeySize = 256;
                cryptor.BlockSize = 128;

                byte[] iv = cryptor.IV;

                using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(secret, 0, secret.Length);
                }
                byte[] encryptedContent = ms.ToArray();

                byte[] result = new byte[iv.Length + encryptedContent.Length];

                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

                return result;
            }
        }

        private static byte[] DecryptByteArray(byte[] key, byte[] secret)
        {
            byte[] iv = new byte[16];
            byte[] encryptedContent = new byte[secret.Length - 16];

            Buffer.BlockCopy(secret, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(secret, iv.Length, encryptedContent, 0, encryptedContent.Length);

            using (MemoryStream ms = new MemoryStream())
            using (AesManaged cryptor = new AesManaged())
            {
                cryptor.Mode = CipherMode.CBC;
                cryptor.Padding = PaddingMode.None;
                cryptor.KeySize = 256;
                cryptor.BlockSize = 128;

                using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateDecryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedContent, 0, encryptedContent.Length);
                }
                return ms.ToArray();
            }
        }
    }
}
