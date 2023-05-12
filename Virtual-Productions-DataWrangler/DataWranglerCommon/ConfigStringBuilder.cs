using System.Text.RegularExpressions;

namespace DataWranglerCommon
{
	public class ConfigString
	{
		private readonly string m_configValue;

		public ConfigString(string a_configStringValue)
		{
			m_configValue = a_configStringValue;
		}

		public string Build(ConfigStringBuilder a_builder)
		{
			return a_builder.Replace(m_configValue);
		}
	};

	public class ConfigStringBuilder
	{
		private Dictionary<string, string> m_replacementsByKey = new Dictionary<string, string>();

		public void AddReplacement(string a_key, string a_replacementValue)
		{
			m_replacementsByKey.Add(a_key, a_replacementValue);
		}

		public string Replace(string a_configString)
		{
			Regex regex = new Regex("\\$\\{([a-zA-Z0-9]+)\\}", RegexOptions.CultureInvariant);
			bool performedAnyReplacement = true;

			string output = a_configString;
			while (performedAnyReplacement)
			{
				performedAnyReplacement = false;
				MatchCollection matches = regex.Matches(output);

				foreach (Match match in matches)
				{
					string targetValue = match.Groups[1].Value;
					if (m_replacementsByKey.TryGetValue(targetValue, out string? replacement))
					{
						output = output.Replace(match.Value, replacement);
						performedAnyReplacement = true;
					}
				}
			}

			return output;
		}
	}
}
