using Autofac;
using Autofac.Core;
using System;
using System.Web.Mvc;
namespace Victornet
{
    /// <summary>
    /// ����ע������
    /// </summary>
    /// <remarks>
    /// ��Autofac���з�װ
    /// </remarks>
    public class DIContainer
    {
        private static IContainer _container;

        /// <summary>
        /// ע��DIContainer
        /// </summary>
        /// <param name="container">Autofac.IContainer</param>
        public static void RegisterContainer(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// ��������ȡ���
        /// </summary>
        /// <typeparam name="TService">�������</typeparam>
        /// <returns>���ػ�ȡ�����</returns>
        public static TService Resolve<TService>()
        {
            return ResolutionExtensions.Resolve<TService>(_container);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TService">�������</typeparam>
        /// <param name="parameters">Autofac.Core.Parameter</param>
        /// <returns>���ػ�ȡ�����</returns>
        public static TService Resolve<TService>(params Parameter[] parameters)
        {
            return ResolutionExtensions.Resolve<TService>(_container, parameters);
        }

        /// <summary>
        /// ��key��ȡ���
        /// </summary>
        /// <typeparam name="TService">�������</typeparam>
        /// <param name="serviceKey">ö�����͵�Key</param>
        /// <returns>���ػ�ȡ�����</returns>
        public static TService ResolveKeyed<TService>(object serviceKey)
        {
            return ResolutionExtensions.ResolveKeyed<TService>(_container, serviceKey);
        }

        /// <summary>
        /// �����ƻ�ȡ���
        /// </summary>
        /// <typeparam name="TService">�������</typeparam>
        /// <param name="serviceName">�������</param>
        /// <returns>���ػ�ȡ�����</returns>
        public static TService ResolveNamed<TService>(string serviceName)
        {
            return ResolutionExtensions.ResolveNamed<TService>(_container, serviceName);
        }

        /// <summary>
        /// ��ȡInstancePerHttpRequest�����
        /// </summary>
        /// <typeparam name="TService">�������</typeparam>
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
