using System;
namespace Victornet.Tasks
{
	public interface ITaskScheduler
	{
		void Start();
		void Stop();
		void Update(TaskDetail task);
		void ResumeAll();
		TaskDetail GetTask(int Id);
		void Run(int Id);
		void SaveTaskStatus();
	}
}
