using PlatformInterop.Client;
using Test.Shared;

namespace Test.Client
{
	[TestClass]
	public class PersonServerPlatformInterop
	{
		private IClientService? serviceUnderTest;
		private IAsyncDisposable? serviceDisposer;

		[TestInitialize]
		public void Setup()
		{
			(serviceUnderTest, serviceDisposer) = InteropClient.CreateClient<IClientService>("Test.PersonServer.exe");
		}


		[TestMethod]
		public void ServiceWasCreated()
		{
			Assert.IsNotNull(serviceUnderTest);
		}

		[TestMethod]
		public async Task HostIsRunning32Bit()
		{
			Assert.IsNotNull(serviceUnderTest);

			bool hostIs64Bit = await serviceUnderTest.IsServiceRunningAs64BitProcess();

			Assert.IsFalse(hostIs64Bit);
		}

		[TestMethod]
		public void ClientIsRunning64Bit()
		{
			Assert.IsTrue(Environment.Is64BitProcess);
		}

		[TestMethod]
		public async Task CanWriteAndReadBackData()
		{
			Assert.IsNotNull(serviceUnderTest);

			var data = new Person[]
			{
				new("Alex", "Palmer", 33, new ("Fake Street", 123, 8014)),
				new("Bilbo", "Baggins", 111, new ("Bag End", 22, null)),
				new("Principal", "Skinner", 46, new ("Springfield Elementary", 5, null)),
			};

			foreach (var person in data)
			{
				await serviceUnderTest.AddPersonAsync(person);
			}

			var dataFromService = await serviceUnderTest.GetPeopleAsync();

			Assert.IsNotNull(dataFromService);
			Assert.AreEqual(dataFromService.Count, data.Length);

			for (int i = 0; i < data.Length; i++)
			{
				Assert.AreEqual(data[i], dataFromService[i]);
			}
		}

		[TestMethod]
		public async Task IsThreadSafe()
		{
			Assert.IsNotNull(serviceUnderTest);

			var nThreads = 8;
			var nItemsPerThread = 1024;

			var data = new Person[nThreads][];

			for (int i = 0; i < nThreads; i++)
			{
				data[i] = new Person[nItemsPerThread];

				for (int j = 0; j < nItemsPerThread; j++)
				{
					data[i][j] = new Person(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i * j, null);
				}
			}

			var mre = new ManualResetEvent(false);

			var threads = new Thread[nThreads];

			for (int i = 0; i < nThreads; i++)
			{
				var threadData = data[i];

				threads[i] = new Thread(() =>
				{
					mre.WaitOne();

					for (int j = 0; j < nItemsPerThread; j++)
					{
						serviceUnderTest.AddPersonAsync(threadData[j]).Wait();
					}
				});
				threads[i].Start();
			}

			mre.Set();

			foreach (var thread in threads)
			{
				thread.Join();
			}

			HashSet<Person> expectedPeople = [.. data.SelectMany(_ => _)];

			var actualPeople = await serviceUnderTest.GetPeopleAsync();

			foreach (Person person in actualPeople)
			{
				Assert.IsTrue(expectedPeople.Remove(person));
			}

			Assert.IsTrue(expectedPeople.Count == 0);
		}


		[TestCleanup]
		public async Task Teardown()
		{
			await serviceDisposer!.DisposeAsync();
			serviceUnderTest = null;
			serviceDisposer = null;
		}
	}
}