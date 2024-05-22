namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;

internal static class RollingHasher
{
	public static ulong PushChar(ulong hash, char c)
	{
		hash = LeftShiftModP(hash, 8 * sizeof(char));

		hash += c;

		if (hash >= P)
		{
			hash -= P;
		}

		return hash;
	}

	public static ulong PopChar(ulong hash, char c)
	{
		hash += P - c;

		if (hash >= P)
		{
			hash -= P;
		}

		return RightShiftModP(hash, 8 * sizeof(char));
	}

	public static ulong PushString(ulong hash, string str)
	{
		foreach (char c in str)
		{
			hash = PushChar(hash, c);
		}

		return hash;
	}

	public static ulong PopString(ulong hash, string str)
	{
		foreach (char c in Enumerable.Reverse(str))
		{
			hash = PopChar(hash, c);
		}

		return hash;
	}

	private static ulong LeftShiftModP(ulong hash, int i)
	{
		while (i-- != 0)
		{
			hash <<= 1;
			if (hash >= P)
			{
				hash -= P;
			}
		}

		return hash;
	}

	private static ulong RightShiftModP(ulong hash, int i)
	{
		while (i-- != 0)
		{
			if ((hash & 1UL) == 1UL)
			{
				hash += P;
			}
			hash >>= 1;
		}

		return hash;
	}

	private const ulong P = 0x7fffffffffffffe7;
}