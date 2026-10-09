using Rampage.Configuration;

namespace Rampage.NaturalistAtlas.ManagementSite.Internal
{
    /// <summary>
    /// ManagementSite 进程配置（config/app.json 的 Management 节）
    /// ManagementSite process configuration (the Management section of config/app.json)
    /// </summary>
    [SuperConfig]
    public static class ManagementConfig
    {
        /// <summary>
        /// 雪花机器码（0~1023，与 Collector/WebApi 各进程互异；必须在首次发号前绑定）。
        /// 分配约定：Collector=1、WebApi=2、ManagementSite=3
        /// Snowflake worker id (0~1023, unique per process across Collector/WebApi/ManagementSite;
        /// must bind before the first id is issued). Allocation: Collector=1, WebApi=2, ManagementSite=3
        /// </summary>
        [ConfigValue("Management.WorkerId")]
        public static int WorkerId
        {
            get;
            set;
        }
    }
}
