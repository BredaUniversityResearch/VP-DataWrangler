using System;

namespace BlackmagicCameraControl;

//11:5 fixed point data
public readonly struct Fixed16 : IEquatable<Fixed16>
{
	private const float Exponent = (1 << 11);

	private readonly short m_data;
	public float AsFloat => ConvertToFloat();

	public Fixed16(short a_value)
	{
		m_data = a_value;
	}

	public Fixed16(float a_value)
	{
		if (a_value < -16.0f || a_value > 16.0f)
		{
			throw new ArgumentOutOfRangeException("Fixed16 only allows values in [-16..16] range");
		}

		m_data = (short) (a_value * Exponent);
	}

	public short AsInt16()
	{
		return m_data;
	}

	public float ConvertToFloat()
	{
		return (float)m_data / Exponent;
	}

	public bool Equals(Fixed16 other)
	{
		return m_data == other.m_data;
	}

	public override bool Equals(object? obj)
	{
		return obj is Fixed16 other && Equals(other);
	}

	public override int GetHashCode()
	{
		return m_data.GetHashCode();
	}

	public static bool operator ==(Fixed16 left, Fixed16 right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Fixed16 left, Fixed16 right)
	{
		return !left.Equals(right);
	}

}