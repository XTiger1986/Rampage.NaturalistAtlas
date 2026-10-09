/**
 * admin-city.js — 城市管理页交互
 *
 * 职责：
 *   1. CityPages.bindStatus —— 启停按钮：确认后 POST api/city/status，成功刷新页面
 *   2. CityPages.bindDelete —— 删除按钮：确认后 DELETE api/city/delete/{id}，成功移除行
 *
 * 依赖：rpg-core.js（RPG.confirm / RPG.Post / RPG.Delete）、jQuery
 */
(function (global) {
    'use strict';

    /** 绑定启停切换（按钮 data-id / data-status 携带目标状态） */
    function bindStatus() {
        $(document).on('click', '[data-action=city-status]', function () {
            var btn = $(this);
            var id = btn.attr('data-id');
            var status = btn.attr('data-status');
            if (!id || status === undefined) { return; }

            var text = status === '1' ? '确认启用该城市？' : '确认停用该城市？';
            global.RPG.confirm(text, function () {
                btn.prop('disabled', true);
                global.RPG.Post('/api/city/status', { Id: id, Status: status }, function (jsonResult) {
                    if (global.RPG.isSuccess(jsonResult)) {
                        // 启停影响整行展示，直接刷新列表页
                        window.location.reload();
                    } else {
                        btn.prop('disabled', false);
                        global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '操作失败'));
                    }
                });
            });
        });
    }

    /** 绑定删除（拒绝场景由服务端引用校验返回） */
    function bindDelete() {
        $(document).on('click', '[data-action=city-delete]', function () {
            var btn = $(this);
            var id = btn.attr('data-id');
            if (!id) { return; }

            global.RPG.confirm('确认删除该城市？删除后不可恢复。', function () {
                btn.prop('disabled', true);
                global.RPG.Delete('/api/city/delete/' + id, {}, function (jsonResult) {
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

    global.CityPages = {
        bindStatus: bindStatus,
        bindDelete: bindDelete
    };

    // 页面引入即生效（事件为委托绑定，重复初始化无副作用由 jQuery 保证唯一）
    $(function () {
        bindStatus();
        bindDelete();
    });
})(window);
