namespace CommonLogging;

public class LogMessage
{
	public TimeOnly Time { get; }
	public string Source { get; }
	public ELogSeverity Severity { get; }
	public string Message { get; }

	public LogMessage(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
	{
		Time = a_time;
		Source = a_source;
		Severity = a_severity;
		Message = a_message;
	}
};