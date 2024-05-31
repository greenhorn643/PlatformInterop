using Test.Shared;

namespace Test.PersonServer;

internal class ClientService : IClientService
{
	private readonly List<Person?> people = [];
	private readonly object syncRoot = new();

	public Task<bool> IsServiceRunningAs64BitProcess()
	{
#if DEBUG
		Console.WriteLine(nameof(IsServiceRunningAs64BitProcess));
#endif
		var result = Environment.Is64BitProcess;
		return Task.FromResult(result);
	}

	public Task<int> AddPersonAsync(Person person)
	{
#if DEBUG
		Console.WriteLine(nameof(AddPersonAsync));
#endif
		lock (syncRoot)
		{
			int id = people.Count;
			people.Add(person);
			return Task.FromResult(id);
		}
	}

	public Task RemovePersonAsync(int id)
	{
#if DEBUG
		Console.WriteLine(nameof(RemovePersonAsync));
#endif
		lock (syncRoot)
		{
			if (id >= 0 && id < people.Count)
			{
				people[id] = null;
			}
			return Task.CompletedTask;
		}
	}

	public Task<Person?> GetPersonAsync(int id)
	{
#if DEBUG
		Console.WriteLine(nameof(GetPersonAsync));
#endif
		lock (syncRoot)
		{
			if (id >= 0 && id <= people.Count)
			{
				return Task.FromResult(people[id]);
			}
			return Task.FromResult<Person?>(null);
		}
	}

	public Task<List<Person>> GetPeopleAsync()
	{
#if DEBUG
		Console.WriteLine(nameof(GetPeopleAsync));
#endif
		lock (syncRoot)
		{
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			return Task.FromResult(people.Where(_ => _ is not null).ToList());
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
		}
	}
}
