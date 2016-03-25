//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

namespace Victornet.Common
{
    /// <summary>
    /// 设置管理器接口
    /// </summary>
    /// <typeparam name="TSettingsEntity"></typeparam>
    public interface ISettingsManager<TSettingsEntity> where TSettingsEntity : class
    {
        /// <summary>
        /// 获取设置
        /// </summary>
        /// <returns>settings</returns>
        TSettingsEntity Get();

        /// <summary>
        /// 保存设置
        /// </summary>
        /// <param name="settings">settings</param>
        void Save(TSettingsEntity settings);
    }
}