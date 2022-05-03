using System;

namespace BlackmagicCameraControl;

//11:5 fixed point data
public struct Fixed16
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
}