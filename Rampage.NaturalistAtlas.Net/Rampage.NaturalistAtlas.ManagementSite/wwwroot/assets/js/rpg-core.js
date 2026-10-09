/**
 * rpg-core.js — 管理后台全站 JS 核心（无依赖，基于 jQuery 3.x）
 *
 * 职责：
 *   1. RPG.FormSubmitter ：表单统一 AJAX 提交（表单编码 / JSON body 双模式），
 *      含输入验证反馈（invalid-feedback）与提示条（sp-tip）联动
 *   2. RPG.Get/Post/Put/Delete ：REST 辅助请求
 *   3. RPG.confirm / showSuccessTip / showErrorTip ：交互反馈
 *
 * 服务端契约（Rampage.Model.R）：
 *   { Code:0, Message, Data, IsSuccess }  — JSON 保持原始大小写
 *   验证失败附带 ValidationResult.Details = { 字段名: { Name, Description } }
 */
(function (global, $) {
    'use strict';

    /** 语言包（由 _Layout 注入覆盖） */
    var Lang = {
        Confirm: '确认',
        Cancel: '取消',
        OperationConfirm: '确认操作',
        NetworkError: '网络异常，请稍后重试',
        OperationFailed: '操作失败'
    };

    /** 判定服务端结果是否成功（Code=0 即成功，与 R.IsSuccess 一致） */
    function isSuccess(jsonResult) {
        return !!jsonResult && (jsonResult.IsSuccess === true || jsonResult.code === 0);
    }

    /** 读取结果消息（兼容 Pascal/camel） */
    function getMessage(jsonResult, fallback) {
        if (!jsonResult) { return fallback || Lang.OperationFailed; }
        return jsonResult.Message || jsonResult.message || fallback || Lang.OperationFailed;
    }

    /** 展示提示条；tipId 为元素 id，ok 控制配色 */
    function showTip(tipId, message, ok) {
        var tip = document.getElementById(tipId);
        if (!tip) { return; }
        tip.textContent = message;
        tip.style.display = 'block';
        tip.classList.remove('sp-tip--error', 'sp-tip--ok');
        tip.classList.add(ok ? 'sp-tip--ok' : 'sp-tip--error');
    }

    /** 全局成功/失败气泡（页面无指定提示条时使用） */
    function showFloatingTip(message, ok) {
        var box = document.createElement('div');
        box.className = 'sp-floating-tip ' + (ok ? 'sp-floating-tip--ok' : 'sp-floating-tip--error');
        box.textContent = message;
        document.body.appendChild(box);
        // 动画结束后移除节点
        window.setTimeout(function () { box.remove(); }, 2600);
    }

    /** 清空全部字段级验证反馈 */
    function clearVerifyFeedback(form) {
        $(form).find('[verify-name]').each(function () {
            this.textContent = '';
            this.style.display = '';
        });
        $(form).find('.is-invalid').removeClass('is-invalid');
    }

    /** 把 { 字段名: 文案 } 回填到 invalid-feedback 容器并高亮输入控件 */
    function fillVerifyFeedback(form, errors) {
        Object.keys(errors).forEach(function (name) {
            var feedback = $(form).find('[verify-name="' + name + '"]');
            if (feedback.length > 0) {
                feedback[0].textContent = errors[name];
                feedback[0].style.display = 'block';
            }
            $(form).find('[name="' + name + '"]').addClass('is-invalid');
        });
    }

    /** 清空并回填验证错误到各字段的 invalid-feedback 容器 */
    function applyVerifyFeedback(form, validationResult) {
        if (!form) { return; }
        clearVerifyFeedback(form);
        var details = validationResult && validationResult.Details;
        if (!details) { return; }
        Object.keys(details).forEach(function (name) {
            var item = details[name];
            var feedback = $(form).find('[verify-name="' + name + '"]');
            if (feedback.length > 0) {
                feedback[0].textContent = item.Description || item.Name || '格式错误';
                feedback[0].style.display = 'block';
            }
            // 高亮对应输入控件（name 相同者）
            $(form).find('[name="' + name + '"]').addClass('is-invalid');
        });
    }

    /** 收集表单数据为对象（含 checkbox 布尔与多选数组） */
    function serializeForm(form) {
        var data = {};
        var formData = new FormData(form);
        formData.forEach(function (value, key) {
            if (data.hasOwnProperty(key)) {
                // 同名多项 → 数组
                if (!Array.isArray(data[key])) { data[key] = [data[key]]; }
                data[key].push(value);
            } else {
                data[key] = value;
            }
        });
        // 未勾选的 checkbox 不会被 FormData 收集，显式补 false
        $(form).find('input[type="checkbox"]').each(function () {
            if (!data.hasOwnProperty(this.name)) { data[this.name] = this.checked; }
        });
        return data;
    }

    /**
     * 表单统一 AJAX 提交器
     * @param {string} formId 表单 id
     * @param {object} options 配置：
     *   useBody            : true 时以 JSON body 提交（服务端 [FromBody] 场景），否则表单编码
     *   useFormData        : true 时以 multipart/form-data 提交（表单含文件 input[type=file] 场景，
     *                        文件随保存动作一起上传，服务端 [FromForm] 模型内含 IFormFile 时使用）
     *   validationHandler  : 提交前校验钩子；返回 true 继续提交，返回 { 字段名: 文案 } 则回填
     *                        invalid-feedback 并中止提交（SourcePower 原版仅有拼错的
     *                        validationHanlder，本站实现为可用版本，勿回拷覆盖）
     *   prepareHandler     : 提交前数据预处理（可修改 data）
     *   completeHandler    : 成功后回调（参数为服务端 R）
     *   failHandler        : 失败后回调（参数为服务端 R 或 null）
     *   tipContainerId     : 提示条元素 id（缺省用全局气泡）
     *   tipAutoHidden      : 1 时提示条 4 秒后自动隐藏
     *   verifyFeedback     : 1 时把 ValidationResult.Details 回填到 invalid-feedback
     *   btnLoading         : 1 时提交按钮进入加载态防重复提交
     */
    function FormSubmitter(formId, options) {
        var settings = $.extend({
            useBody: false,
            useFormData: false,
            validationHandler: null,
            prepareHandler: null,
            completeHandler: null,
            failHandler: null,
            tipContainerId: null,
            tipAutoHidden: 1,
            verifyFeedback: 1,
            btnLoading: 1
        }, options || {});

        var form = document.getElementById(formId);
        if (!form) { return; }

        $(form).on('submit', function (evt) {
            evt.preventDefault();

            var data = serializeForm(form);

            // 提交前校验钩子：返回错误映射则回填并中止
            if (settings.validationHandler) {
                var verdict = settings.validationHandler(data);
                if (verdict !== true && verdict) {
                    clearVerifyFeedback(form);
                    fillVerifyFeedback(form, verdict);
                    return;
                }
            }
            if (settings.prepareHandler) { settings.prepareHandler(data); }

            // multipart 模式：直接以原生 FormData 提交（保留文件流），serializeForm 结果仍用于 prepareHandler
            var formData = null;
            if (settings.useFormData) {
                formData = new FormData(form);
            }

            var ajaxOptions = {
                url: form.action,
                type: form.method || 'POST',
                success: function (jsonResult) {
                    if (isSuccess(jsonResult)) {
                        if (settings.tipContainerId) {
                            showTip(settings.tipContainerId, getMessage(jsonResult, '操作成功'), true);
                        } else {
                            showFloatingTip(getMessage(jsonResult, '操作成功'), true);
                        }
                        if (settings.completeHandler) { settings.completeHandler(jsonResult); }
                    } else {
                        // 验证失败优先回填字段级反馈
                        if (settings.verifyFeedback && jsonResult && jsonResult.ValidationResult) {
                            applyVerifyFeedback(form, jsonResult.ValidationResult);
                        }
                        if (settings.tipContainerId) {
                            showTip(settings.tipContainerId, getMessage(jsonResult), false);
                        } else {
                            showFloatingTip(getMessage(jsonResult), false);
                        }
                        if (settings.failHandler) { settings.failHandler(jsonResult); }
                    }
                },
                error: function () {
                    var message = Lang.NetworkError;
                    if (settings.tipContainerId) {
                        showTip(settings.tipContainerId, message, false);
                    } else {
                        showFloatingTip(message, false);
                    }
                    if (settings.failHandler) { settings.failHandler(null); }
                },
                complete: function () {
                    if (settings.btnLoading) {
                        $(form).find('button[type="submit"]').prop('disabled', false).removeClass('sp-btn-loading');
                    }
                    if (settings.tipAutoHidden && settings.tipContainerId) {
                        window.setTimeout(function () {
                            var tip = document.getElementById(settings.tipContainerId);
                            if (tip) { tip.style.display = 'none'; }
                        }, 4000);
                    }
                }
            };

            // JSON body 模式：服务端参数为 [FromBody] 时必须 JSON，表单编码会 415
            if (settings.useBody) {
                ajaxOptions.contentType = 'application/json';
                ajaxOptions.data = JSON.stringify(data);
            } else if (formData) {
                // multipart 模式：交给浏览器自动生成 boundary，不得手动设置 contentType
                ajaxOptions.contentType = false;
                ajaxOptions.processData = false;
                ajaxOptions.data = formData;
            } else {
                ajaxOptions.data = $.param(data);
            }

            if (settings.btnLoading) {
                $(form).find('button[type="submit"]').prop('disabled', true).addClass('sp-btn-loading');
            }
            $.ajax(ajaxOptions);
        });
    }

    /**
     * REST 辅助：默认表单编码提交（[FromForm] 接口）；useBody:true 时以 JSON body 提交（[FromBody] 接口）。
     * failCallback 为网络层失败（HTTP 错误/超时）专用通道；未提供时回退 callback(null) 兼容旧调用
     */
    function request(method, url, data, callback, useBody, failCallback) {
        var ajaxOptions = {
            url: url,
            type: method,
            success: function (jsonResult) {
                if (callback) { callback(jsonResult); }
            },
            error: function () {
                if (failCallback) { failCallback(); }
                else if (callback) { callback(null); }
            }
        };
        if (useBody) {
            ajaxOptions.data = JSON.stringify(data || {});
            ajaxOptions.contentType = 'application/json';
        }
        else {
            ajaxOptions.data = data || {};
        }
        $.ajax(ajaxOptions);
    }

    function Get(url, data, callback) { request('GET', url, data, callback); }
    function Post(url, data, callback, useBody) { request('POST', url, data, callback, useBody); }
    function Put(url, data, callback, useBody) { request('PUT', url, data, callback, useBody); }
    function Delete(url, data, callback, useBody, failCallback) { request('DELETE', url, data, callback, useBody, failCallback); }

    /**
     * Confirm dialog (Bootstrap modal matching the theme; falls back to the native confirm
     * when bootstrap is unavailable). Keeps the legacy 3-arg call shape.
     * options: { title, okText, danger, requireText }
     *   - danger: red confirm button (irreversible actions such as delete/hide)
     *   - requireText: retype-to-confirm -- the OK button stays disabled until the user
     *     types this exact string (e.g. the observer's login for hide-by-author)
     */
    function confirmBox(text, okCallback, cancelCallback, options) {
        if (!window.bootstrap || !bootstrap.Modal) {
            if (window.confirm(text)) { if (okCallback) { okCallback(); } }
            else if (cancelCallback) { cancelCallback(); }
            return;
        }
        var opt = options || {};
        var id = 'rpgConfirmModal';
        $('#' + id).remove();
        var req = opt.requireText ? String(opt.requireText) : null;
        var requireBlock = req
            ? '<div class="mt-2"><input type="text" class="form-control" id="' + id + 'Req" placeholder="输入 ' + req.replace(/"/g, '&quot;') + ' 以确认" autocomplete="off" /></div>'
            : '';
        var html = ''
            + '<div class="modal fade" id="' + id + '" tabindex="-1" aria-hidden="true">'
            + '<div class="modal-dialog modal-dialog-centered modal-sm"><div class="modal-content sp-modal-content">'
            + '<div class="modal-header"><h6 class="modal-title">' + (opt.title || '请确认') + '</h6>'
            + '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="关闭"></button></div>'
            + '<div class="modal-body"><div class="small text-break">' + text + '</div>' + requireBlock + '</div>'
            + '<div class="modal-footer py-2">'
            + '<button type="button" class="btn btn-sm btn-outline-secondary" data-bs-dismiss="modal">取消</button>'
            + '<button type="button" class="btn btn-sm ' + (opt.danger ? 'btn-danger' : 'btn-primary') + '" id="' + id + 'Ok">' + (opt.okText || '确认') + '</button>'
            + '</div></div></div></div>';
        $('body').append(html);
        var $modal = $('#' + id);
        var modal = new bootstrap.Modal($modal[0]);
        var done = false;
        var $ok = $('#' + id + 'Ok');
        if (req) {
            $ok.prop('disabled', true);
            $modal.on('input', '#' + id + 'Req', function () {
                $ok.prop('disabled', $.trim($(this).val()) !== req);
            });
        }
        $ok.on('click', function () {
            done = true;
            modal.hide();
            if (okCallback) { okCallback(); }
        });
        $modal[0].addEventListener('hidden.bs.modal', function () {
            if (!done && cancelCallback) { cancelCallback(); }
            $modal.remove();
        });
        modal.show();
    }

    function showSuccessTip(message) { showFloatingTip(message, true); }
    function showErrorTip(message) { showFloatingTip(message, false); }

    global.RPG = {
        Lang: Lang,
        isSuccess: isSuccess,
        getMessage: getMessage,
        FormSubmitter: FormSubmitter,
        Get: Get,
        Post: Post,
        Put: Put,
        Delete: Delete,
        confirm: confirmBox,
        showSuccessTip: showSuccessTip,
        showErrorTip: showErrorTip
    };
})(window, jQuery);
