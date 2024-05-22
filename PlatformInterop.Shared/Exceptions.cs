namespace PlatformInterop.Shared;

public class PlatformInteropException(string errorMessage)
	: Exception(errorMessage)
{ }