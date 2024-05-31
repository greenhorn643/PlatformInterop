using PlatformInterop.Client;
using Test.Shared;

var (serviceUnderTest, disposer) = InteropClient.CreateClient<IClientService>("Test.PersonServer.exe");

int nItems = 100000;

var t0 = DateTime.Now;

for (int i = 0; i < nItems; i++)
{
	await serviceUnderTest.AddPersonAsync(new Person("alex", "palmer", 33, null));
}

var t1 = DateTime.Now;

var elapsed = t1 - t0;

Console.WriteLine($"wrote {nItems} Person objects in {elapsed.TotalSeconds} seconds");

t0 = DateTime.Now;

var people = await serviceUnderTest.GetPeopleAsync();

t1 = DateTime.Now;

elapsed = t1 - t0;

Console.WriteLine($"read {people.Count} Person objects in {elapsed.TotalSeconds} seconds");