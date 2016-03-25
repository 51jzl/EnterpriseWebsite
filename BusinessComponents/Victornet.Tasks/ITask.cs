using System;
namespace Victornet.Tasks
{
	public interface ITask
	{
		void Execute(TaskDetail taskDetail = null);
	}
}
