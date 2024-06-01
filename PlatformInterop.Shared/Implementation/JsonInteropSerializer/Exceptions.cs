namespace PlatformInterop.Shared.Implementation.JsonInteropSerializer;
public class JsonDeserializationException(string message) : Exception(message) { }
public class MethodNotFoundException(string message) : Exception(message) { }
public class InvalidPacketException(string message) : Exception(message) { }