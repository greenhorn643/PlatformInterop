namespace PlatformInterop.Shared;

public static class TypeExtension
{
	public static Type? GetTaskType(this Type type)
	{
		if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(Task<>))
			{
				return type.GetGenericArguments()[0];
			}
		}

		if (type == typeof(Task))
		{
			return typeof(void);
		}

		return null;
	}
}