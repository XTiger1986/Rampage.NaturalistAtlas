using System;
using System.Threading;

namespace Rampage.NaturalistAtlas.Collector
{
    class Program
    {

        /// <summary>
        /// 采集作业程序入口 / Collector entry point
        /// </summary>
        /// <param name="args">args[0] 为环境名 / args[0] is the environment name</param>
        static void Main(string[] args)
        {
            // 重定向/服务场景下默认按本地码页输出会把中文写坏，统一 UTF-8（个别宿主不允许设置时忽略）
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* ignore */ }

            RampageBooter.Run<NaturalistAtlasCollectorApplication>(args);
            if (Console.IsInputRedirected)
            {
                // 无控制台（服务/后台运行）时挂起等待进程退出信号
                // Suspend forever when running headless (service/background), until killed
                Console.WriteLine("Collector running headless. Send Ctrl+C / SIGTERM to quit.");
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                Console.WriteLine("Press any key to quit~");
                Console.ReadKey();
            }
        }

    }
}
