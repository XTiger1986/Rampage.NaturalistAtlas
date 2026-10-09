using Microsoft.Extensions.DependencyInjection;
using Rampage.AppLog;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Collector.Jobs;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.SchedulerJob;
using Rampage.Utilities;
using System;

namespace Rampage.NaturalistAtlas.Collector
{
    public class NaturalistAtlasCollectorApplication : RampageBasicApplication, IRampageApplication
    {
        public override void Run(string[] args)
        {
            // base.Run 内部完成 SuperConfig 绑定（CollectorConfig.WorkerId 在此之后才可读）
            base.Run(args);

            // 雪花机器码须在首次发号前绑定（配置绑定之后、作业启动之前）。
            // 越界值 fail-fast：静默回落 0 会与 WebApi/ManagementSite 撞机器码，多实例下产生主键冲突
            // The snowflake worker id must bind before the first id is issued (after config binding,
            // before jobs start). Out-of-range values fail fast — silently falling back to 0 would
            // collide with WebApi/ManagementSite and break primary keys across instances
            int workerId = CollectorConfig.WorkerId;
            SnowflakeTool.SetWorkerId(workerId);

            // 一次性任务模式：--run-wanted 仅执行集邮缺口同步、--run-check 仅执行删除对账后退出
            // （不启动调度；手动补跑/运维与上线前"存活 id 零误杀"验证用）
            // One-shot modes: --run-wanted runs only the collection-gap sync, --run-check only the
            // deletion check, then exits (no scheduler; for manual/ops triggers and the pre-launch
            // zero-false-deletion verification against live ids)
            if (Array.IndexOf(args, "--run-histogram") >= 0)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 一次性任务：物候直方图（全采集城市）");
                RampageLogger.Info("一次性任务：物候直方图 (--run-histogram)");
                using (Rampage.DependencyInjection.RampageScope.Begin(RampageBooter.Application.GetServiceProvider()))
                {
                    IAtlasCollectionService collectionService = GlobalScope.ServiceProvider.GetRequiredService<IAtlasCollectionService>();
                    foreach (int cityId in collectionService.GetCollectionCityIds())
                    {
                        collectionService.RunHistogramAsync(cityId).GetAwaiter().GetResult();
                    }
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 一次性任务完成，退出");
                return;
            }

            if (Array.IndexOf(args, "--run-wanted") >= 0)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 一次性任务：集邮缺口同步开始");
                RampageLogger.Info("一次性任务：集邮缺口同步开始 (--run-wanted)");
                using (Rampage.DependencyInjection.RampageScope.Begin(RampageBooter.Application.GetServiceProvider()))
                {
                    IAtlasCollectionService collectionService = GlobalScope.ServiceProvider.GetRequiredService<IAtlasCollectionService>();
                    collectionService.RunWantedSyncAsync().GetAwaiter().GetResult();
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 一次性任务完成，退出");
                return;
            }

            if (Array.IndexOf(args, "--run-check") >= 0)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 一次性任务：删除对账开始（双源 × 采集城市）");
                RampageLogger.Info("一次性任务：删除对账开始 (--run-check)");
                using (Rampage.DependencyInjection.RampageScope.Begin(RampageBooter.Application.GetServiceProvider()))
                {
                    IAtlasCollectionService collectionService = GlobalScope.ServiceProvider.GetRequiredService<IAtlasCollectionService>();
                    foreach (int cityId in collectionService.GetCollectionCityIds())
                    {
                        collectionService.RunDeletionCheckAsync(AtlasSource.INaturalist, cityId).GetAwaiter().GetResult();
                        collectionService.RunDeletionCheckAsync(AtlasSource.Gbif, cityId).GetAwaiter().GetResult();
                    }
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 一次性任务完成，退出");
                return;
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] NaturalistAtlas.Collector 就绪：雪花WorkerId={workerId}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 调度：增量=启动即跑+每小时 | 回补=启动即跑+每10分钟切片续采 | 分类刷新=每日03:00 | 物候直方图=每日03:30 | 删除对账=每周四04:00 | 集邮缺口=每周四04:30");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 采集城市在管理后台 常规设置→采集城市ID清单 配置（未配置默认北京）；随时 Ctrl+C 退出，重启后自动续采不重复");
            RampageLogger.Info($"Collector 就绪：WorkerId={workerId}，调度启动（增量=启动+每小时 / 回补=启动+每10分钟 / 分类刷新=每日03:00 / 直方图=每日03:30 / 对账=每周四04:00 / 集邮缺口=每周四04:30）");

            SchedulerJobStarter.StartJobs(new CollectorSchedulerRegistry());
        }
    }
}
