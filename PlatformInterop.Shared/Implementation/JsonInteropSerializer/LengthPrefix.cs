namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal static class LengthPrefix
{
	public class InvalidLengthException : Exception { }


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

	public static bool HasFullPacket(List<byte> buffer)
	{
		if (buffer.Count < 4)
		{
			return false;
		}

		int offset = 0;
		int packetLength = DecodeInt32(buffer, ref offset);

		if (packetLength < buffer.Count)
		{
			throw new InvalidLengthException();
		}

		return packetLength == buffer.Count;
	}

	public static byte[][] Unpackage(List<byte> buffer)
	{
		if (buffer.Count < 4)
		{
			throw new InvalidLengthException();
		}

		int offset = 0;
		int packetLength = DecodeInt32(buffer, ref offset);

		if (packetLength != buffer.Count)
		{
			throw new InvalidLengthException();
		}

		List<byte[]> sections = [];

		while (offset < buffer.Count)
		{
			sections.Add(TakeLengthPrefixedChunk(buffer, ref offset));
		}

		if (offset != buffer.Count)
		{
			throw new InvalidLengthException();
		}

		return [.. sections];
	}

	private static byte[] TakeLengthPrefixedChunk(List<byte> buffer, ref int offset)
	{
		if (sizeof(int) + offset > buffer.Count)
		{
			throw new InvalidLengthException();
		}

		int chunkSize = DecodeInt32(buffer, ref offset);

		if (chunkSize <= 0 || chunkSize + offset > buffer.Count)
		{
			throw new InvalidLengthException();
		}

		var chunk = new byte[chunkSize];
		buffer.CopyTo(offset, chunk, 0, chunkSize);
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

	private static int DecodeInt32(List<byte> buffer, ref int offset)
	{
		int x = buffer[offset]
			| buffer[offset + 1] << 8
			| buffer[offset + 2] << 16
			| buffer[offset + 3] << 24;
		offset += 4;
		return x;
	}
}
