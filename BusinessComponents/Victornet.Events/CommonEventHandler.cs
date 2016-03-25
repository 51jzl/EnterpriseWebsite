using System;
namespace Victornet.Events
{
	public delegate void CommonEventHandler<S, A>(S sender, A eventArgs);
}
