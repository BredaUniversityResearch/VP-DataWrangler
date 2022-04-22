namespace ShotGridIntegration;

public class ShotGridAPIException : Exception
{
	public ShotGridAPIException(string a_message)
		: base(a_message)
	{
	}
}