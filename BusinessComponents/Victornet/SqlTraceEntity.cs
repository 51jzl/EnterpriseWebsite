using System;
namespace Victornet
{
	public class SqlTraceEntity
	{
		public string Sql
		{
			get;
			set;
		}
		public long ElapsedMilliseconds
		{
			get;
			set;
		}
	}
}
