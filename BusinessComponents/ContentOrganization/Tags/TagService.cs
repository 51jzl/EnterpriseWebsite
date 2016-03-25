
//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Victornet.Caching;
using Victornet.Common.Repositories;
using Victornet.Events;

namespace Victornet.Common
{
    /// <summary>
    /// 标签业务逻辑类
    /// </summary>
    public class TagService : TagService<Tag>
    {
        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="tenantTypeId">租户类型Id</param>
        public TagService(string tenantTypeId)
            : base(tenantTypeId)
        { }
    }
}
