using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DataWranglerCommon
{
	//Represents a SMTPE time code formatted as HH:MM:SS:FF
	//See: https://en.wikipedia.org/wiki/SMPTE_timecode
	[JsonConverter(typeof(TimeCodeJsonConverter))]
	public readonly struct TimeCode
	{
		public readonly uint TimeCodeAsBinaryCodedDecimal = ~0u;

		public static readonly TimeCode Invalid = new();

		public byte Hour => ReadInt8(TimeCodeAsBinaryCodedDecimal, 3);
		public byte Minute => ReadInt8(TimeCodeAsBinaryCodedDecimal, 2);
		public byte Second => ReadInt8(TimeCodeAsBinaryCodedDecimal, 1);
		public byte Frame => ReadInt8(TimeCodeAsBinaryCodedDecimal, 0);

		public static readonly Regex Format = new("([0-9]{2}):([0-9]{2}):([0-9]{2}):([0-9]{2})");

		private TimeCode(uint a_timeCodeAsBinaryCodedDecimal)
		{
			TimeCodeAsBinaryCodedDecimal = a_timeCodeAsBinaryCodedDecimal;
		}

		public TimeCode(int a_hour, int a_minute, int a_second, int a_frame)
			: this(BinaryCodedDecimal.FromTime(a_hour, a_minute, a_second, a_frame))
		{
		}

		public override string ToString()
		{
			return $"{Hour:D2}:{Minute:D2}:{Second:D2}:{Frame:D2}";
		}

		private static byte ReadInt8(uint a_target, int a_offset)
		{
			return ReadInt8(BitConverter.GetBytes(a_target), a_offset);
		}

		private static byte ReadInt8(byte[] a_target, int a_offset)
		{
			byte value = a_target[a_offset];
			return (byte)((((value & 0xF0) >> 4) * 10) + (value & 0x0F));
		}

		public static TimeCode FromBCD(uint a_binaryCodedTimeCode)
		{
			return new TimeCode(a_binaryCodedTimeCode);
		}

		public static TimeCode FromString(string a_formattedTimeCode)
		{
			Match match = Format.Match(a_formattedTimeCode);
			if (!match.Success)
			{
				throw new ArgumentException($"Value of time code ({a_formattedTimeCode}) does not match expected format of HH:MM:SS:FF", nameof(a_formattedTimeCode));
			}

			int hour = int.Parse(match.Groups[1].ValueSpan);
			int minute = int.Parse(match.Groups[2].ValueSpan);
			int second = int.Parse(match.Groups[3].ValueSpan);
			int frame = int.Parse(match.Groups[4].ValueSpan);
			return new TimeCode(hour, minute, second, frame);
		}

		public bool Equals(TimeCode other)
		{
			return TimeCodeAsBinaryCodedDecimal == other.TimeCodeAsBinaryCodedDecimal;
		}

		public override bool Equals(object? obj)
		{
			return obj is TimeCode other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int)TimeCodeAsBinaryCodedDecimal;
		}

		public static bool operator ==(TimeCode left, TimeCode right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(TimeCode left, TimeCode right)
		{
			return !left.Equals(right);
		}
	}
}
