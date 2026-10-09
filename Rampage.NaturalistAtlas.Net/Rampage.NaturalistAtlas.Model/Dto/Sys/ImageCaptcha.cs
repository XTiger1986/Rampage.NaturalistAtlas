using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    public class ImageCaptcha
    {

        public static readonly int VALID_SECONDS = 120;

        public static readonly string CACHE_KEY_TIME_PFX = "IMAGE_CAPTCHA_TIME";
        public static readonly string CACHE_KEY_TIME_TPL = CACHE_KEY_TIME_PFX + "_{0}_{1}";
        public static readonly string CACHE_KEY_CODE_PFX = "IMAGE_CAPTCHA_CODE";
        public static readonly string CACHE_KEY_CODE_TPL = CACHE_KEY_CODE_PFX + "_{0}_{1}";

        /// <summary>
        /// 图形验验证码会话ID
        /// </summary>
        public string? CaptchaId { get; set; }

        /// <summary>
        /// 验证码图片
        /// </summary>
        public string? Base64 { get; set; }

    }
}
