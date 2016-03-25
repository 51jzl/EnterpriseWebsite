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
    /// 隐私项目分组Id管理类
    /// </summary>
    public class PrivacyItemGroupIds
    {
        #region Instance
        private static PrivacyItemGroupIds _instance = new PrivacyItemGroupIds();
        /// <summary>
        /// 获取单例
        /// </summary>
        /// <returns></returns>
        public static PrivacyItemGroupIds Instance()
        {
            return _instance;
        }

        private PrivacyItemGroupIds()
        { }

        #endregion

        /// <summary>
        /// 个人资料
        /// </summary>
        /// <returns></returns>
        public int Profile()
        {
            return 1;
        }

        /// <summary>
        /// 空间访问
        /// </summary>
        /// <returns></returns>
        public int VisitSpace()
        {
            return 2;
        }

        /// <summary>
        /// 沟通互动
        /// </summary>
        /// <returns></returns>
        public int Interactive()
        {
            return 3;
        }


    }
}