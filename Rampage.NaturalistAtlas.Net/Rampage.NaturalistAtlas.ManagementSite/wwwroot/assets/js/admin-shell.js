/**
 * admin-shell.js — 管理后台外壳交互
 *
 * 职责：
 *   1. 侧栏开合（data-sp-toggle-sidebar / 窄屏遮罩）
 *   2. 侧栏折叠子菜单（data-sp-nav-toggle）
 *   3. 顶栏管理员下拉（data-sp-admin-menu）
 *   4. 退出登录（data-sp-logout → POST /api/passport/logout）
 *
 * 依赖：rpg-core.js（RPG.Post）、jQuery
 */
(function (global) {
    'use strict';

    /** 侧栏开合：body 挂 sp-sidebar-open 控制位移与遮罩 */
    function bindSidebarToggle() {
        var toggles = document.querySelectorAll('[data-sp-toggle-sidebar]');
        if (toggles.length === 0) { return; }

        toggles.forEach(function (btn) {
            btn.addEventListener('click', function (evt) {
                // 遮罩自身点击时阻止链接/按钮默认行为
                if (evt.currentTarget.classList.contains('sp-sidebar-backdrop')) {
                    evt.preventDefault();
                }
                document.body.classList.toggle('sp-sidebar-open');
            });
        });
    }

    /** 折叠子菜单：点击带子级的导航项切换 open */
    function bindNavToggle() {
        var navToggles = document.querySelectorAll('[data-sp-nav-toggle]');
        navToggles.forEach(function (link) {
            link.addEventListener('click', function (evt) {
                // 无真实地址的父级菜单（javascript:void(0)）阻止默认跳转
                var href = link.getAttribute('href') || '';
                if (href === 'javascript:void(0)' || href === '' || href === '#') {
                    evt.preventDefault();
                }
                var group = link.closest('.sp-nav-group');
                if (group) {
                    group.classList.toggle('open');
                }
            });
        });
    }

    /** 顶栏管理员下拉：点击 chip 开合，点击外部关闭 */
    function bindAdminMenu() {
        var menu = document.getElementById('spAdminMenu');
        if (!menu) { return; }
        var chip = menu.querySelector('[data-sp-admin-menu]');
        if (!chip) { return; }

        chip.addEventListener('click', function (evt) {
            evt.stopPropagation();
            var isOpen = menu.classList.toggle('open');
            chip.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });

        document.addEventListener('click', function (evt) {
            if (!menu.contains(evt.target)) {
                menu.classList.remove('open');
                chip.setAttribute('aria-expanded', 'false');
            }
        });
    }

    /** 退出登录：调框架接口清 cookie 后回登录页 */
    function bindLogout() {
        var logoutBtn = document.querySelector('[data-sp-logout]');
        if (!logoutBtn) { return; }

        logoutBtn.addEventListener('click', function (evt) {
            evt.preventDefault();
            global.RPG.confirm('确认退出当前账号？', function () {

            var done = function () {
                window.location.href = '/Passport/SignIn';
            };

            if (global.RPG && global.RPG.Post) {
                global.RPG.Post('/api/passport/logout', {}, done);
            } else {
                // rpg-core 未加载时兜底
                fetch('/api/passport/logout', { method: 'POST' }).then(done, done);
            }
            });
        });
    }

    function init() {
        bindSidebarToggle();
        bindNavToggle();
        bindAdminMenu();
        bindLogout();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})(window);
