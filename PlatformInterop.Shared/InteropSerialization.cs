using System.Reflection;
using Buffer = ByteBuffer.ByteBuffer;

namespace PlatformInterop.Shared;

public enum DeserializationResultType
{
	Success,
	InsufficientData,
}

public class DeserializationResult<T>
{
	public DeserializationResultType ResultType { get; set; }
	public T? Value { get; set; }
}

public interface IInteropClientSerializer
{
	byte[] SerializeRequest(InteropRequest req);
	DeserializationResult<string> DeserializeCallerId(Buffer buffer);
	DeserializationResult<InteropResponse> DeserializeResponse(Buffer buffer, Type ReturnType);
}

public interface IInteropHostSerializer
{
	DeserializationResult<string> DeserializeCallerId(Buffer buffer);
	DeserializationResult<(InteropRequest, MethodInfo)> DeserializeRequest(Buffer buffer);
	byte[] SerializeResponse(InteropResponse resp, Type ReturnType);
}

public class InteropRequest
{
	public required string CallerId { get; set; }
	public required InteropMethodInfo MethodInfo { get; set; }
	public required object?[] Args { get; set; }
}

public class InteropResponse
{
	public required string CallerId { get; set; }
	public bool IsSuccess { get; set; }
	public string? ErrorMessage { get; set; }
	public object? Value { get; set; }
}

public class InteropMethodInfo
{
	public static InteropMethodInfo FromFullMethodInfo(MethodInfo methodInfo)
	{
		return new InteropMethodInfo
		{
			MethodName = methodInfo.Name,
			ArgumentTypes = methodInfo.GetParameters().Select(_ => _.ParameterType).ToArray(),
			ReturnType = methodInfo.ReturnType,
		};
	}

	public required string MethodName { get; set; }
	public required Type[] ArgumentTypes { get; set; }
	public required Type ReturnType { get; set; }
}