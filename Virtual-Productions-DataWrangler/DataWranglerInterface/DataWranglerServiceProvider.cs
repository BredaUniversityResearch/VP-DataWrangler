using ShotGridIntegration;

namespace DataWranglerInterface
{
	public class DataWranglerServiceProvider
	{
		public static DataWranglerServiceProvider Instance { get; }

		static DataWranglerServiceProvider()
		{
			Instance = new DataWranglerServiceProvider();
		}

		public readonly ShotGridAPI ShotGridAPI = new ShotGridAPI();
	}
}
