using Rampage.Configuration;
using System;

namespace Rampage.NaturalistAtlas.Collector
{
    /// <summary>
    /// Collector 进程配置（config/app.json 的 Collector 节）
    /// Collector process configuration (the Collector section of config/app.json)
    /// </summary>
    [SuperConfig]
    public static class CollectorConfig
    {
        /// <summary>
        /// 雪花机器码（0~1023，各进程须互异；必须在首次发号前绑定）
        /// Snowflake worker id (0~1023, unique per process; must bind before the first id is issued)
        /// </summary>
        [ConfigValue("Collector.WorkerId")]
        public static int WorkerId
        {
            get;
            set;
        }
    }
}
