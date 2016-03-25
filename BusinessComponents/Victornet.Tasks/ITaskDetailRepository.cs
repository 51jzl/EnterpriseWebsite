using System;
using Victornet.Repositories;
namespace Victornet.Tasks
{
	public interface ITaskDetailRepository : IRepository<TaskDetail>
	{
		void SaveTaskStatus(TaskDetail taskDetail);
	}
}
