namespace DataWranglerCommon
{
	public class BinaryCodedDecimal
	{
		public static void WriteInt8(int a_value, byte[] a_target, int a_offset)
		{
			byte lower = (byte)(a_value % 10);
			byte upper = (byte)(a_value / 10);
			a_target[a_offset] = (byte)((upper << 4) | lower);
		}

		public static void WriteInt16(int a_value, byte[] a_target, int a_offset)
		{
			WriteInt8(a_value / 100, a_target, a_offset);
			WriteInt8(a_value % 100, a_target, a_offset - 1);
		}

		public static byte ReadInt8(byte[] a_target, int a_offset)
		{
			byte value = a_target[a_offset];
			return (byte)((((value & 0xF0) >> 4) * 10) + (value & 0x0F));
		}

		public static short ReadInt16(byte[] a_target, int a_offset)
		{
			short upper = ReadInt8(a_target, a_offset);
			short lower = ReadInt8(a_target, a_offset - 1);
			return (short)((upper * 100) + lower);
		}

		public static int FromTime(int a_hour, int a_minute, int a_second, int a_frame)
		{
			byte[] buffer = new byte[4];
			WriteInt8(a_hour, buffer, 3);
			WriteInt8(a_minute, buffer, 2);
			WriteInt8(a_second, buffer, 1);
			WriteInt8(a_frame, buffer, 0);
			return BitConverter.ToInt32(buffer);
		}

		public static int FromTime(DateTime a_time)
		{
			return FromTime(a_time.Hour, a_time.Minute, a_time.Second, 0);
		}

		public static int FromDate(DateTime a_time)
		{
			byte[] buffer = new byte[4];
			WriteInt16(a_time.Year, buffer, 3);
			WriteInt8(a_time.Month, buffer, 1);
			WriteInt8(a_time.Day, buffer, 0);
			return BitConverter.ToInt32(buffer);
		}

		public static DateTime ToDateTime(int a_date, int a_time)
		{
			//BCD - Date: YYYYMMDD
			//		Time: HHMMSSFF - Frame (FF) not used.
			byte[] dateBuffer = BitConverter.GetBytes(a_date);
			byte[] timeBuffer = BitConverter.GetBytes(a_time);
		
			int year = ReadInt16(dateBuffer, 3);
			int month = ReadInt8(dateBuffer, 1);
			int day = ReadInt8(dateBuffer, 0);

			int hour = ReadInt8(timeBuffer, 3);
			int minute = ReadInt8(timeBuffer, 2);
			int second = ReadInt8(timeBuffer, 1);

			return new DateTime(year, month, day, hour, minute, second);
		}
	};
}