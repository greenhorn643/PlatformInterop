using Test.Shared;

namespace Test.PersonServer;

internal class ClientService : IClientService
{
	private readonly List<Person> people = [];

	public Task AddPersonAsync(Person person)
	{
		people.Add(person);
		return Task.CompletedTask;
	}

	public Task<Person[]> GetPeopleAsync()
	{
		return Task.FromResult(people.ToArray());
	}
}
