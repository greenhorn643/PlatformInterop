using System.Reflection;

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
	byte[] SerializeRequest(InteropMethodInfo methodInfo, object[] args);
	DeserializationResult<InteropResponse> DeserializeResponse(List<byte> buffer, Type ReturnType);
}

public interface IInteropHostSerializer
{
	DeserializationResult<(MethodInfo, object[])> DeserializeRequest(List<byte> buffer);
	byte[] SerializeResponse(InteropResponse resp, Type ReturnType);
}

public class InteropResponse
{
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