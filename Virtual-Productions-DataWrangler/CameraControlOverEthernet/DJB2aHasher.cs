using System.Text;

namespace CameraControlOverEthernet
{
	public static class DJB2aHasher
	{
		public static uint Hash(ReadOnlySpan<byte> a_data)
		{
			uint hash = 5381;
			for (int index = 0; index < a_data.Length; index++)
			{
				// Equivalent to: `hash * 33 ^ a_data[index]`
				hash = ((hash << 5) + hash) ^ a_data[index];
			}

			return hash;
		}

		public static uint Hash(string a_string)
		{
			return Hash(Encoding.UTF8.GetBytes(a_string));
		}
	}
}
