using System.Reflection;
using System.Text.Json;
using Buffer = ByteBuffer.ByteBuffer;

namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal class HostSerializer(MethodLocator methodLocator) : IInteropHostSerializer
{
	public DeserializationResult<string> DeserializeCallerId(Buffer buffer)
	{
		if (!LengthPrefix.TryPeekPacket(buffer, out var packet))
		{
			return DeserializeRequestInsufficentData<string>();
		}

		var sections = LengthPrefix.Unpackage(packet!);

		if (sections.Length == 0)
		{
			throw new InvalidPacketException($"packet contains no data");
		}

		var callerId = JsonSerializer.Deserialize<string>(sections[0])
			?? throw new JsonDeserializationException(nameof(String));

		return DeserializeRequestSuccess(callerId);
	}

	public DeserializationResult<(InteropRequest, MethodInfo)> DeserializeRequest(Buffer buffer)
	{
		if (!LengthPrefix.TryPopPacket(buffer, out var packet))
		{
			return DeserializeRequestInsufficentData<(InteropRequest, MethodInfo)>();
		}

		var sections = LengthPrefix.Unpackage(packet!);

		if (sections.Length == 0)
		{
			throw new InvalidPacketException($"packet contains no data");
		}

		var callerId = JsonSerializer.Deserialize<string>(sections[0])
			?? throw new JsonDeserializationException(nameof(String));

		var safeMethodInfo = JsonSerializer.Deserialize<JsonSafeMethodInfo>(sections[1])
			?? throw new JsonDeserializationException(nameof(JsonSafeMethodInfo));

		if (!methodLocator.TryLocateMethod(safeMethodInfo, out var methodInfo))
		{
			throw new MethodNotFoundException(JsonSerializer.Serialize(safeMethodInfo));
		}

		var interopMethodInfo = InteropMethodInfo.FromFullMethodInfo(methodInfo!);

		if (sections.Length != interopMethodInfo.ArgumentTypes.Length + 2)
		{
			throw new InvalidPacketException($"expected {interopMethodInfo.ArgumentTypes.Length} arguments to method"
				+ $" {JsonSerializer.Serialize(safeMethodInfo)}. Found {sections.Length - 2}");
		}

		var args = new object[interopMethodInfo.ArgumentTypes.Length];

		for (int i = 0; i < args.Length; i++)
		{
			args[i] = JsonSerializer.Deserialize(sections[i + 2], interopMethodInfo.ArgumentTypes[i])
				?? throw new JsonDeserializationException(interopMethodInfo.ArgumentTypes[i].Name);
		}

		var req = new InteropRequest
		{
			CallerId = callerId,
			MethodInfo = interopMethodInfo,
			Args = args,
		};

		return DeserializeRequestSuccess((req, methodInfo!));
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

	private static DeserializationResult<T> DeserializeRequestInsufficentData<T>()
	{
		return new DeserializationResult<T>
		{
			ResultType = DeserializationResultType.InsufficientData
		};
	}

	private static DeserializationResult<T> DeserializeRequestSuccess<T>(T value)
	{
		return new DeserializationResult<T>
		{
			ResultType = DeserializationResultType.Success,
			Value = value,
		};
	}
}
