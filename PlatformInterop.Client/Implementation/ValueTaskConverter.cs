using PlatformInterop.Shared;

namespace PlatformInterop.Client.Implementation
{
	public static class ValueTaskConverter
	{
		public static async Task<TResult> Convert<TResult>(Task<object?> t)
		{
			var r = await t;
			if (r is TResult result)
			{
				return result;
			}
			else
			{
				throw new PlatformInteropException($"{nameof(ValueTaskConverter)} {typeof(TResult).Name}");
			}
		}

		public static async Task<bool> ConvertToBool(Task<object?> t)
		{
			var r = await t;
			if (r is bool result)
			{
				return result;
			}
			else
			{
				throw new PlatformInteropException($"{nameof(ValueTaskConverter)} {typeof(bool).Name}");
			}
		}
	}
}
