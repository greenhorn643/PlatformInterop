namespace Test.Shared;


public record Address(string Street, int Number, int? Postcode);

public record Person(string FirstName, string LastName, int Age, Address? Address);

public interface IClientService
{
	Task<bool> IsServiceRunningAs64BitProcess();

	Task<int> AddPersonAsync(Person person);

	Task RemovePersonAsync(int id);

	Task<Person?> GetPersonAsync(int id);

	Task<List<Person>> GetPeopleAsync();
}