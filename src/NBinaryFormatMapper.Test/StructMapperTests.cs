using Shouldly;
using System.Runtime.InteropServices;
using Xunit;

namespace NBinaryFormatMapper.Test
{
	public class StructMapperTests
	{
		[StructLayout(LayoutKind.Sequential)]
		unsafe private struct TestStruct
		{
			public short number;
			public byte byte1;
			public fixed byte bytesArr3[3];
			public byte byte2;
		}

		[Fact]
		unsafe public void ProperlyMapsStruct()
		{
			int structSize = sizeof(TestStruct);
			structSize.ShouldBe(8); // Why? Probably .net uses some padding

			var byteArray = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

			using var mapper = new StructMapper(byteArray);
			var mappedResult = mapper.Read<TestStruct>(offset: 0);
			(mappedResult.number).ShouldBe((short)(2 * 256 + 1));

			mappedResult.byte1.ShouldBe((byte)3);
			mappedResult.bytesArr3[0].ShouldBe((byte)4);
			mappedResult.bytesArr3[1].ShouldBe((byte)5);
			mappedResult.bytesArr3[2].ShouldBe((byte)6);
			mappedResult.byte2.ShouldBe((byte)7);
		}

		[Fact]
		public void ReadsStructOverBytesArray()
		{
			using var mapper1 = new StructMapper(new byte[] { 1, 0 });
			{
				var result = mapper1.Read<TestStruct>(offset: 0);
				((int)result.number).ShouldBe(1);
			}

			using var mapper2 = new StructMapper(new byte[] { 0, 1 });
			{
				var result = mapper2.Read<TestStruct>(offset: 0);
				((int)result.number).ShouldBe(256);
			}
		}
    }
}
