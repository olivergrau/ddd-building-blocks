namespace RocketLaunch.ReadModel.Core.Exceptions
{
	public class ReadModelWrongStateException : Exception
	{
		public ReadModelWrongStateException(string message, Exception? inner = null)
			: base(message, inner)
		{
		}
	}
}
