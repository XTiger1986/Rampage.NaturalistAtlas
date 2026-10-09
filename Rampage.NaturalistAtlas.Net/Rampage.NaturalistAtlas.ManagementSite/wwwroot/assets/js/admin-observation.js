/**
 * admin-observation.js — 内容治理页交互（观察列表/详情）
 *
 * 职责：
 *   1. ObservationPages.bindStatus —— 行内隐藏/恢复：确认后 POST api/observation/status，成功刷新
 *   2. ObservationPages.bindHideByAuthor —— 按作者批量隐藏：输入昵称确认后 POST api/observation/hide-by-author
 *
 * 依赖：rpg-core.js（RPG.confirm / RPG.Post / 提示气泡）、jQuery
 */
(function (global) {
    'use strict';

    /** 绑定隐藏/恢复切换（按钮 data-id / data-status 携带目标状态） */
    function bindStatus() {
        $(document).on('click', '[data-action=obs-status]', function () {
            var btn = $(this);
            var id = btn.attr('data-id');
            var status = btn.attr('data-status');
            if (!id || status === undefined) { return; }

            var text = status === '1' ? '确认隐藏该观察？主站将在隐藏缓存时限内不再展示。' : '确认恢复该观察为正常展示？';
            global.RPG.confirm(text, function () {
                btn.prop('disabled', true);
                global.RPG.Post('/api/observation/status', { ObservationId: id, Status: status }, function (jsonResult) {
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

    /** 绑定按作者批量隐藏（列表页右上角） */
    function bindHideByAuthor() {
        $('#btnHideByAuthor').on('click', function () {
            var login = $.trim($('#txtHideAuthor').val() || '');
            if (!login) {
                global.RPG.showErrorTip('请先输入观察者昵称');
                $('#txtHideAuthor').trigger('focus');
                return;
            }
            // 影响面大：复读昵称确认（确认钮在输入匹配前保持禁用）
            global.RPG.confirm('确认隐藏观察者「<b>' + login.replace(/</g, '&lt;') + '</b>」名下<b>全部</b>正常观察？该操作影响范围较大。', function () {
                $('#btnHideByAuthor').prop('disabled', true);
                global.RPG.Post('/api/observation/hide-by-author', { UserLogin: login }, function (jsonResult) {
                    $('#btnHideByAuthor').prop('disabled', false);
                    if (global.RPG.isSuccess(jsonResult)) {
                        global.RPG.showSuccessTip('已隐藏 ' + (jsonResult.Data || 0) + ' 条观察');
                        // 稍候刷新以展示新状态
                        window.setTimeout(function () { window.location.reload(); }, 900);
                    } else {
                        global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '操作失败'));
                    }
                });
            }, null, { title: '按作者隐藏', danger: true, okText: '隐藏', requireText: login });
        });
    }

    global.ObservationPages = {
        bindStatus: bindStatus,
        bindHideByAuthor: bindHideByAuthor
    };

    // 页面引入即生效（事件为委托绑定）
    $(function () {
        bindStatus();
        bindHideByAuthor();
    });
})(window);
