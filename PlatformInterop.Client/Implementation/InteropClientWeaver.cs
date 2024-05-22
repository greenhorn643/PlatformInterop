using PlatformInterop.Shared;
using System.Reflection;
using System.Reflection.Emit;

namespace PlatformInterop.Client.Implementation;

internal static class InteropClientWeaver
{
	private static readonly AssemblyBuilder DynamicAssembly;
	private static readonly ModuleBuilder DynamicModule;

	static InteropClientWeaver()
	{
		var assemblyName = new AssemblyName(
			CreateUniqueName(nameof(InteropClientWeaver)));

		DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
			assemblyName,
			AssemblyBuilderAccess.Run);

		DynamicModule = DynamicAssembly.DefineDynamicModule(
			CreateUniqueName(nameof(InteropClientWeaver)));
	}

	public static Type CreateClientType(Type clientInterface)
	{
		var typeBuilder = DefineType(clientInterface);

		DefineConstructor(typeBuilder);

		var methods = GetMethods(clientInterface);

		foreach (var method in methods)
		{
			DefineMethod(typeBuilder, method);
		}

		return typeBuilder.CreateType();
	}

	private static TypeBuilder DefineType(Type clientInterface)
	{
		if (!clientInterface.IsInterface)
		{
			throw new ArgumentException("expected interface", nameof(clientInterface));
		}

		var name = CreateUniqueName(clientInterface.Name);
		var typeBuilder = DynamicModule.DefineType(name);

		typeBuilder.AddInterfaceImplementation(clientInterface);
		typeBuilder.SetParent(typeof(InteropClientBase));

		return typeBuilder;
	}

	private static void DefineConstructor(TypeBuilder typeBuilder)
	{
		Type[] ctorArgList = [typeof(IInteropChannel), typeof(IInteropClientSerializer)];

		var constructor = typeBuilder.DefineConstructor(
			MethodAttributes.Public,
			CallingConventions.Standard,
			ctorArgList);

		var baseConstructor = typeof(InteropClientBase).GetConstructor(ctorArgList)!;

		var constructorGenerator = constructor.GetILGenerator();
		constructorGenerator.Emit(OpCodes.Ldarg_0);
		constructorGenerator.Emit(OpCodes.Ldarg_1);
		constructorGenerator.Emit(OpCodes.Ldarg_2);
		constructorGenerator.Emit(OpCodes.Call, baseConstructor);
		constructorGenerator.Emit(OpCodes.Ret);
	}

	private static string CreateUniqueName(string prefix)
	{
		return $"{prefix}_{Guid.NewGuid().ToString().Replace('-', '_')}";
	}

	private static MethodInfo[] GetMethods(Type clientInterface)
	{
		var methods = clientInterface
			.GetMethods();

		if (!methods.All(IsAsyncMethod))
		{
			throw new ArgumentException(
				"interface must expose only async methods",
				nameof(clientInterface));
		}

		if (methods.Any(_ => _.IsStatic))
		{
			throw new ArgumentException(
				"static interface methods are not supported",
				nameof(clientInterface));
		}

		return methods;
	}

	private static bool IsAsyncMethod(MethodInfo method)
	{
		return method.ReturnType == typeof(Task)
			|| (method.ReturnType.IsGenericType
			&& method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
	}

	private static void DefineMethod(
		TypeBuilder typeBuilder,
		MethodInfo method)
	{
		var methodId = InteropClientBase.RegisterMethod(method);

		Type[] paramTypes = [.. method.GetParameters().Select(_ => _.ParameterType)];

		var methodBuilder = typeBuilder.DefineMethod(
			method.Name,
			MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
			method.ReturnType,
			paramTypes);

		var requestAsyncMethod = typeof(InteropClientBase)
			.GetMethod(nameof(InteropClientBase.RequestAsync))!;

		var methodGenerator = methodBuilder.GetILGenerator();

		methodGenerator.DeclareLocal(typeof(object[]));

		methodGenerator.Emit(OpCodes.Ldc_I4, paramTypes.Length);
		methodGenerator.Emit(OpCodes.Newarr, typeof(object));
		methodGenerator.Emit(OpCodes.Stloc_0);

		bool extraLocalAllocated = false;
		for (int i = 0; i < paramTypes.Length; i++)
		{
			if (paramTypes[i].IsValueType)
			{
				if (!extraLocalAllocated)
				{
					methodGenerator.DeclareLocal(typeof(object[]));
					extraLocalAllocated = true;
				}

				methodGenerator.Emit(OpCodes.Ldarg, i + 1);
				methodGenerator.Emit(OpCodes.Box, paramTypes[i]);
				methodGenerator.Emit(OpCodes.Stloc_1);
				methodGenerator.Emit(OpCodes.Ldloc_0);
				methodGenerator.Emit(OpCodes.Ldc_I4, i);
				methodGenerator.Emit(OpCodes.Ldloc_1);
				methodGenerator.Emit(OpCodes.Stelem, typeof(object));
			}
			else
			{
				methodGenerator.Emit(OpCodes.Ldloc_0);
				methodGenerator.Emit(OpCodes.Ldc_I4, i);
				methodGenerator.Emit(OpCodes.Ldarg, i + 1);
				methodGenerator.Emit(OpCodes.Stelem, typeof(object));
			}
		}

		methodGenerator.Emit(OpCodes.Ldarg_0);
		methodGenerator.Emit(OpCodes.Ldstr, methodId);
		methodGenerator.Emit(OpCodes.Ldloc_0);

		methodGenerator.Emit(OpCodes.Call, requestAsyncMethod);
		methodGenerator.Emit(OpCodes.Ret);

		typeBuilder.DefineMethodOverride(
			methodBuilder,
			method);
	}
}
