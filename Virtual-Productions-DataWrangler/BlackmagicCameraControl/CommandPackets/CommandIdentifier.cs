namespace BlackmagicCameraControl.CommandPackets
{
	public struct CommandIdentifier
	{
		public readonly byte Category;
		public readonly byte Parameter;

		public CommandIdentifier(byte a_category, byte a_parameter)
		{
			Category = a_category;
			Parameter = a_parameter;
		}

		public override int GetHashCode()
		{
			return Category | (Parameter >> 8);
		}

		public override string ToString()
		{
			return $"[Command: {Category}:{Parameter}]";
		}
	}
}
