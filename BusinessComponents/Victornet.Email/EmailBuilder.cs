using Microsoft.CSharp.RuntimeBinder;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using Victornet;
using Victornet.Caching;
using Victornet.Utilities;

namespace Victornet.Email
{

    public class EmailBuilder
    {

        private static volatile EmailBuilder _defaultInstance = null;
        private static Dictionary<string, EmailTemplate> emailTemplates = null;
        private static bool isInitialized;
        private static readonly object lockObject = new object();

        private EmailBuilder()
        {
        }

        private static void EnsureLoadTemplates()
        {
            if (!isInitialized)
            {
                lock (lockObject)
                {
                    if (!isInitialized)
                    {
                        emailTemplates = LoadEmailTemplates();
                        isInitialized = true;
                    }
                }
            }
        }

        private static EmailTemplate GetEmailTemplate(string templateName)
        {
            if ((emailTemplates == null) || !emailTemplates.ContainsKey(templateName))
            {
                throw new ExceptionFacade(new ResourceExceptionDescriptor().WithContentNotFound("邮件模板", templateName), null);
            }
            return emailTemplates[templateName];
        }

        public static EmailBuilder Instance()
        {
            if (_defaultInstance == null)
            {
                lock (lockObject)
                {
                    if (_defaultInstance == null)
                    {
                        _defaultInstance = new EmailBuilder();
                    }
                }
            }
            EnsureLoadTemplates();
            return _defaultInstance;
        }

        private static Dictionary<string, EmailTemplate> LoadEmailTemplates()
        {
            string str = "zh-CN";
            string cacheKey = "EmailTemplates::" + str;
            ICacheService service = DIContainer.Resolve<ICacheService>();
            Dictionary<string, EmailTemplate> dictionary = service.Get<Dictionary<string, EmailTemplate>>(cacheKey);
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, EmailTemplate>();
                string searchPattern = "*.xml";
                string[] files = Directory.GetFiles(WebUtility.GetPhysicalFilePath(string.Format("~/Languages/" + str + "/emails/", new object[0])), searchPattern);
                string physicalFilePath = WebUtility.GetPhysicalFilePath("~/Applications/");
                IEnumerable<string> first = new List<string>();
                if (Directory.Exists(physicalFilePath))
                {
                    foreach (string str6 in Directory.GetDirectories(physicalFilePath))
                    {
                        string path = Path.Combine(str6, @"Languages\" + str + @"\emails\");
                        if (Directory.Exists(path))
                        {
                            first = first.Union<string>(Directory.GetFiles(path, searchPattern));
                        }
                    }
                }
                files = files.Union<string>(first).ToArray<string>();
                Type modelType = new ExpandoObject().GetType();
                foreach (string str8 in files)
                {
                    if (File.Exists(str8))
                    {
                        XmlDocument document = new XmlDocument();
                        document.Load(str8);
                        foreach (XmlNode node in document.GetElementsByTagName("email"))
                        {
                            XmlNode namedItem = node.Attributes.GetNamedItem("templateName");
                            if (namedItem != null)
                            {
                                string innerText = namedItem.InnerText;
                                EmailTemplate template = new EmailTemplate(node);
                                dictionary[innerText] = template;
                                if (!string.IsNullOrEmpty(template.Body))
                                {
                                    Razor.Compile(template.Body, modelType, innerText);
                                }
                            }
                        }
                    }
                }
                service.Add(cacheKey, dictionary, CachingExpirationType.Stable);
            }
            return dictionary;
        }

        public MailMessage Resolve(string templateName, ExpandoObject model, string to, string from = null)
        {
            return this.Resolve(templateName, model, new string[] { to }, from, null, null);
        }

        /// <summary>
        /// 反编译解决乱码ing
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="model"></param>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        /// <returns></returns>
        public MailMessage Resolve(string templateName, object model, IEnumerable<string> to, string from = null, IEnumerable<string> cc = null, IEnumerable<string> bcc = null) //[Dynamic] object model 第二个参数
        {
            if (to == null)
            {
                return null;
            }
            //if (<Resolve>o__SiteContainer2.<>p__Site3 == null)
            //{
            //    <Resolve>o__SiteContainer2.<>p__Site3 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(EmailBuilder), new CSharpArgumentInfo[] { 
            //        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }));

            //    EmailBuilder.<Resolve>o__SiteContainer2.<>p__Site3 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(EmailBuilder), new CSharpArgumentInfo[]
            //    {
            //        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            //    }));
            //}
            //if (<Resolve>o__SiteContainer2.<>p__Site3.Target.Invoke(<Resolve>o__SiteContainer2.<>p__Site3, ((dynamic) model) == ((dynamic) null)))
            //{
            //    model = new ExpandoObject();
            //}

            EmailSettings settings = DIContainer.Resolve<IEmailSettingsManager>().Get();
            EmailTemplate emailTemplate = GetEmailTemplate(templateName);
            if (string.IsNullOrEmpty(from))
            {
                if (!string.Equals(emailTemplate.From, "NoReplyAddress", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!string.Equals(emailTemplate.From, "AdminAddress", StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new ExceptionFacade(new CommonExceptionDescriptor("发件人不能为null"), null);
                    }
                    from = settings.AdminEmailAddress;
                }
                else
                {
                    from = settings.NoReplyAddress;
                }
            }
            MailMessage message = new MailMessage {
                IsBodyHtml = true
            };
            try
            {
                //if (<Resolve>o__SiteContainer2.<>p__Site5 == null)
                //{
                //    <Resolve>o__SiteContainer2.<>p__Site5 = CallSite<Func<CallSite, Type, string, object, MailAddress>>.Create(Binder.InvokeConstructor(CSharpBinderFlags.None, typeof(EmailBuilder), new CSharpArgumentInfo[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }));
                //}
                //message.From = <Resolve>o__SiteContainer2.<>p__Site5.Target.Invoke(<Resolve>o__SiteContainer2.<>p__Site5, typeof(MailAddress), from, ((dynamic) model).SiteName);
            }
            catch
            {
            }
            foreach (string str in to)
            {
                try
                {
                    message.To.Add(str);
                }
                catch
                {
                }
            }
            if (cc != null)
            {
                foreach (string str2 in cc)
                {
                    try
                    {
                        message.CC.Add(str2);
                    }
                    catch
                    {
                    }
                }
            }
            if (bcc != null)
            {
                foreach (string str3 in bcc)
                {
                    try
                    {
                        message.Bcc.Add(str3);
                    }
                    catch
                    {
                    }
                }
            }
            try
            {
                message.Subject = (string) Razor.Parse(emailTemplate.Subject, (dynamic) model, emailTemplate.TemplateName);
            }
            catch (Exception exception)
            {
                throw new ExceptionFacade(new CommonExceptionDescriptor("编译邮件模板标题时报错"), exception);
            }
            message.Priority = emailTemplate.Priority;
            if (!string.IsNullOrEmpty(emailTemplate.Body))
            {
                try
                {
                    message.Body = (string) Razor.Parse(emailTemplate.Body, (dynamic) model, emailTemplate.Body);
                    return message;
                }
                catch (Exception exception2)
                {
                    throw new ExceptionFacade("编译邮件模板内容时报错", exception2);
                }
            }
            if (string.IsNullOrEmpty(emailTemplate.BodyUrl))
            {
                throw new ExceptionFacade("邮件模板中Body、BodyUrl必须填一个", null);
            }
            message.Body = HttpCollects.GetHTMLContent(emailTemplate.BodyUrl);
            return message;
        }

        public IList<EmailTemplate> EmailTemplates
        {
            get
            {
                return emailTemplates.Values.ToReadOnly<EmailTemplate>();
            }
        }
    }
}

