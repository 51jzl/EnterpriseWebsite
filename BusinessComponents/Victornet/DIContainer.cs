using Autofac;
using Autofac.Core;
using System;
using System.Web.Mvc;
namespace Victornet
{
    /// <summary>
    /// 依赖注入容器
    /// </summary>
    /// <remarks>
    /// 对Autofac进行封装
    /// </remarks>
    public class DIContainer
    {
        private static IContainer _container;

        /// <summary>
        /// 注册DIContainer
        /// </summary>
        /// <param name="container">Autofac.IContainer</param>
        public static void RegisterContainer(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// 按参数获取组件
        /// </summary>
        /// <typeparam name="TService">组件类型</typeparam>
        /// <returns>返回获取的组件</returns>
        public static TService Resolve<TService>()
        {
            return ResolutionExtensions.Resolve<TService>(_container);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TService">组件类型</typeparam>
        /// <param name="parameters">Autofac.Core.Parameter</param>
        /// <returns>返回获取的组件</returns>
        public static TService Resolve<TService>(params Parameter[] parameters)
        {
            return ResolutionExtensions.Resolve<TService>(_container, parameters);
        }

        /// <summary>
        /// 按key获取组件
        /// </summary>
        /// <typeparam name="TService">组件类型</typeparam>
        /// <param name="serviceKey">枚举类型的Key</param>
        /// <returns>返回获取的组件</returns>
        public static TService ResolveKeyed<TService>(object serviceKey)
        {
            return ResolutionExtensions.ResolveKeyed<TService>(_container, serviceKey);
        }

        /// <summary>
        /// 按名称获取组件
        /// </summary>
        /// <typeparam name="TService">组件类型</typeparam>
        /// <param name="serviceName">组件名称</param>
        /// <returns>返回获取的组件</returns>
        public static TService ResolveNamed<TService>(string serviceName)
        {
            return ResolutionExtensions.ResolveNamed<TService>(_container, serviceName);
        }

        /// <summary>
        /// 获取InstancePerHttpRequest的组件
        /// </summary>
        /// <typeparam name="TService">组件类型</typeparam>
        /// <returns></returns>
        public static TService ResolvePerHttpRequest<TService>()
        {
            IDependencyResolver current = DependencyResolver.Current;
            if (current != null)
            {
                TService service = (TService)current.GetService(typeof(TService));
                if (service != null)
                {
                    return service;
                }
            }
            return ResolutionExtensions.Resolve<TService>(_container);
        }
    }
}
