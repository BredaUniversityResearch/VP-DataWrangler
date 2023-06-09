using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityCacheSearchCondition
	{
		public readonly ShotGridEntityTypeInfo TargetEntityTypeInfo;
		private readonly List<FieldInfo> m_fieldPath = new List<FieldInfo>(4);
		private object m_targetValue;

		public ShotGridEntityCacheSearchCondition(ShotGridEntityTypeInfo a_targetEntityTypeInfo, ShotGridSearchCondition a_searchConditions)
		{
			if (a_searchConditions.Condition != "is")
			{
				throw new Exception($"Search condition of type \"{a_searchConditions.Condition}\" is not supported");
			}

			TargetEntityTypeInfo = a_targetEntityTypeInfo;
			m_targetValue = a_searchConditions.Value;

			Type fieldOwnerType = TargetEntityTypeInfo.ImplementedType;
			string fieldString = a_searchConditions.Field;
			int separatorIndexLast = 0;
			do
			{
				int separatorIndex = fieldString.IndexOf('.', separatorIndexLast);
				string fieldName = fieldString.Substring(separatorIndexLast, (separatorIndex == -1 ? fieldString.Length : separatorIndex) - separatorIndexLast);

				//Upper case seems to refer to a type cast, so we will just ignore it.
				if (!char.IsUpper(fieldName[0]))
				{
					List<FieldInfo> fieldPathContinuation = new List<FieldInfo>(4);
					if (FindFieldByJsonName(fieldOwnerType, fieldName, fieldPathContinuation))
					{
						fieldOwnerType = fieldPathContinuation[0].FieldType;
						fieldPathContinuation.Reverse();
						m_fieldPath.AddRange(fieldPathContinuation);
					}
				}

				separatorIndexLast = separatorIndex + 1;
			} while (separatorIndexLast != 0);
		}

		private bool FindFieldByJsonName(Type a_targetType, string a_fieldName, List<FieldInfo> a_fieldPath)
		{
			bool result = false;
			FieldInfo[] fieldsOnType = a_targetType.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo field in fieldsOnType)
			{
				if (field.FieldType == typeof(ShotGridEntityRelationships))
				{
					result = FindFieldByJsonName(field.FieldType, a_fieldName, a_fieldPath);
					if (result)
					{
						a_fieldPath.Add(field);
						break;
					}
				}
				else
				{
					JsonPropertyAttribute? jsonAttr = field.GetCustomAttribute<JsonPropertyAttribute>();
					if (jsonAttr != null && jsonAttr.PropertyName == a_fieldName)
					{
						result = true;
						a_fieldPath.Add(field);
						break;
					}
				}
			}

			if (result == false && a_targetType.BaseType != null)
			{
				result = FindFieldByJsonName(a_targetType.BaseType, a_fieldName, a_fieldPath);
			}

			return result;
		}

		public bool Matches(ShotGridEntity a_targetObject)
		{
			object currentTarget = a_targetObject;
			foreach (FieldInfo field in m_fieldPath)
			{
				object? newValue = field.GetValue(currentTarget);
				if (newValue == null)
				{
					throw new Exception($"Failed to walk field paths to destination value. Value returned from {field.Name} on {currentTarget.GetType()} returned null");
				}

				currentTarget = newValue;
			}

			return currentTarget.Equals(m_targetValue);
		}
	}
}
