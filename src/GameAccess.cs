using System;
using System.Reflection;
using HarmonyLib;

namespace SonicChestFilters
{
	internal static class GameAccess
	{
		internal static T Get<T>(object instance, string fieldName) where T : class
		{
			if (instance == null)
			{
				return null;
			}
			return AccessTools.Field(instance.GetType(), fieldName)?.GetValue(instance) as T;
		}

		internal static object Get(object instance, string fieldName)
		{
			if (instance == null)
			{
				return null;
			}
			return AccessTools.Field(instance.GetType(), fieldName)?.GetValue(instance);
		}

		internal static void Call(object instance, string methodName, params object[] args)
		{
			if (instance == null)
			{
				return;
			}

			MethodInfo method = AccessTools.Method(instance.GetType(), methodName);
			if (method == null)
			{
				return;
			}

			method.Invoke(instance, BindArguments(method, args));
		}

		private static object[] BindArguments(MethodInfo method, object[] args)
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 0)
			{
				return null;
			}

			object[] bound = new object[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				if (args != null && i < args.Length)
				{
					bound[i] = args[i];
					continue;
				}

				if (parameters[i].HasDefaultValue)
				{
					bound[i] = parameters[i].DefaultValue;
					continue;
				}

				Type type = parameters[i].ParameterType;
				bound[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
			}

			return bound;
		}
	}
}
