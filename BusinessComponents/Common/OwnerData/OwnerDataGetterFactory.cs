//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Victornet.Common.Repositories;

namespace Victornet.Common
{
    /// <summary>
    /// 评论URL获取器工厂
    /// </summary>
    public static class OwnerDataGetterFactory
    {
        /// <summary>
        /// 依据tenantTypeId获取OwnerDatalGetterFactory
        /// </summary>
        /// <returns></returns>
        public static IOwnerDataGetter Get(string dataKey)
        {
            return DIContainer.Resolve<IEnumerable<IOwnerDataGetter>>().Where(g => g.DataKey.Equals(dataKey, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }
    }
}
