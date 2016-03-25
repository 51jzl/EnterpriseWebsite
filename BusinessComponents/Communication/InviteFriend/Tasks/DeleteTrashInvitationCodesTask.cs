//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using Victornet.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Victornet.Common
{
    
    /// <summary>
    /// 每天执行批量删除过期的邀请码任务
    /// </summary>
    public class DeleteTrashInvitationCodesTask : ITask
    {
        /// <summary>
        /// 任务执行的内容
        /// </summary>
        /// <param name="taskDetail">任务配置状态信息</param>
        public void Execute(TaskDetail taskDetail)
        {
            InviteFriendService inviteFriendService = new InviteFriendService();
            inviteFriendService.DeleteTrashInvitationCodes();
        }
    }
}
