﻿//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using Victornet.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Victornet.Common.Configuration;
using Victornet.Common;

namespace Victornet.Common
{
    /// <summary>
    /// 定期移除过期的推荐内容的任务
    /// </summary>
    public class DeleteExpiredRecommendItemsTask : ITask
    {
        /// <summary>
        /// 任务执行的内容
        /// </summary>
        /// <param name="taskDetail">任务配置状态信息</param>
        public void Execute(TaskDetail taskDetail)
        {
            RecommendService recommendService = new RecommendService();
            recommendService.DeleteExpiredRecommendItems();
        }
    }
}
