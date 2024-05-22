namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal class JsonSafeMethodInfo
{
	public JsonSafeMethodInfo() { }

	public JsonSafeMethodInfo(InteropMethodInfo methodInfo)
	{
		MethodName = methodInfo.MethodName;
		ArgumentTypes = [.. methodInfo.ArgumentTypes.Select(_ => _.Name)];
		ReturnType = methodInfo.ReturnType.Name!;
	}

	public string MethodName { get; set; } = string.Empty;
	public string[] ArgumentTypes { get; set; } = [];
	public string ReturnType { get; set; } = string.Empty;
}

internal class JsonSafeResponse
{
	public JsonSafeResponse() { }

	public JsonSafeResponse(InteropResponse resp)
	{
		IsSuccess = resp.IsSuccess;
		ErrorMessage = resp.ErrorMessage;
	}

	public bool IsSuccess { get; set; }
	public string? ErrorMessage { get; set; }
}