using System;
using System.Collections.Generic;
namespace Victornet.Events
{
	public delegate void BatchEventHandler<S, A>(System.Collections.Generic.IEnumerable<S> senders, A eventArgs);
}
