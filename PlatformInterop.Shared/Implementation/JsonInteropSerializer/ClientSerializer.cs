using System.Text.Json;

namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal class ClientSerializer : IInteropClientSerializer
{
	public class JsonDeserializationException(string message) : Exception(message) { }
	public class MethodNotFoundException(string message) : Exception(message) { }
	public class InvalidPacketException(string message) : Exception(message) { }



	public DeserializationResult<InteropResponse> DeserializeResponse(List<byte> buffer, Type returnType)
	{
		if (!LengthPrefix.HasFullPacket(buffer))
		{
			return DeserializeResponseInsufficentData();
		}

		var sections = LengthPrefix.Unpackage(buffer);

		if (sections.Length == 0)
		{
			throw new InvalidPacketException($"packet contains no data");
		}

		var safeResp = JsonSerializer.Deserialize<JsonSafeResponse>(sections[0])
			?? throw new JsonDeserializationException(nameof(JsonSafeResponse));

		if (safeResp.IsSuccess && returnType != typeof(void))
		{
			verifySectionCount(2);

			var value = JsonSerializer.Deserialize(sections[1], returnType)
				?? throw new JsonDeserializationException(returnType.Name);

			var resp = new InteropResponse
			{
				IsSuccess = true,
				ErrorMessage = null,
				Value = value,
			};

			return DeserializeResponseSuccess(resp);
		}
		else
		{
			verifySectionCount(1);

			var resp = new InteropResponse
			{
				IsSuccess = safeResp.IsSuccess,
				ErrorMessage = safeResp.ErrorMessage,
				Value = null,
			};

			return DeserializeResponseSuccess(resp);
		}

		void verifySectionCount(int expectedCount)
		{
			if (sections.Length != expectedCount)
			{
				throw new InvalidPacketException($"expected {expectedCount} packet sections; found {sections.Length}");
			}
		}
	}

	public byte[] SerializeRequest(InteropMethodInfo methodInfo, object[] args)
	{
		return LengthPrefix.Package(
			[ JsonSerializer.SerializeToUtf8Bytes(new JsonSafeMethodInfo(methodInfo)),
			.. Enumerable.Range(0, args.Length)
				.Select(i => JsonSerializer.SerializeToUtf8Bytes(args[i], methodInfo.ArgumentTypes[i])),
			]);
	}
	private static DeserializationResult<InteropResponse> DeserializeResponseInsufficentData()
	{
		return new DeserializationResult<InteropResponse>
		{
			ResultType = DeserializationResultType.InsufficientData
		};
	}
	private static DeserializationResult<InteropResponse> DeserializeResponseSuccess(InteropResponse resp)
	{
		return new DeserializationResult<InteropResponse>
		{
			ResultType = DeserializationResultType.Success,
			Value = resp,
		};
	}
}

