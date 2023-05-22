namespace DataWranglerCommon.BRAWSupport
{
	public enum EBlackmagicCameraCodec
	{
		Raw = 0,
		DNxHD = 1,
		ProRes = 2,
		BlackmagicRAW = 3
	}

	public static class BlackmagicCameraCodec
	{
		public static bool FindFromFileExtension(string a_fileExtension, out EBlackmagicCameraCodec a_result)
		{
			a_result = EBlackmagicCameraCodec.Raw;
			bool result = false;

			switch (a_fileExtension)
			{
				case ".raw":
					a_result = EBlackmagicCameraCodec.Raw;
					result = true;
					break;
				case ".braw":
					a_result = EBlackmagicCameraCodec.BlackmagicRAW;
					result = true;
					break;
			}

			return result;
		}
	};
}
