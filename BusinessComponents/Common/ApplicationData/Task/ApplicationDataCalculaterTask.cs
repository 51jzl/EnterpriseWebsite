//--------------------------------------------------------------
//<version>V0.5</verion>
//<createdate>2012-11-02</createdate>
//<author>mazq</author>
//<email>mazq@Victornet.com</email>
//<log date="2012-11-02" version="0.5">创建</log>
//--------------------------------------------------------------
//</VictornetCopyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Victornet.Tasks;

namespace Victornet.Common
{
    /// <summary>
    /// 定时计算ApplicationData
    /// </summary>
    public class ApplicationDataCalculaterTask : ITask
    {
        /// <summary>
        /// 计算ApplicationData
        /// </summary>
        /// <param name="taskDetail"></param>
        void ITask.Execute(TaskDetail taskDetail)
        {
            IEnumerable<IApplicationDataCalculater> applicationDataCalculaters = DIContainer.Resolve<IEnumerable<IApplicationDataCalculater>>();
            foreach (var applicationDataCalculater in applicationDataCalculaters)
            {
                applicationDataCalculater.Calculate();
            }
        }
    }
}
