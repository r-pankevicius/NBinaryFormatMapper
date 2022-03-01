using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NBinaryFormatMapper
{
	/// <summary>
	/// Helps to map memory bytes to structs.
	/// Mark structure layout using <see cref="StructLayoutAttribute"/> attribute.
	/// </summary>
	public class StructMapper : IDisposable
	{
		readonly byte[] m_Bytes;
		readonly GCHandle m_GCHandle;
		readonly IntPtr m_OriginAddress;

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
			new StructMapper(File.ReadAllBytes(pathToFile));

		/// <summary>
		/// Reads all bytes from file and creates mapper on them (async version).
		/// </summary>
		/// <param name="pathToFile">Path to file</param>
		/// <returns>New <see cref="StructMapper"/> over file content.</returns>
		public static async Task<StructMapper> FromFileAsync(string pathToFile)
		{
			byte[] bytes = await File.ReadAllBytesAsync(pathToFile).ConfigureAwait(false);
			return new StructMapper(bytes);
		}
		
		public void Dispose() => m_GCHandle.Free();

		public T Read<T>(int offset) where T : struct =>
			(T)Marshal.PtrToStructure(m_OriginAddress + offset, typeof(T));

		public IEnumerable<T> ReadMany<T>(int offset, int recordSize, int count) where T : struct
		{
			IntPtr pointer = m_OriginAddress + offset;
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
