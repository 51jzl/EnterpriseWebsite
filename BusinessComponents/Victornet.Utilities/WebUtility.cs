using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

namespace Victornet.Utilities
{
    public static class WebUtility
    {
        public static readonly string HtmlNewLine = "<br />";

        public static string FormatCompleteUrl(string content)
        {
            string pattern = "src=[\"']\\s*(/[^\"']*)\\s*[\"']";
            string str2 = "href=[\"']\\s*(/[^\"']*)\\s*[\"']";
            string str3 = HostPath(HttpContext.Current.Request.Url);
            content = Regex.Replace(content, pattern, "src=\"" + str3 + "$1\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            content = Regex.Replace(content, str2, "href=\"" + str3 + "$1\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return content;
        }

        public static string GetIP()
        {
            return GetIP(HttpContext.Current);
        }

        public static string GetIP(HttpContext httpContext)
        {
            string userHostAddress = string.Empty;
            if (httpContext != null)
            {
                userHostAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(userHostAddress))
                {
                    userHostAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                if (string.IsNullOrEmpty(userHostAddress))
                {
                    userHostAddress = HttpContext.Current.Request.UserHostAddress;
                }
            }
            return userHostAddress;
        }

        public static string GetPhysicalFilePath(string filePath)
        {
            if ((filePath.IndexOf(@":\") != -1) || (filePath.IndexOf(@"\\") != -1))
            {
                return filePath;
            }
            if (HostingEnvironment.IsHosted)
            {
                return HostingEnvironment.MapPath(filePath);
            }
            filePath = filePath.Replace('/', Path.DirectorySeparatorChar).Replace("~", "");
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        }

        public static string GetServerDomain(Uri uri, string[] domainRules)
        {
            if (uri == null)
            {
                return string.Empty;
            }
            string str = uri.Host.ToString().ToLower();
            if (str.IndexOf('.') <= 0)
            {
                return str;
            }
            string[] strArray = str.Split(new char[] { '.' });
            string s = strArray.GetValue((int)(strArray.Length - 1)).ToString();
            int result = -1;
            if (int.TryParse(s, out result))
            {
                return str;
            }
            string oldValue = string.Empty;
            string str4 = string.Empty;
            string str5 = string.Empty;
            for (int i = 0; i < domainRules.Length; i++)
            {
                if (str.EndsWith(domainRules[i].ToLower()))
                {
                    oldValue = domainRules[i].ToLower();
                    str4 = str.Replace(oldValue, "");
                    if (str4.IndexOf('.') > 0)
                    {
                        string[] strArray2 = str4.Split(new char[] { '.' });
                        return (strArray2.GetValue((int)(strArray2.Length - 1)).ToString() + oldValue);
                    }
                    return (str4 + oldValue);
                }
                str5 = str;
            }
            return str5;
        }

        public static string HostPath(Uri uri)
        {
            if (uri == null)
            {
                return string.Empty;
            }
            string str = uri.IsDefaultPort ? string.Empty : (":" + Convert.ToString(uri.Port, CultureInfo.InvariantCulture));
            return (uri.Scheme + Uri.SchemeDelimiter + uri.Host + str);
        }

        public static string HtmlDecode(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent))
            {
                return rawContent;
            }
            return HttpUtility.HtmlDecode(rawContent);
        }

        public static string HtmlEncode(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent))
            {
                return rawContent;
            }
            return HttpUtility.HtmlEncode(rawContent);
        }

        public static string ResolveUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
            {
                return relativeUrl;
            }
            if (!relativeUrl.StartsWith("~/"))
            {
                return relativeUrl;
            }
            string[] strArray = relativeUrl.Split(new char[] { '?' });
            string str = VirtualPathUtility.ToAbsolute(strArray[0]);
            if (strArray.Length > 1)
            {
                str = str + "?" + strArray[1];
            }
            return str;
        }

        public static void Return304(HttpContext httpContext, bool endResponse = true)
        {
            ReturnStatusCode(httpContext, 0x130, "304 Not Modified", endResponse);
        }

        public static void Return403(HttpContext httpContext)
        {
            ReturnStatusCode(httpContext, 0x193, null, false);
            if (httpContext != null)
            {
                httpContext.Response.SuppressContent = true;
                httpContext.Response.End();
            }
        }

        public static void Return404(HttpContext httpContext)
        {
            ReturnStatusCode(httpContext, 0x194, null, false);
            if (httpContext != null)
            {
                httpContext.Response.SuppressContent = true;
                httpContext.Response.End();
            }
        }

        private static void ReturnStatusCode(HttpContext httpContext, int statusCode, string status, bool endResponse)
        {
            if (httpContext != null)
            {
                httpContext.Response.Clear();
                httpContext.Response.StatusCode = statusCode;
                if (!string.IsNullOrEmpty(status))
                {
                    httpContext.Response.Status = status;
                }
                if (endResponse)
                {
                    httpContext.Response.End();
                }
            }
        }

        public static void SetStatusCodeForError(HttpResponseBase response)
        {
            response.StatusCode = 300;
        }

        public static string UrlDecode(string urlToDecode)
        {
            if (string.IsNullOrEmpty(urlToDecode))
            {
                return urlToDecode;
            }
            return HttpUtility.UrlDecode(urlToDecode);
        }

        public static string UrlEncode(string urlToEncode)
        {
            if (string.IsNullOrEmpty(urlToEncode))
            {
                return urlToEncode;
            }
            return HttpUtility.UrlEncode(urlToEncode).Replace("'", "%27");
        }
    }
}
