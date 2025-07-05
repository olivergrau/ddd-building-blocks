namespace RocketLaunch.ReadModel.Core.Exceptions
{
	public class ReadModelException : System.Exception
	{
		public ReadModelException(string message, System.Exception? inner = null)
			: base(message, inner)
		{
		}
	}
}