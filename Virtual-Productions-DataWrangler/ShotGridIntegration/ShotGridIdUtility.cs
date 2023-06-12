namespace ShotGridIntegration;

public static class ShotGridIdUtility
{
	private const short GuidCheckValue = 0x7EEF;
	public static int ToShotGridId(Guid a_entityId)
	{
		byte[] guidBytes = a_entityId.ToByteArray();
		int shotGridId = BitConverter.ToInt32(guidBytes, 0);
		short check = BitConverter.ToInt16(guidBytes, sizeof(int));
		if (check != GuidCheckValue && a_entityId != Guid.Empty)
		{
			throw new Exception($"Guid does not match expected format Guid: {a_entityId}. Expected {GuidCheckValue} in bytes 4-6");
		}

		return shotGridId;
	}

	public static Guid ToDataEntityId(int a_shotGridId)
	{
		return new Guid(a_shotGridId, GuidCheckValue, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}
}