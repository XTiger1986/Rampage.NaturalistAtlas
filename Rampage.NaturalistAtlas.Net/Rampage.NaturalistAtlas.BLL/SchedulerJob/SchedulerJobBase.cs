using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using Rampage.AppLog;
using Rampage.Caching;
using Rampage.Data.Client;
using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.Common;
using Rampage.RedisLibrary;
using System;

namespace Rampage.NaturalistAtlas.SchedulerJob
{
    public abstract class SchedulerJobBase : IJob
    {
        protected IServiceScope ServiceScope
        {
            get;
            private set;
        } = default!;

        protected IScopedServiceLocator ServiceLocator
        {
            get;
            private set;
        } = default!;

        protected IServiceProvider ServiceProvider
        {
            get;
            private set;
        } = default!;

        protected DatabaseClient MainDb
        {
            get;
            private set;
        } = default!;

        protected IStandAloneCache MemoryCache
        {
            get;
            private set;
        } = default!;

        protected RedisHelper RedisClient
        {
            get;
            private set;
        } = default!;

        private string? _scopeName;
        protected string ScopeName
        {
            get {
                if (string.IsNullOrEmpty(_scopeName))
                {
                    _scopeName = $"SchedulerJob_{this.GetType().Name}";
                }
                return _scopeName;
            }
        }

        public void Execute()
        {
            try
            {
                using (RampageScope.Begin(RampageBooter.Application.GetServiceProvider()))
                {
                    RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(nameof(SchedulerJobBase));
                    ServiceProvider = handle.ServiceProvider;
                    ServiceLocator = handle.Locator;
                    ServiceScope = handle.OwnedScope
                        ?? throw new InvalidOperationException("SchedulerJobBase: Begin() did not own a scope.");

                    List<DatabaseClient> databaseClientList = ServiceLocator.GetList<DatabaseClient>();

                    MainDb = databaseClientList.Find(dc => dc.Name == GlobalVars.MAIN_DB_NAME)!;
                    MemoryCache = SingletonServiceLocator.GetService<IStandAloneCache>();
                    RedisClient = RedisFactory.GetClient()!;

                    AutoInjecter autoInjecter = new AutoInjecter(ServiceLocator);
                    autoInjecter.Build(this);

                    ExecuteJob();
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        protected abstract void ExecuteJob();

        protected void LogError(string? message, Exception ex)
        {
            RampageLogger.Error(message, ex);
        }

        protected void LogInfo(string message)
        {
            RampageLogger.Info(message);
        }

    }
}
