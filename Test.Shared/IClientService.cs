namespace Test.Shared;

public record Person(string FirstName, string LastName, int Age);

public interface IClientService
{
	Task AddPersonAsync(Person person);
	Task<Person[]> GetPeopleAsync();
}
