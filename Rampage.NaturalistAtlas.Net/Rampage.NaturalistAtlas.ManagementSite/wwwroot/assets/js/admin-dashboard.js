/**
 * admin-dashboard.js — 运营看板（首页）图表渲染
 *
 * 职责：
 *   1. 拉取 /api/home/dashboard 聚合数据
 *   2. 渲染六张 Chart.js 图表：类群构成（环形）/ 近30天入库（折线）/ 活跃城市Top10（条形）/
 *      采集失败趋势（折线）/ 举报量趋势（折线）/ 近12月观察（柱状）
 *   3. 填充同步健康度表与新鲜度徽标
 *
 * 约定：所有外部文本（任务摘要/城市名等来自数据库与源站报错）经 esc() 转义后再拼接——
 * sync_log.message 是源站原文，含尖括号时必须当纯文本处理
 *
 * 依赖：chart.umd.min.js、rpg-core.js（RPG.isSuccess / showErrorTip）、jQuery
 */
(function (global) {
    'use strict';

    /** 自然色板：蕨绿系 + 琥珀点缀（暗底高对比） */
    var PALETTE = ['#3fbf7f', '#6fd6a0', '#9ce8bd', '#2f9663', '#c9ecd7', '#e8b45a', '#f0c987', '#7bae95', '#93b3a0'];
    var TEXT_COLOR = '#93b3a0';
    var GRID_COLOR = 'rgba(178, 226, 194, .1)';
    var FAIL_COLOR = '#e06c5a';

    /** HTML 转义（任务摘要/城市名等外部文本，拼接 innerHTML 前必须经过） */
    function esc(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function formatNumber(value) {
        return (value || 0).toLocaleString('zh-CN');
    }

    /** 数量缩写：1234 → 1.2k（y 轴刻度） */
    function compactNumber(value) {
        if (value >= 10000) { return Math.round(value / 1000) + 'w'; }
        if (value >= 1000) { return (value / 1000).toFixed(1) + 'k'; }
        return String(value);
    }

    /** 基础坐标轴样式（暗色 + 千分位/缩写刻度） */
    function axisOptions(compact) {
        return {
            ticks: {
                color: TEXT_COLOR,
                font: { size: 11 },
                callback: function (v) { return compact ? compactNumber(v) : formatNumber(v); }
            },
            grid: { color: GRID_COLOR }
        };
    }

    /** 通用 tooltip 回调：数值千分位 */
    var tooltipNumber = {
        callbacks: { label: function (ctx) { return formatNumber(ctx.parsed.y != null ? ctx.parsed.y : ctx.parsed); } }
    };

    /** 通用 tooltip 回调：环形图显示「名目 数量 (百分比)」 */
    var tooltipSlice = {
        callbacks: {
            label: function (ctx) {
                var total = ctx.dataset.data.reduce(function (a, b) { return a + b; }, 0) || 1;
                var pct = (ctx.parsed / total * 100).toFixed(1);
                return ' ' + ctx.label + '：' + formatNumber(ctx.parsed) + '（' + pct + '%）';
            }
        }
    };

    /** 容器宽度判定：环形图 legend 窄容器换到下方（Chart.js 不监听 legend 位置的容器变化，渲染时定档） */
    function legendPosition(canvas) {
        return canvas && canvas.parentElement && canvas.parentElement.clientWidth < 420 ? 'bottom' : 'right';
    }

    function renderIconicChart(canvas, sliceList) {
        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: sliceList.map(function (s) { return s.Label; }),
                datasets: [{
                    data: sliceList.map(function (s) { return s.Value; }),
                    backgroundColor: PALETTE,
                    borderColor: 'rgba(8, 18, 12, .9)',
                    borderWidth: 2
                }]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '58%',
                plugins: {
                    legend: { position: legendPosition(canvas), labels: { color: TEXT_COLOR, boxWidth: 12, font: { size: 12 } } },
                    tooltip: tooltipSlice
                }
            }
        });
    }

    function renderTrendChart(canvas, pointList) {
        new Chart(canvas, {
            type: 'line',
            data: {
                labels: pointList.map(function (p) { return p.Label.slice(5); }),
                datasets: [{
                    label: '入库观察',
                    data: pointList.map(function (p) { return p.Value; }),
                    borderColor: '#3fbf7f',
                    backgroundColor: 'rgba(63, 191, 127, .15)',
                    fill: true,
                    tension: .35,
                    pointRadius: 2,
                    pointBackgroundColor: '#6fd6a0'
                }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: tooltipNumber },
                scales: { x: axisOptions(false), y: Object.assign(axisOptions(true), { beginAtZero: true }) }
            }
        });
    }

    function renderMonthlyChart(canvas, pointList) {
        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: pointList.map(function (p) { return p.Label; }),
                datasets: [{
                    label: '观察数',
                    data: pointList.map(function (p) { return p.Value; }),
                    backgroundColor: 'rgba(63, 191, 127, .55)',
                    hoverBackgroundColor: '#3fbf7f',
                    borderRadius: 4,
                    maxBarThickness: 34
                }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: tooltipNumber },
                scales: { x: axisOptions(false), y: Object.assign(axisOptions(true), { beginAtZero: true }) }
            }
        });
    }

    /** 活跃城市 Top10：横向条形（城市名较长，横条留足标签空间） */
    function renderCityTopChart(canvas, sliceList) {
        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: sliceList.map(function (s) { return s.Label; }),
                datasets: [{
                    label: '观察数',
                    data: sliceList.map(function (s) { return s.Value; }),
                    backgroundColor: 'rgba(63, 191, 127, .55)',
                    hoverBackgroundColor: '#3fbf7f',
                    borderRadius: 4,
                    maxBarThickness: 20
                }]
            },
            options: {
                maintainAspectRatio: false,
                indexAxis: 'y',
                plugins: { legend: { display: false }, tooltip: tooltipNumber },
                scales: { x: Object.assign(axisOptions(true), { beginAtZero: true }), y: axisOptions(false) }
            }
        });
    }

    /** 失败/举报小趋势：细折线（有事件才画点，稀疏数据更醒目） */
    function renderEventTrendChart(canvas, pointList, color, label) {
        new Chart(canvas, {
            type: 'line',
            data: {
                labels: pointList.map(function (p) { return p.Label.slice(5); }),
                datasets: [{
                    label: label,
                    data: pointList.map(function (p) { return p.Value; }),
                    borderColor: color,
                    backgroundColor: color + '26',
                    fill: true,
                    tension: .25,
                    pointRadius: function (ctx) { return ctx.parsed.y > 0 ? 3 : 0; },
                    pointBackgroundColor: color
                }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: tooltipNumber },
                scales: { x: axisOptions(false), y: Object.assign(axisOptions(true), { beginAtZero: true, ticks: { color: TEXT_COLOR, font: { size: 11 }, stepSize: 1, precision: 0 } }) }
            }
        });
    }

    function renderHealthTable(tbody, healthList, timeZoneLabel) {
        // 全部经 esc() 转义：LastRunMessage 是源站/运行时原文，必须按纯文本渲染
        var html = '';
        for (var i = 0; i < healthList.length; i++) {
            var item = healthList[i];
            var ok = !!item.IsHealthy;
            html += '<tr>'
                + '<td><span title="' + esc(item.TaskCode) + '">' + esc(item.Task) + '</span></td>'
                + '<td><span class="dash-health-dot ' + (ok ? 'dash-health-dot--ok' : 'dash-health-dot--bad') + '"></span>' + esc(item.LastRunStatusText) + '</td>'
                + '<td>' + (item.LastRunTime ? new Date(item.LastRunTime + 'Z').toLocaleString('zh-CN') : '—') + '</td>'
                + '<td class="small">' + (item.LastRunMessage ? esc(item.LastRunMessage) : '-') + '</td>'
                + '</tr>';
        }
        if (!html) { html = '<tr><td colspan="4" class="sp-empty">暂无采集任务（等待采集端初始化）</td></tr>'; }
        tbody.innerHTML = html;
    }

    function init() {
        $.getJSON('/api/home/dashboard', function (jsonResult) {
            // 首页对所有登录管理员开放（登录固定落 /），但看板数据要 DashboardManagement 权限：
            // 无权限（4001）按空看板优雅降级，不再弹"加载失败"红条误导排障
            // The home page is open to every logged-in admin (login lands at /) while the data needs
            // DashboardManagement: no-permission (4001) degrades to an empty dashboard instead of a
            // misleading "load failed" error
            if (jsonResult && jsonResult.Code === 4001) {
                $('#dashHealthBody').html('<tr><td colspan="4" class="sp-empty">当前账号无运营看板权限，请联系管理员分配</td></tr>');
                return;
            }
            if (!global.RPG.isSuccess(jsonResult) || !jsonResult.Data) {
                global.RPG.showErrorTip(global.RPG.getMessage(jsonResult, '看板数据加载失败'));
                return;
            }
            var data = jsonResult.Data;

            $('#dashCity').text(data.CityName || '全部城市');
            $('#dashTotal').text(formatNumber(data.TotalObservation));
            $('#dashResearch').text(data.ResearchRatio == null ? '—' : (data.ResearchRatio * 100).toFixed(1) + '%');
            $('#dashPendingReport').text(formatNumber(data.PendingReportCount));
            $('#dashMonthInsert').text(formatNumber(data.MonthInsertCount));
            // 新鲜度：<1 天（含时钟偏差的负值）显示「今天」
            var freshnessText = '—';
            if (data.FreshnessDays != null) {
                freshnessText = data.FreshnessDays < 1 ? '今天' : data.FreshnessDays + ' 天前';
            }
            $('#dashFreshness').text(freshnessText);

            if (data.IconicDistribution.length > 0) {
                renderIconicChart(document.getElementById('chartIconic'), data.IconicDistribution);
            } else {
                document.getElementById('chartIconicEmpty').style.display = '';
            }
            if (data.Recent30Days.length > 0) {
                renderTrendChart(document.getElementById('chartTrend'), data.Recent30Days);
            } else {
                document.getElementById('chartTrendEmpty').style.display = '';
            }
            if (data.CityTop10 && data.CityTop10.length > 0) {
                renderCityTopChart(document.getElementById('chartCityTop'), data.CityTop10);
            } else {
                document.getElementById('chartCityTopEmpty').style.display = '';
            }
            var syncFailTotal = (data.SyncFailure30Days || []).reduce(function (a, p) { return a + p.Value; }, 0);
            if (syncFailTotal > 0) {
                renderEventTrendChart(document.getElementById('chartSyncFail'), data.SyncFailure30Days, FAIL_COLOR, '失败/熔断');
            } else {
                document.getElementById('chartSyncFailEmpty').style.display = '';
            }
            var reportTotal = (data.Report30Days || []).reduce(function (a, p) { return a + p.Value; }, 0);
            if (reportTotal > 0) {
                renderEventTrendChart(document.getElementById('chartReport'), data.Report30Days, '#e8b45a', '举报量');
            } else {
                document.getElementById('chartReportEmpty').style.display = '';
            }
            if (data.Monthly12.length > 0) {
                renderMonthlyChart(document.getElementById('chartMonthly'), data.Monthly12);
            } else {
                document.getElementById('chartMonthlyEmpty').style.display = '';
            }
            renderHealthTable(document.getElementById('dashHealthBody'), data.SyncHealthList || []);
        }).fail(function () {
            global.RPG.showErrorTip('看板数据加载失败，请稍后重试');
        });
    }

    $(init);
})(window);
