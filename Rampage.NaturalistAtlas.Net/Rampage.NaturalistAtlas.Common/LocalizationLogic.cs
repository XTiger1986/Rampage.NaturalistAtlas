using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;
using System.Globalization;

namespace Rampage.NaturalistAtlas.Common
{
    public class LocalizationLogic
    {

        public static CultureInfo GetCultureInfo()
        {
            if (RequestContext.Current == null)
            {
                return new CultureInfo("zh-CN");
            }
            string userLanguages = RequestContext.Current.Request.Headers["Accept-Language"].ToString();
            List<string> languages = userLanguages.Split(',').Select(lang => lang.Trim()).ToList();
            bool hasZH = languages.Any(lang => StringTool.StartsWithIgnoreCase(lang, "zh"));

            if (hasZH)
            {
                return new CultureInfo("zh-CN");
            }
            else
            {
                return new CultureInfo("en-US");
            }
        }

    }
}
