using System;
using System.Collections.Generic;
using Victornet.Repositories;
namespace Victornet.Email
{
	public interface IEmailQueueRepository : IRepository<EmailQueueEntry>
	{
		System.Collections.Generic.IEnumerable<EmailQueueEntry> Dequeue(int maxNumber);
	}
}
