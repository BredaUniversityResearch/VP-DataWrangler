namespace CommonLogging;

public class LogMessage
{
	public string Source { get; }
	public ELogSeverity Severity { get; }
	public string Message { get; }

	public LogMessage(string a_source, ELogSeverity a_severity, string a_message)
	{
		Source = a_source;
		Severity = a_severity;
		Message = a_message;
	}
};