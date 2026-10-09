using Rampage.DependencyInjection;
using Rampage.Json;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Utilities;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    public class SettingService : SuperService<Setting, ISettingDao>, ISettingService
    {
        [Auto]
        protected ISettingDao SettingDao = default!;

        [Auto]
        protected IMemoryCacheOperationService MemoryCacheOperationService = default!;

        public S GetSetting<S>() where S : class, new()
        {
            Type type = typeof(S);
            string typeName = type.FullName!;
            Setting? setting = SettingDao.SelectOne(m => m.Type == typeName);
            if (setting != null && !string.IsNullOrEmpty(setting.Data))
            {
                S? model = JsonTool.Parse<S>(setting.Data);
                return model ?? new S();
            }
            return new S();
        }

        public S GetSettingFromCache<S>() where S : class, new()
        {
            Type type = typeof(S);
            string typeName = type.FullName!;
            return HandleWebCache<S>(() => {
                return GetSetting<S>();
            }, Setting.CACHE_KEY_ONE_TPL, MathTool.GenerateInt(1800, 2400), typeName)!;
        }

        public R SaveSetting<S>(S setting, int adminId) where S : class, new()
        {
            Type type = typeof(S);
            string typeName = type.FullName!;

            Setting? model = SettingDao.SelectOne(s => s.Type == typeName);
            if (model == null)
            {
                model = new Setting
                {
                    Name = type.Name,
                    Type = typeName,
                    CreateTime = UtcNow
                };
            }

            model.Data = JsonTool.Stringify(setting);
            model.UpdateTime = UtcNow;

            bool result;
            if (model.Id > 0)
            {
                model.UpdateAdminId = adminId;
                result = SettingDao.UpdateById(model);
            }
            else
            {
                model.CreateAdminId = adminId;
                result = SettingDao.Insert(model);
            }
            if (result)
            {
                MemoryCache.Remove(string.Format(Setting.CACHE_KEY_ONE_TPL, typeName));
                MemoryCacheOperationService.TryAddDeletionQueue(string.Format(Setting.CACHE_KEY_ONE_TPL, typeName), false);
            }

            return result ? R.Success() : R.Failure();
        }


    }
}
