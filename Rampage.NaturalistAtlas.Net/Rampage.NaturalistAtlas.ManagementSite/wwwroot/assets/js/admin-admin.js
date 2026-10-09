/**
 * admin-admin.js — 后台账号/管理组/菜单 页交互
 *
 * 职责：
 *   AdminPages.bindDelete —— 统一“删除行”绑定：
 *     选择器匹配按钮 → 确认 → DELETE 请求 → 成功后淡出并移除对应表格行
 *
 * 依赖：rpg-core.js（RPG.confirm / RPG.Delete / 提示气泡）、jQuery
 */
(function (global) {
    'use strict';

    /**
     * 绑定删除按钮
     * @param {string} selector   按钮选择器（须带 data-id）
     * @param {function} urlBuilder 以 id 生成删除接口地址
     * @param {string} [confirmText] 确认文案；缺省用通用文案
     */
    function bindDelete(selector, urlBuilder, confirmText) {
        var text = confirmText || '确认删除该记录？删除后不可恢复。';

        $(document).on('click', selector, function () {
            var btn = $(this);
            var id = btn.attr('data-id');
            if (!id) { return; }

            global.RPG.confirm(text, function () {
                // 防重复提交：请求期间禁用按钮
                btn.prop('disabled', true);

                var url = urlBuilder(id);
                // 第 4 参留空（表单编码提交）；网络层失败走第 5 参 failCallback 恢复按钮
                // The 4th arg stays falsy (form-encoded); network failures restore the button via the 5th-arg failCallback
                global.RPG.Delete(url, {}, function (jsonResult) {
                    if (global.RPG.isSuccess(jsonResult)) {
                        global.RPG.showSuccessTip(global.RPG.getMessage(jsonResult, '删除成功'));

                        // 成功后移除行：优先服务端返回的 id，其次按钮所在行
                        var removedId = (jsonResult && jsonResult.Data != null) ? jsonResult.Data : id;
                        var row = $('#trDataItem_' + removedId);
                        if (row.length > 0) {
                            row.fadeOut(400, function () { row.remove(); });
                        } else {
                            btn.closest('tr').fadeOut(400, function () { $(this).remove(); });
                        }
                    } else {
                        btn.prop('disabled', false);
                        global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '删除失败'));
                    }
                }, null, function () {
                    // 网络异常等失败场景恢复按钮
                    btn.prop('disabled', false);
                });
            });
        });
    }

    global.AdminPages = {
        bindDelete: bindDelete
    };
})(window);
