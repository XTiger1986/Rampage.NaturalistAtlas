/**
 * admin-personal.js — 个人中心交互（头像上传）
 *
 * 职责：
 *   PersonalPages.bindAvatarUpload —— 选择图片即自动上传并刷新预览
 *
 * 上传契约：POST /api/personal/avatar/upload，FormData(AvatarFile)；
 *          成功返回 R（Data 为新头像 Url 字符串）。
 *
 * 依赖：rpg-core.js、jQuery
 */
(function (global) {
    'use strict';

    /** 绑定头像选择即上传 */
    function bindAvatarUpload() {
        var input = $('#avatarFile');
        if (!input.length) { return; }

        input.on('change', function () {
            var files = this.files;
            if (!files || files.length === 0) { return; }

            var file = files[0];
            if (file.type && file.type.indexOf('image/') !== 0) {
                global.RPG.showErrorTip('仅支持图片文件');
                input.val('');
                return;
            }

            var formData = new FormData();
            // 服务端 IFormFile 参数名为 avatarFile（区分大小写匹配）
            formData.append('avatarFile', file);

            // 预览进入上传中状态
            $('#avatarPreview, #avatarPlaceholder').addClass('is-uploading');

            $.ajax({
                url: '/api/personal/avatar/upload',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (jsonResult) {
                    if (global.RPG.isSuccess(jsonResult)) {
                        var url = jsonResult && jsonResult.Data;
                        if (url) {
                            // 本地即时预览（FileReader），避免等待 CDN 回源
                            var reader = new FileReader();
                            reader.onload = function (e) {
                                $('#avatarPlaceholder').hide();
                                var preview = $('#avatarPreview');
                                preview.attr('src', e.target.result).show();
                            };
                            reader.readAsDataURL(file);
                        }
                        global.RPG.showSuccessTip(global.RPG.getMessage(jsonResult, '头像已更新'));
                    } else {
                        global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '上传失败'));
                    }
                },
                error: function () {
                    global.RPG.showErrorTip('上传失败，请稍后重试');
                },
                complete: function () {
                    $('#avatarPreview, #avatarPlaceholder').removeClass('is-uploading');
                    input.val('');
                }
            });
        });
    }

    global.PersonalPages = {
        bindAvatarUpload: bindAvatarUpload
    };
})(window);
