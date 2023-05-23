using DataWranglerCommon;

namespace DataWranglerInterface
{
	public class DataWranglerServiceProvider
	{
		public static DataWranglerServices Instance { get; private set; } = null!;

		public static void Use(DataWranglerServices a_services)
		{
			Instance = a_services;
		}
	}
}
