namespace RocketLaunch.ReadModel.Core.Exceptions
{
	public class InconsistentReadModelException : System.Exception
	{
		public InconsistentReadModelException(string message, System.Exception? inner = null)
			: base(message, inner)
		{
		}
	}
}