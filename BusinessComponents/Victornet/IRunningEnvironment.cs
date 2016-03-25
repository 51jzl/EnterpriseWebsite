using System;
namespace Victornet
{
	public interface IRunningEnvironment
	{
		bool IsFullTrust
		{
			get;
		}
		void RestartAppDomain();
	}
}
