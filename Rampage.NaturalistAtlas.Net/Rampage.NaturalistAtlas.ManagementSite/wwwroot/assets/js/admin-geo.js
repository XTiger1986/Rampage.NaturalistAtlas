/**
 * admin-geo.js — 地理档案页交互（国家/省份）
 *
 * 职责（按钮以 data-api 携带接口前缀，通用绑定）：
 *   GeoPages.bindStatus —— 启停按钮（data-id/data-status/data-api）→ POST {api}/status
 *   GeoPages.bindDelete —— 删除按钮（data-id/data-api）→ DELETE {api}/delete/{id}
 *
 * 依赖：rpg-core.js（RPG.confirm / RPG.Post / RPG.Delete / 提示气泡）、jQuery
 */
(function (global) {
    'use strict';

    /** 启停切换：成功后整页刷新（影响徽章与按钮组） */
    function bindStatus() {
        $(document).on('click', '[data-action=geo-status]', function () {
            var btn = $(this);
            var id = btn.attr('data-id');
            var status = btn.attr('data-status');
            var api = btn.attr('data-api');
            if (!id || status === undefined || !api) { return; }

            var text = status === '1' ? '确认启用该档案？' : '确认停用该档案？';
            global.RPG.confirm(text, function () {
                btn.prop('disabled', true);
                global.RPG.Post(api + '/status', { Id: id, Status: status }, function (jsonResult) {
                    if (global.RPG.isSuccess(jsonResult)) {
                        window.location.reload();
                    } else {
                        btn.prop('disabled', false);
                        global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '操作失败'));
                    }
                });
            });
        });
    }

    /** 删除（拒绝场景由服务端引用校验返回） */
    function bindDelete() {
        $(document).on('click', '[data-action=geo-delete]', function () {
            var btn = $(this);
            var id = btn.attr('data-id');
            var api = btn.attr('data-api');
            if (!id || !api) { return; }

            global.RPG.confirm('确认删除该档案？被下级档案引用时将被拒绝。', function () {
                btn.prop('disabled', true);
                global.RPG.Delete(api + '/delete/' + id, {}, function (jsonResult) {
                    if (global.RPG.isSuccess(jsonResult)) {
                        global.RPG.showSuccessTip(global.RPG.getMessage(jsonResult, '删除成功'));
                        var row = $('#trDataItem_' + id);
                        if (row.length > 0) {
                            row.fadeOut(400, function () { row.remove(); });
                        }
                    } else {
                        btn.prop('disabled', false);
                        global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '删除失败'));
                    }
                });
            });
        });
    }

    global.GeoPages = {
        bindStatus: bindStatus,
        bindDelete: bindDelete
    };

    // 页面引入即生效（事件为委托绑定）
    $(function () {
        bindStatus();
        bindDelete();
    });
})(window);
