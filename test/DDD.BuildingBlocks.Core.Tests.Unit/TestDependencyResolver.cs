namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Collections.Generic;

    public class TestDependencyResolver : IDependencyResolver
	{
		private readonly Dictionary<string, object> _objectDict = new();

        public void AddObject(string key, object value)
		{
			_objectDict.Add(key, value);
		}

		public T? Resolve<T>() where T : notnull
		{
			throw new NotImplementedException();
		}

		public object Resolve(string name, Type type)
		{
			throw new NotImplementedException();
		}

		public object? Resolve(Type type)
        {
            return _objectDict.TryGetValue(type.ToString(), out var value) ? value : null;
        }
	}
}
