//------------------------------------------------------------------------------
// <copyright company="Victornet">
//     Copyright (c) Victornet Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using Victornet;
using Victornet.Common.Repositories;

namespace Victornet.Common
{
    /// <summary>
    /// 设置管理器
    /// </summary>
    /// <typeparam name="TSettingsEntity"></typeparam>
    public class SettingManager<TSettingsEntity> : ISettingsManager<TSettingsEntity> where TSettingsEntity : class, IEntity, new()
    {
        public ISettingsRepository<TSettingsEntity> repository { get; set; }

        public TSettingsEntity Get()
        {
            return repository.Get();
        }

        public void Save(TSettingsEntity settings)
        {
            repository.Save(settings);
        }
    }
}