using System.Reflection;

namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal class MethodLocator(Type clientInterface)
{
	private readonly IReadOnlyDictionary<ulong, MethodInfo> methodCache = CreateMethodCache(clientInterface);

	public bool TryLocateMethod(JsonSafeMethodInfo safeMethodInfo, out MethodInfo? methodInfo)
	{
		return methodCache.TryGetValue(HashMethodSignature(safeMethodInfo), out methodInfo);
	}

	private static ulong HashMethodSignature(MethodInfo methodInfo)
	{
		return HashMethodSignature(new JsonSafeMethodInfo(
			InteropMethodInfo.FromFullMethodInfo(methodInfo)));
	}

	private static ulong HashMethodSignature(JsonSafeMethodInfo methodInfo)
	{
		return RollingHasher.PushString(
			SeedHash,
			string.Join("-", [
				methodInfo.MethodName,
				.. methodInfo.ArgumentTypes,
				methodInfo.ReturnType,
				]));
	}

	private static IReadOnlyDictionary<ulong, MethodInfo> CreateMethodCache(Type clientInterface)
	{
		Dictionary<ulong, MethodInfo> methodCache = [];
		foreach (var methodInfo in clientInterface.GetMethods())
		{
			ulong hash = HashMethodSignature(methodInfo);
			methodCache.Add(hash, methodInfo);
		}
		return methodCache;
	}

	private const ulong SeedHash = 0xa0f42cd9365be345;
}
