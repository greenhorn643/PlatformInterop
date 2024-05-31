using Nito.Collections;
using System.Text.Json;

namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal class ClientSerializer : IInteropClientSerializer
{
	public class JsonDeserializationException(string message) : Exception(message) { }
	public class MethodNotFoundException(string message) : Exception(message) { }
	public class InvalidPacketException(string message) : Exception(message) { }



	public DeserializationResult<InteropResponse> DeserializeResponse(Deque<byte> buffer, Type returnType)
	{
		if (!LengthPrefix.TryPopPacket(buffer, out var packet))
		{
			return DeserializeInsufficentData<InteropResponse>();
		}

		var sections = LengthPrefix.Unpackage(packet!);

		if (sections.Length == 0)
		{
			throw new InvalidPacketException($"packet contains no data");
		}

		var safeResp = JsonSerializer.Deserialize<JsonSafeResponse>(sections[0])
			?? throw new JsonDeserializationException(nameof(JsonSafeResponse));

		if (safeResp.IsSuccess && returnType != typeof(void))
		{
			verifySectionCount(2);

			var value = JsonSerializer.Deserialize(sections[1], returnType);

			var resp = new InteropResponse
			{
				CallerId = safeResp.CallerId,
				IsSuccess = true,
				ErrorMessage = null,
				Value = value,
			};

			return DeserializeSuccess(resp);
		}
		else
		{
			verifySectionCount(1);

			var resp = new InteropResponse
			{
				CallerId = safeResp.CallerId,
				IsSuccess = safeResp.IsSuccess,
				ErrorMessage = safeResp.ErrorMessage,
				Value = null,
			};

			return DeserializeSuccess(resp);
		}

		void verifySectionCount(int expectedCount)
		{
			if (sections.Length != expectedCount)
			{
				throw new InvalidPacketException($"expected {expectedCount} packet sections; found {sections.Length}");
			}
		}
	}

	public byte[] SerializeRequest(InteropRequest req)
	{
		return LengthPrefix.Package([
			JsonSerializer.SerializeToUtf8Bytes(req.CallerId),
			JsonSerializer.SerializeToUtf8Bytes(new JsonSafeMethodInfo(req.MethodInfo)),
			.. Enumerable.Range(0, req.Args.Length)
				.Select(i => JsonSerializer.SerializeToUtf8Bytes(req.Args[i], req.MethodInfo.ArgumentTypes[i])),
		]);
	}
	private static DeserializationResult<T> DeserializeInsufficentData<T>()
	{
		return new DeserializationResult<T>
		{
			ResultType = DeserializationResultType.InsufficientData
		};
	}
	private static DeserializationResult<T> DeserializeSuccess<T>(T value)
	{
		return new DeserializationResult<T>
		{
			ResultType = DeserializationResultType.Success,
			Value = value,
		};
	}

	public DeserializationResult<string> DeserializeCallerId(Deque<byte> buffer)
	{
		if (!LengthPrefix.TryPeekPacket(buffer, out var packet))
		{
			return DeserializeInsufficentData<string>();
		}

		var sections = LengthPrefix.Unpackage(packet!);

		if (sections.Length == 0)
		{
			throw new InvalidPacketException($"packet contains no data");
		}

		var safeResp = JsonSerializer.Deserialize<JsonSafeResponse>(sections[0])
			?? throw new JsonDeserializationException(nameof(JsonSafeResponse));

		return DeserializeSuccess(safeResp.CallerId);
	}
}

