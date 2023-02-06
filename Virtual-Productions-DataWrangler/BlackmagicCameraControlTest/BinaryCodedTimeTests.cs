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
			uint bcdTime = BinaryCodedDecimal.FromTime(time);
			Assert.True(bcdTime == 0x09101800);
		}

		[Fact]
		public void ToBinaryCodedDate()
		{
			DateTime time = new DateTime(2015, 05, 03, 09, 10, 18);
			uint bcdTime = BinaryCodedDecimal.FromDate(time);
			Assert.True(bcdTime == 0x20150503);
		}

		[Fact]
		public void FromBinaryCodedTime()
		{
			uint bcdTime = 0x09101800;
			uint bcdDate = 0x20150503;
			DateTime time = BinaryCodedDecimal.ToDateTime(bcdDate, bcdTime);
			Assert.True(time == new DateTime(2015, 05, 03, 09, 10, 18));
		}
	}
}
