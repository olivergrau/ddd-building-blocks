using System;
using System.Diagnostics.CodeAnalysis;

namespace DDD.BuildingBlocks.Core.Util
{
    public static class ApplicationTime
    {
        public static DateTime Current => _dateSetter();

        private static Func<DateTime> _dateSetter = () => DateTime.UtcNow;

		[ExcludeFromCodeCoverage]
        public static void Set(Func<DateTime> dateSetter)
        {
            _dateSetter = dateSetter;
        }

        [ExcludeFromCodeCoverage]
		public static void Reset()
        {
            _dateSetter = () => DateTime.UtcNow;
        }
    }
}