using FluentScheduler;

namespace Rampage.NaturalistAtlas.SchedulerJob
{
    public class SchedulerJobStarter
    {
        /// <summary>
        /// WebApi / WebSite 等 Web 宿主：仅本机 MemoryCache* Job。
        /// </summary>
        public static void StartWebHostJobs()
        {
            JobManager.Initialize(new WebHostSchedulerRegistry());
            JobManager.Start();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="registry"></param>
        public static void StartJobs(Registry registry)
        {
            JobManager.Initialize(registry);
            JobManager.Start();
        }

    }
}
