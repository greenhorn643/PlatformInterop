using PlatformInterop.Client;
using System.Text.Json;
using Test.Shared;

var (clientService, disposable) = InteropClient.CreateClient<IClientService>(
	"..\\..\\..\\..\\Test.PersonServer\\bin\\Debug\\net8.0-windows\\Test.PersonServer.exe");

using var _ = disposable;

var person = new Person(
	FirstName: "Alex",
	LastName: "Palmer",
	Age: 33);

Console.WriteLine($"Environment.Is64BitProcess: {Environment.Is64BitProcess}");

await clientService.AddPersonAsync(person);
await clientService.AddPersonAsync(person);
var people = await clientService.GetPeopleAsync();

Console.WriteLine(JsonSerializer.Serialize(people));