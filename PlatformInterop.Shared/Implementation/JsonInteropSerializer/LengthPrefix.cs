using Nito.Collections;

namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal static class LengthPrefix
{
	public class InvalidLengthException(string what, int expectedLength, int actualLength)
		: Exception($"{what} | expected length: {expectedLength} -- actual length: {actualLength}")
	{ }


	public static byte[] Package(byte[][] sections)
	{
		List<byte> buffer = [0x00, 0x00, 0x00, 0x00];

		void addToBuffer(byte[] bytes)
		{
			buffer.AddRange(EncodeInt32(bytes.Length).AsSpan());
			buffer.AddRange(bytes.AsSpan());
		}

		foreach (var section in sections)
		{
			addToBuffer(section);
		}

		var totalLengthBytes = EncodeInt32(buffer.Count);
		for (int i = 0; i < 4; i++)
		{
			buffer[i] = totalLengthBytes[i];
		}
		return [.. buffer];
	}

	public static bool TryPopPacket(Deque<byte> buffer, out byte[]? packet)
	{
		if (TryPeekPacket(buffer, out packet))
		{
			buffer.RemoveRange(0, packet!.Length);
			return true;
		}
		return false;
	}

	public static bool TryPeekPacket(Deque<byte> buffer, out byte[]? packet)
	{
		if (buffer.Count < 4)
		{
			packet = null;
			return false;
		}

		int offset = 0;
		int packetLength = DecodeInt32(buffer, ref offset);

		if (packetLength > buffer.Count)
		{
			packet = null;
			return false;
		}

		packet = buffer.Take(packetLength).ToArray();
		return true;
	}

	public static byte[][] Unpackage(byte[] packet)
	{
		if (packet.Length < 4)
		{
			throw new InvalidLengthException(
				nameof(Unpackage) + " length prefix",
				4,
				packet.Length);
		}

		int offset = 0;
		int packetLength = DecodeInt32(packet, ref offset);

		if (packetLength > packet.Length)
		{
			throw new InvalidLengthException(
				nameof(Unpackage) + " packet length",
				packetLength,
				packet.Length);
		}

		List<byte[]> sections = [];

		while (offset < packet.Length)
		{
			sections.Add(TakeLengthPrefixedChunk(packet, ref offset));
		}

		if (offset != packetLength)
		{
			throw new InvalidLengthException(
				nameof(Unpackage) + " bytes consumed",
				packet.Length,
				offset);
		}

		return [.. sections];
	}

	private static byte[] TakeLengthPrefixedChunk(byte[] packet, ref int offset)
	{
		if (sizeof(int) + offset > packet.Length)
		{
			throw new InvalidLengthException(
				nameof(TakeLengthPrefixedChunk) + " length prefix",
				sizeof(int) + offset,
				packet.Length);
		}

		int chunkSize = DecodeInt32(packet, ref offset);

		if (chunkSize <= 0 || chunkSize + offset > packet.Length)
		{
			throw new InvalidLengthException(
				nameof(TakeLengthPrefixedChunk) + " chunk size",
				packet.Length,
				chunkSize);
		}

		var chunk = packet.AsSpan().Slice(offset, chunkSize).ToArray();
		offset += chunkSize;
		return chunk;
	}

	private static byte[] EncodeInt32(int x)
	{
		return [
			(byte)x,
				(byte)(x >> 8),
				(byte)(x >> 16),
				(byte)(x >> 24)];
	}

	private static int DecodeInt32<TBuffer>(TBuffer buffer, ref int offset)
		where TBuffer : IList<byte>
	{
		int x = buffer[offset]
			| buffer[offset + 1] << 8
			| buffer[offset + 2] << 16
			| buffer[offset + 3] << 24;
		offset += 4;
		return x;
	}
}
