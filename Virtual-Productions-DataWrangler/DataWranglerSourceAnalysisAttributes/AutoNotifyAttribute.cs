namespace AutoNotify
{
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class AutoNotifyAttribute : Attribute
	{
		public AutoNotifyAttribute()
		{
		}
		public string PropertyName { get; set; } = "";

	}

	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class AutoNotifyPropertyAttribute : Attribute
	{
		public string BackingFieldName { get; set; }
		public AutoNotifyPropertyAttribute(string a_backingFieldName)
		{
			BackingFieldName = a_backingFieldName;
		}
	}
}