//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using PetaPoco;
using Victornet;
using Victornet.Caching;

namespace Victornet.Common
{

    /// <summary>
    /// 封装后台管理用户时用于查询用户的条件
    /// </summary>
    public class TagQuery
    {

        /// <summary>
        /// 审核状态
        /// </summary>
        public PubliclyAuditStatus? PubliclyAuditStatus { get; set; }

        /// <summary>
        ///租户类型Id
        /// </summary>
        public string TenantTypeId { get; set; }

        /// <summary>
        /// 关键字
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        ///是否为特色标签
        /// </summary>
        public bool? IsFeatured { get; set; }
    }
}
