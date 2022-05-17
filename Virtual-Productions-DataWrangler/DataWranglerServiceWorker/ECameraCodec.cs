using System;

namespace DataWranglerServiceWorker
{
	public enum ECameraCodec
	{
		Raw = 0,
		DNxHD = 1,
		ProRes = 2,
		BlackmagicRAW = 3
	}

	public static class CameraCodec
	{
		public static bool FindFromFileExtension(string a_fileExtension, out ECameraCodec a_result)
		{
			a_result = ECameraCodec.Raw;
			bool result = false;

			switch (a_fileExtension)
			{
				case ".raw":
					a_result = ECameraCodec.Raw;
					result = true;
					break;
				case ".braw":
					a_result = ECameraCodec.BlackmagicRAW;
					result = true;
					break;
			}

			return result;
		}
	};
}
