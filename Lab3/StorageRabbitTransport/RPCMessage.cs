namespace Services;

/// <summary>
/// Envelope for every request and reply on the wire
/// Same shape as the lecturer's: the method name plus its JSON-encoded arguments or result
/// </summary>
public class RPCMessage
{
	/// <summary>
	/// "Call_&lt;Method&gt;" in requests, "Result_&lt;Method&gt;" in replies
	/// </summary>
	public string Action { get; set; }

	/// <summary>
	/// JSON of the arguments (request) or of the return value (reply). Null if there is none
	/// </summary>
	public string Data { get; set; }
}
