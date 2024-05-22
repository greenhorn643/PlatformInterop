using System.Reflection;
using System.Text.Json;
using static PlatformInterop.Shared.Implementation.JsonInteropSerializer.ClientSerializer;

namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal class HostSerializer(MethodLocator methodLocator) : IInteropHostSerializer
{
	public DeserializationResult<(MethodInfo, object[])> DeserializeRequest(List<byte> buffer)
	{
		if (!LengthPrefix.HasFullPacket(buffer))
		{
			return DeserializeRequestInsufficentData();
		}

		var sections = LengthPrefix.Unpackage(buffer);

		if (sections.Length == 0)
		{
			throw new InvalidPacketException($"packet contains no data");
		}

		var safeMethodInfo = JsonSerializer.Deserialize<JsonSafeMethodInfo>(sections[0])
			?? throw new JsonDeserializationException(nameof(JsonSafeMethodInfo));

		if (!methodLocator.TryLocateMethod(safeMethodInfo, out var methodInfo))
		{
			throw new MethodNotFoundException(JsonSerializer.Serialize(safeMethodInfo));
		}

		var interopMethodInfo = InteropMethodInfo.FromFullMethodInfo(methodInfo!);

		if (sections.Length != interopMethodInfo.ArgumentTypes.Length + 1)
		{
			throw new InvalidPacketException($"expected {interopMethodInfo.ArgumentTypes.Length} arguments to method"
				+ $" {JsonSerializer.Serialize(safeMethodInfo)}. Found {sections.Length - 1}");
		}

		var args = new object[interopMethodInfo.ArgumentTypes.Length];

		for (int i = 0; i < args.Length; i++)
		{
			args[i] = JsonSerializer.Deserialize(sections[i + 1], interopMethodInfo.ArgumentTypes[i])
				?? throw new JsonDeserializationException(interopMethodInfo.ArgumentTypes[i].Name);
		}

		return DeserializeRequestSuccess(methodInfo!, args);
	}
	public byte[] SerializeResponse(InteropResponse resp, Type returnType)
	{
		if (resp.IsSuccess && returnType != typeof(void))
		{
			return LengthPrefix.Package(
				[ JsonSerializer.SerializeToUtf8Bytes(new JsonSafeResponse(resp)),
				  JsonSerializer.SerializeToUtf8Bytes(resp.Value, returnType),
				]);
		}
		else
		{
			return LengthPrefix.Package(
				[ JsonSerializer.SerializeToUtf8Bytes(new JsonSafeResponse(resp)),
				]);
		}
	}

	private static DeserializationResult<(MethodInfo, object[])> DeserializeRequestInsufficentData()
	{
		return new DeserializationResult<(MethodInfo, object[])>
		{
			ResultType = DeserializationResultType.InsufficientData
		};
	}

	private static DeserializationResult<(MethodInfo, object[])> DeserializeRequestSuccess(MethodInfo methodInfo, object[] args)
	{
		return new DeserializationResult<(MethodInfo, object[])>
		{
			ResultType = DeserializationResultType.Success,
			Value = (methodInfo, args),
		};
	}
}
