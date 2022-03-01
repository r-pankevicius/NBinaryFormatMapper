// Used under MIT License, see https://github.com/r-pankevicius/NBinaryFormatMapper

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NBinaryFormatMapper
{
	/// <summary>
	/// Provides read only access mapping to structs over memory bytes.
	/// Memory is allocated with GC pinning.
	/// </summary>
	/// <remarks>
	/// Tip: mark structure layout using <see cref="StructLayoutAttribute"/> attribute.
	/// </remarks>
	public class StructMapper : IDisposable
	{
		readonly byte[] m_Bytes;
		readonly GCHandle m_GCHandle;
		readonly IntPtr m_OriginAddress;

		/// <summary>
		/// Access to underlying bytes array.
		/// </summary>
		public byte[] Bytes => m_Bytes;

		/// <summary>
		/// Constructor "pins" bytes in memory (see <see cref="GCHandleType.Pinned"/>).
		/// </summary>
		/// <param name="bytes">Bytes to work with</param>
		public StructMapper(byte[] bytes)
		{
			m_Bytes = bytes;
			m_GCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			m_OriginAddress = m_GCHandle.AddrOfPinnedObject();
		}

		/// <summary>
		/// Reads all bytes from file and creates mapper on them.
		/// </summary>
		/// <param name="pathToFile">Path to file</param>
		/// <returns>New <see cref="StructMapper"/> over file content.</returns>
		public static StructMapper FromFile(string pathToFile) =>
			new(File.ReadAllBytes(pathToFile));

		/// <summary>
		/// Reads all bytes from file and creates mapper on them (async version).
		/// </summary>
		/// <param name="pathToFile">Path to file</param>
		/// <returns>New <see cref="StructMapper"/> over file content.</returns>
		public static async Task<StructMapper> FromFileAsync(string pathToFile)
		{
			byte[] bytes = await File.ReadAllBytesAsync(pathToFile).ConfigureAwait(false);
			return new(bytes);
		}

		public void Dispose()
		{
			m_GCHandle.Free();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Copies bytes at given offset to the structure.
		/// <para>
		/// Note: changing fields of returned structure doesn't change underlying bytes.
		/// </para>
		/// </summary>
		/// <typeparam name="T">Structure type</typeparam>
		/// <param name="offset">Offset in underlying memory bytes, starting from 0</param>
		/// <returns>Structure (copy of bytes)</returns>
		public T Read<T>(int offset) where T : struct =>
			(T)Marshal.PtrToStructure(m_OriginAddress + offset, typeof(T));

		public IntPtr GetPointer(int offset) => m_OriginAddress + offset;

		public IEnumerable<T> ReadMultiple<T>(int offset, int recordSize, int count) where T : struct
		{
			IntPtr pointer = GetPointer(offset);
			return ReadMultiple<T>(pointer, recordSize, count);
		}

		public static IEnumerable<T> ReadMultiple<T>(IntPtr pointer, int recordSize, int count) where T : struct
		{
			for (int idx = 0; idx < count; idx++)
			{
				yield return (T)Marshal.PtrToStructure(pointer, typeof(T));
				pointer += recordSize;
			}
		}

		public string ReadUnicodeString(int offset, int numCharacters) =>
			ReadString(offset, numCharacters, 2, Encoding.Unicode);

		public string ReadASCIIString(int offset, int numCharacters) =>
			ReadString(offset, numCharacters, 1, Encoding.ASCII);

		public byte[] ReadBytes(int offset, int size)
		{
			var result = new byte[size];
			Array.Copy(m_Bytes, offset, result, 0, size);
			return result;
		}

		public static string ToASCIIString(params byte[] bytes) =>
			Encoding.ASCII.GetString(bytes);

		#region Implementation

		string ReadString(int offset, int numCharacters, int characterSize, Encoding encoding)
		{
			int numBytes = numCharacters * characterSize;
			if (numBytes <= 0)
				return string.Empty;

			return encoding.GetString(m_Bytes, offset, numBytes);
		}

		#endregion
	}
}
