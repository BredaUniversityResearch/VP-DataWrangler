using System;
using DataWranglerCommon;
using Xunit;

namespace BlackmagicCameraControlTest
{
	public class BinaryCodedTimeTests
	{
		[Fact]
		public void ToBinaryCodedTime()
		{
			DateTime time = new DateTime(2015, 05, 03, 09, 10, 18);
			int bcdTime = BinaryCodedDecimal.FromTime(time);
			Assert.True(bcdTime == 0x09101800);
		}

		[Fact]
		public void ToBinaryCodedDate()
		{
			DateTime time = new DateTime(2015, 05, 03, 09, 10, 18);
			int bcdTime = BinaryCodedDecimal.FromDate(time);
			Assert.True(bcdTime == 0x20150503);
		}

		[Fact]
		public void FromBinaryCodedTime()
		{
			int bcdTime = 0x09101800;
			int bcdDate = 0x20150503;
			DateTime time = BinaryCodedDecimal.ToDateTime(bcdDate, bcdTime);
			Assert.True(time == new DateTime(2015, 05, 03, 09, 10, 18));
		}
	}
}
