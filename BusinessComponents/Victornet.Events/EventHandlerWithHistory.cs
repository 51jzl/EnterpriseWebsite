using System;
namespace Victornet.Events
{
	public delegate void EventHandlerWithHistory<S, A>(S sender, A eventArgs, S historyData);
}
