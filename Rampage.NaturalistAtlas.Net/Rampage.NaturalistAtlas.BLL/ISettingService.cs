using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    public interface ISettingService : ISuperService<Setting, ISettingDao>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <returns></returns>
        S GetSetting<S>() where S : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <returns></returns>
        S GetSettingFromCache<S>() where S : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="setting"></param>
        /// <param name="adminId"></param>
        /// <returns></returns>
        R SaveSetting<S>(S setting, int adminId) where S : class, new();


    }
}
