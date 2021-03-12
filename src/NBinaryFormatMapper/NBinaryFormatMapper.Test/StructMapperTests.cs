using Shouldly;
using Xunit;

namespace NBinaryFormatMapper.Test
{
	public class StructMapperTests
	{
		private struct _16bits
		{
			public short Number;
		}

		[Fact]
		public void Test16bits()
		{
			using var mapper1 = new StructMapper(new byte[] { 1, 0 });
			{
				var result = mapper1.Read<_16bits>(0);
				((int)result.Number).ShouldBe(1);
			}

			using var mapper2 = new StructMapper(new byte[] { 0, 1 });
			{
				var result = mapper2.Read<_16bits>(0);
				((int)result.Number).ShouldBe(256);
			}
		}
    }
}
