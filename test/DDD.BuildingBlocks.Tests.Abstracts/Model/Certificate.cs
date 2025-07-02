using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
	public class Certificate : ValueObject<Certificate>
	{
		public Certificate(string prefix, string code)
		{
			if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(prefix));
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(code));
            }

            Prefix = prefix;
			Code = code;
		}

		public string Code
		{
			get;
		}
		public string Prefix
		{
			get;
		}

		protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
		{
			return new[] { Code, Prefix };
		}

		public override string ToString()
		{
			return $"Code:{Code},Prefix:{Prefix}";
		}
	}
}
