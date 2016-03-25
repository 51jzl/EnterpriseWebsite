//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Victornet.Common;

namespace Victornet.Search
{
    /// <summary>
    /// 计数类型扩展
    /// </summary>
    public static class CountTypesExtension
    {
        /// <summary>
        /// 搜索次数
        /// </summary>
        public static string SearchCount(this CountTypes countTypes)
        {
            return "SearchCount";
        }
    }
}
