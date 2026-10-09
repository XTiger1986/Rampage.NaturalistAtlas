using Microsoft.AspNetCore.Cors.Infrastructure;
using Rampage.Idempotency;
using Rampage.Localization;
using Rampage.NaturalistAtlas.Common;
using Rampage.NaturalistAtlas.SchedulerJob;
using Rampage.NaturalistAtlas.WebApi.Internal;
using Rampage.RedisLibrary;
using Rampage.Web;
using Rampage.Web.HealthChecks;
using Rampage.Web.Middleware;
using Rampage.Web.Mvc;

namespace Rampage.NaturalistAtlas.WebApi
{
    public class NaturalistAtlasApplication : RampageWebApplication
    {
        public const string CORS_POLICY_NAME = "AllowSpecificOrigin";

        public NaturalistAtlasApplication() : base()
        {
            ApplicationBuilderCreated += (webBuilder, services) => {
                // 雪花机器码须在首次发号前绑定（举报提交等写路径会发号）；越界值直接抛出，宁可启动失败也不静默共用 0
                // The snowflake worker id must bind before the first id is issued (report submits do);
                // out-of-range values throw — fail at startup rather than silently sharing worker id 0
                Rampage.Utilities.SnowflakeTool.SetWorkerId(WebApiConfig.WorkerId);

                webBuilder.Services.AddRampageHealthChecks();
                webBuilder.Services.AddRouting(options => {
                    options.LowercaseUrls = true;
                    options.AppendTrailingSlash = true;
                });
                // HTTP 幂等：成功响应缓存完整 R JSON，重复请求原样回放
                webBuilder.Services.Configure<IdempotencyOptions>(opt => {
                    opt.HeaderName = IdempotencyActionFilter.DefaultHeaderName;
                    opt.Ttl = TimeSpan.FromHours(24);
                    opt.MaxKeyLength = 64;
                });
                webBuilder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
                webBuilder.Services.AddSingleton<IIdempotencyKeyBuilder, NaturalistAtlasIdempotencyKeyBuilder>();

                webBuilder.Services.AddScoped<IdempotencyActionFilter>();
                webBuilder.Services.AddScoped<Internal.ReportRateLimitActionFilter>();

                // 与 NaturalistAtlasApiController 上 [EnableCors("AllowSpecificOrigin")] 对齐；白名单见配置节 Cors
                // Cors:Enabled=false 时跳过注册（一般仅用于特殊部署；玩家站跨域调用需保持 true）
                if (Rampage.Web.Cors.CorsOptions.Load().Enabled)
                {
                    webBuilder.Services.AddCors(options => {
                        Action<CorsPolicyBuilder> configure = policy => {
                            policy
                                .SetIsOriginAllowed(origin => Rampage.Web.Cors.CorsOptions.Load().IsOriginAllowed(origin))
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        };
                        options.AddPolicy(CORS_POLICY_NAME, configure);
                        options.AddDefaultPolicy(configure);
                    });
                }
            };

            // 链路ID应最先挂（覆盖全部后续中间件与业务日志）；CORS 紧随其后，避免预检被后续中间件/重定向破坏
            ApplicationBuilt += (app, provider) => {
                app.UseRampageCorrelationId();
                if (Rampage.Web.Cors.CorsOptions.Load().Enabled)
                {
                    app.UseCors(CORS_POLICY_NAME);
                }
            };

            ApplicationInitialized += (app, provider) => {
                LocalizationManager.CultureInfoGetter = LocalizationLogic.GetCultureInfo;
                SchedulerJobStarter.StartWebHostJobs();
            };
        }

    }
}
