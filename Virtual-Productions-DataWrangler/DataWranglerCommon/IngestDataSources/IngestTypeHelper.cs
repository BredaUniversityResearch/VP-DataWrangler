using System.Reflection;

namespace DataWranglerCommon.IngestDataSources
{
	internal static class IngestTypeHelper
	{
		public static List<Type> FindTypesInheritingFrom(Type a_parentType)
		{
			List<Type> resultTypes = new();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (!type.IsAbstract && type.IsAssignableTo(a_parentType))
					{
						resultTypes.Add(type);
					}
				}
			}

			return resultTypes;
		}
	}
}
