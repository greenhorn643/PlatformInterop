using PlatformInterop.Host;
using Test.PersonServer;

Console.WriteLine("Hello, from Test.PersonServer");
Console.WriteLine($"Environment.Is64BitProcess: {Environment.Is64BitProcess}");

await InteropHost.Run(new ClientService(), args);