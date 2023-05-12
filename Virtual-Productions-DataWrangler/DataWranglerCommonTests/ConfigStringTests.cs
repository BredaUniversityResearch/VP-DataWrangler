using DataWranglerCommon;

namespace DataWranglerCommonTests
{
	public class ConfigStringTests
	{
		private const string SingleConfigString = "Hello ${Replacement}";
		private static readonly KeyValuePair<string, string> SingleConfigReplacement = new KeyValuePair<string, string>("Replacement", "World");
		private static readonly string SingleConfigExpected = $"Hello {SingleConfigReplacement.Value}";

		[Fact]
		public void SingleReplacementTest()
		{
			ConfigString cfg = new ConfigString(SingleConfigString);
			ConfigStringBuilder sb = new ConfigStringBuilder();
			sb.AddReplacement(SingleConfigReplacement.Key, SingleConfigReplacement.Value);

			string result = cfg.Build(sb);
			Assert.True(result == SingleConfigExpected);
		}
	}
}
