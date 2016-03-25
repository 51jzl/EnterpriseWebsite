//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Victornet.Common
{
    /// <summary>
    /// 提醒信息
    /// </summary>
    [Serializable]
    public class ReminderInfo
    {
        /// <summary>
        /// 提醒对象Id
        /// </summary>
        public long ObjectId { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime DateCreated { get; set; }
    }
}
