/**
 * APK 应用包 · 签名配置 —— 静态交互脚本
 * 仅用于页面演示：APK 列表、搜索过滤、查看 SHA1、打开文件夹
 */
(function () {
    'use strict';

    const $ = (sel) => document.querySelector(sel);
    const $$ = (sel) => Array.prototype.slice.call(document.querySelectorAll(sel));

    /* ---------------- Toast ---------------- */
    let toastTimer = null;

    function toast(text) {
        const el = $('#toast');
        el.textContent = text;
        el.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove('show'), 2000);
    }

    /* ---------------- 模拟数据 ---------------- */
    const APKS = [
        { name: '微信', version: '1.0.1.0', size: '2.14 MB', time: '2026-09-26 14:21' },
        { name: '抖音', version: '1.2.3', size: '3.02 MB', time: '2026-09-26 11:04' },
        { name: '哔哩哔哩', version: '2.0.5', size: '2.66 MB', time: '2026-09-25 20:31' },
        { name: '淘宝', version: '3.4.1', size: '1.87 MB', time: '2026-09-25 17:55' },
        { name: '支付宝', version: '10.5.20', size: '3.41 MB', time: '2026-09-24 22:13' },
        { name: '高德地图', version: '13.1.0', size: '2.05 MB', time: '2026-09-24 19:05' },
        { name: '网易云音乐', version: '8.9.20', size: '1.53 MB', time: '2026-09-23 16:39' },
        { name: '钉钉', version: '7.0.1', size: '2.91 MB', time: '2026-09-23 10:47' },
        { name: '知乎', version: '9.8.1', size: '1.24 MB', time: '2026-09-22 11:48' },
        { name: '京东', version: '12.2.0', size: '2.38 MB', time: '2026-09-21 09:33' },
        { name: '美团', version: '11.6.3', size: '1.66 MB', time: '2026-09-20 15:21' },
        { name: '小红书', version: '8.11.5', size: '2.72 MB', time: '2026-09-19 18:02' }
    ];

    /* ---------------- 渲染 ---------------- */
    function iconHTML(name) {
        return '<span class="apk-icon">' +
            '<svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">' +
            '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" ' +
            'd="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>' +
            '<polyline points="3.27 6.96 12 12.01 20.73 6.96" fill="none" stroke="currentColor" stroke-width="2"/>' +
            '<line x1="12" y1="22.08" x2="12" y2="12" stroke="currentColor" stroke-width="2"/></svg>' +
            '</span>';
    }

    function cardHTML(a) {
        return '<li class="apk-card" data-name="' + a.name + '">' +
            iconHTML(a.name) +
            '<div class="apk-info">' +
            '<div class="apk-name">' + a.name + '</div>' +
            '<div class="apk-meta">' +
            '<span class="tag">v' + a.version + '</span>' +
            '<span>' + a.size + '</span>' +
            '<span>' + a.time.slice(5, 16) + '</span>' +
            '</div></div></li>';
    }

    function render() {
        const grid = $('#apkGrid');
        const kw = $('#apkSearch').value.trim().toLowerCase();
        const list = APKS.filter(function (a) {
            return a.name.toLowerCase().indexOf(kw) !== -1;
        });

        grid.innerHTML = list.map(cardHTML).join('');
        $('#apkCount').textContent = list.length;
        $('#emptyState').classList.toggle('show', list.length === 0);
    }

    /* ---------------- 搜索 ---------------- */
    $('#apkSearch').addEventListener('input', render);

    /* ---------------- 刷新 ---------------- */
    $('#btnRefresh').addEventListener('click', function () {
        toast('正在扫描目录：' + $('#apkRoot').value);
        render();
    });

    /* ---------------- 浏览目录 ---------------- */
    $('#btnRoot').addEventListener('click', function () {
        toast('演示：此处调用 FolderBrowserDialog 选择目录');
    });

    $('#btnKey').addEventListener('click', function () {
        toast('演示：选择签名证书（*.jks）');
    });

    /* ---------------- 查看 SHA1 ---------------- */
    $('#btnSha1').addEventListener('click', function () {
        const alias = $('#keyAlias').value || 'release';
        const text = [
            '别名: ' + alias,
            '创建日期: 2025-6-10',
            '条目类型: PrivateKeyEntry',
            '证书链长度: 1',
            '证书[1]:',
            '所有者: CN=DevKit, OU=Dev, O=DevKit, L=Shenzhen, ST=Guangdong, C=CN',
            '发布者: CN=DevKit, OU=Dev, O=DevKit, L=Shenzhen, ST=Guangdong, C=CN',
            '序列号: 7a3f2c1e',
            '生效时间: Tue Jun 10 10:00:00 CST 2025, 失效时间: Fri Jun 05 10:00:00 CST 2125',
            '证书指纹:',
            '\t SHA1值: 12:34:56:78:9A:BC:DE:F0:11:22:33:44:55:66:77:88:99:AA:BB:CC',
            '\t SHA256值: AB:CD:EF:01:23:45:67:89:0A:BC:DE:F0:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33',
            '签名算法名称: SHA256withRSA',
            '主体公共密钥算法: 2048 位 RSA 密钥'
        ].join('\n');
        $('#terminalText').textContent = text;
        toast('已获取证书指纹');
    });

    /* ---------------- 打开文件夹 ---------------- */
    $('#apkGrid').addEventListener('click', function (e) {
        const card = e.target.closest('.apk-card');
        if (!card) return;
        toast('演示：explorer.exe /select,' + $('#apkRoot').value + '\\' + card.dataset.name + '.apk');
    });

    /* ---------------- 启动 ---------------- */
    render();
})();
