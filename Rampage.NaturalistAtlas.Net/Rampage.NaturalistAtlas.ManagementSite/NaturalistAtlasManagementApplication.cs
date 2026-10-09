using Microsoft.AspNetCore.Cors.Infrastructure;
using Rampage.Localization;
using Rampage.Management.Web;
using Rampage.NaturalistAtlas.Common;
using Rampage.Web.HealthChecks;
using Rampage.Web.Middleware;

namespace Rampage.NaturalistAtlas.ManagementSite
{
    public class NaturalistAtlasManagementApplication : RampageManagementApplication
    {
        public const string CorsPolicyName = "AllowSpecificOrigin";

        public NaturalistAtlasManagementApplication() : base()
        {
            ApplicationBuilderCreated += (webBuilder, services) =>
            {
                // 雪花机器码须在首次发号前绑定（工单处理等写路径会发号）；越界值直接抛出，宁可启动失败也不静默共用 0
                // The snowflake worker id must bind before the first id is issued (report handling does);
                // out-of-range values throw — fail at startup rather than silently sharing worker id 0
                Rampage.Utilities.SnowflakeTool.SetWorkerId(Internal.ManagementConfig.WorkerId);

                webBuilder.Services.AddRampageHealthChecks();
                webBuilder.Services.AddRouting(options =>
                {
                    options.LowercaseUrls = true;
                    options.AppendTrailingSlash = true;
                });

                // 管理后台默认同域自调用 API；Cors:Enabled=false 时不注册策略，避免跨域中间件介入
                Rampage.Web.Cors.CorsOptions corsOptions = Rampage.Web.Cors.CorsOptions.Load();
                if (corsOptions.Enabled)
                {
                    webBuilder.Services.AddCors(options =>
                    {
                        Action<CorsPolicyBuilder> configure = policy =>
                        {
                            policy
                                .SetIsOriginAllowed(origin => Rampage.Web.Cors.CorsOptions.Load().IsOriginAllowed(origin))
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        };
                        options.AddPolicy(CorsPolicyName, configure);
                        options.AddDefaultPolicy(configure);
                    });
                }
            };
            ApplicationBuilt += (app, provider) =>
            {
                // 链路ID最先挂，覆盖全部后续中间件与业务日志
                app.UseRampageCorrelationId();
            };

            ApplicationInitialized += (app, provider) =>
            {
                LocalizationManager.CultureInfoGetter = LocalizationLogic.GetCultureInfo;

                if (app is WebApplication webApp)
                {
                    webApp.MapRampageHealthChecks("/health");
                }
            };
        }

    }
}
