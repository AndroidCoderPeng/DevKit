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
    let scanTimer = null;

    function toast(text) {
        const el = $('#toast');
        el.textContent = text;
        el.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove('show'), 2000);
    }

    function setScanProgress(percent) {
        const bar = $('#scanProgressBar');
        const text = $('#scanPercentText');
        if (!bar || !text) return;
        const value = Math.max(0, Math.min(100, percent));
        bar.style.width = value + '%';
        text.textContent = Math.round(value) + '%';
    }

    function startScanProgress() {
        const btn = $('#btnRefresh');
        const panel = $('#scanProgress');
        if (!btn || !panel) return;

        clearInterval(scanTimer);
        btn.disabled = true;
        panel.classList.remove('hidden');
        setScanProgress(0);

        let progress = 0;
        scanTimer = setInterval(function () {
            progress += 7 + Math.random() * 14;
            if (progress >= 98) {
                progress = 98;
            }
            setScanProgress(progress);
        }, 120);
    }

    function finishScanProgress() {
        clearInterval(scanTimer);
        const btn = $('#btnRefresh');
        const panel = $('#scanProgress');
        if (btn) {
            btn.disabled = false;
        }
        setScanProgress(100);
        if (panel) {
            setTimeout(function () {
                panel.classList.add('hidden');
            }, 500);
        }
    }

    /* ---------------- 模拟数据 ---------------- */
    const APKS = [
        { fileName: '微信', fullName: 'D:\\APK\\release\\微信_20260926_1.0.1.0.apk', fileSize: '2.14 MB', modifyTime: '2026-09-26 14:21:08', buildTime: '20260926', version: '1.0.1.0', extraInfo: '' },
        { fileName: '抖音', fullName: 'D:\\APK\\release\\抖音_20260926_1.2.3.apk', fileSize: '3.02 MB', modifyTime: '2026-09-26 11:04:21', buildTime: '20260926', version: '1.2.3', extraInfo: '' },
        { fileName: '哔哩哔哩', fullName: 'D:\\APK\\release\\哔哩哔哩_20260925_2.0.5_release.apk', fileSize: '2.66 MB', modifyTime: '2026-09-25 20:31:42', buildTime: '20260925', version: '2.0.5', extraInfo: 'release' },
        { fileName: '淘宝', fullName: 'D:\\APK\\release\\淘宝_20260925_3.4.1.apk', fileSize: '1.87 MB', modifyTime: '2026-09-25 17:55:06', buildTime: '20260925', version: '3.4.1', extraInfo: '' },
        { fileName: '支付宝', fullName: 'D:\\APK\\release\\支付宝_20260924_10.5.20.apk', fileSize: '3.41 MB', modifyTime: '2026-09-24 22:13:18', buildTime: '20260924', version: '10.5.20', extraInfo: '' },
        { fileName: '高德地图', fullName: 'D:\\APK\\release\\高德地图_20260924_13.1.0.apk', fileSize: '2.05 MB', modifyTime: '2026-09-24 19:05:32', buildTime: '20260924', version: '13.1.0', extraInfo: '' },
        { fileName: '网易云音乐', fullName: 'D:\\APK\\release\\网易云音乐_20260923_8.9.20.apk', fileSize: '1.53 MB', modifyTime: '2026-09-23 16:39:04', buildTime: '20260923', version: '8.9.20', extraInfo: '' },
        { fileName: '钉钉', fullName: 'D:\\APK\\release\\钉钉_20260923_7.0.1.apk', fileSize: '2.91 MB', modifyTime: '2026-09-23 10:47:15', buildTime: '20260923', version: '7.0.1', extraInfo: '' },
        { fileName: '知乎', fullName: 'D:\\APK\\release\\知乎_20260922_9.8.1.apk', fileSize: '1.24 MB', modifyTime: '2026-09-22 11:48:27', buildTime: '20260922', version: '9.8.1', extraInfo: '' },
        { fileName: '京东', fullName: 'D:\\APK\\release\\京东_20260921_12.2.0.apk', fileSize: '2.38 MB', modifyTime: '2026-09-21 09:33:11', buildTime: '20260921', version: '12.2.0', extraInfo: '' },
        { fileName: '美团', fullName: 'D:\\APK\\release\\美团_20260920_11.6.3.apk', fileSize: '1.66 MB', modifyTime: '20260920', buildTime: '20260920', version: '11.6.3', extraInfo: '' },
        { fileName: '小红书', fullName: 'D:\\APK\\release\\小红书_20260919_8.11.5.apk', fileSize: '2.72 MB', modifyTime: '2026-09-19 18:02:50', buildTime: '20260919', version: '8.11.5', extraInfo: '' }
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
        const extra = a.extraInfo ? '<span>' + a.extraInfo + '</span>' : '';
        const version = a.version ? '<span class="tag">v' + a.version + '</span>' : '<span class="tag">未解析版本</span>';
        const build = a.buildTime ? a.buildTime.slice(0, 4) + '-' + a.buildTime.slice(4, 6) + '-' + a.buildTime.slice(6, 8) : '未解析日期';
        return '<li class="apk-card" data-path="' + a.fullName.replace(/"/g, '&quot;') + '">' +
            iconHTML(a.fileName) +
            '<div class="apk-info">' +
            '<div class="apk-name">' + a.fileName + '</div>' +
            '<div class="apk-meta">' +
            version + '<span>' + a.fileSize + '</span><span>' + build + '</span>' + extra +
            '</div></div></li>';
    }

    function render() {
        const grid = $('#apkGrid');
        const kw = $('#apkSearch').value.trim().toLowerCase();
        const list = APKS.filter(function (a) {
            return [a.fileName, a.version, a.extraInfo].join(' ').toLowerCase().indexOf(kw) !== -1;
        });

        grid.innerHTML = list.map(cardHTML).join('');
        $('#apkCount').textContent = list.length;
        $('#emptyState').classList.toggle('show', list.length === 0);
        $('#footerStatus').textContent = list.length ? '最近扫描：刚刚' : '目录为空或无匹配结果';
    }

    /* ---------------- 搜索 ---------------- */
    $('#apkSearch').addEventListener('input', render);

    /* ---------------- 刷新 ---------------- */
    $('#btnRefresh').addEventListener('click', function () {
        const root = $('#apkRoot').value.trim() || 'D:\\APK\\release';
        toast('正在扫描目录：' + root);
        $('#headerStatus').textContent = '扫描中';
        startScanProgress();

        setTimeout(function () {
            render();
            finishScanProgress();
            $('#headerStatus').textContent = '已就绪';
            toast('扫描完成：已遍历 ' + root);
        }, 1600);
    });

    /* ---------------- 浏览目录 ---------------- */
    $('#btnRoot').addEventListener('click', function () {
        toast('演示：此处调用 FolderBrowserDialog 选择目录');
    });

    $('#btnKey').addEventListener('click', function () {
        toast('演示：选择签名证书（*.jks）');
    });

    $('#btnJdk').addEventListener('click', function () {
        toast('演示：选择 JDK 安装目录（例如 C:\\Program Files\\Java\\jdk-17）');
    });

    /* ---------------- 查看 SHA1 ---------------- */
    $('#btnSha1').addEventListener('click', function () {
        const alias = $('#keyAlias').value || 'release';
        const jdkPath = $('#jdkPath').value.trim() || '未配置';
        const text = [
            'JDK 路径: ' + jdkPath,
            'JAVA_HOME: ' + (jdkPath === '未配置' ? '未配置' : jdkPath),
            '',
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
        toast('已获取证书指纹，并读取 JDK 路径');
    });

    /* ---------------- 打开文件夹 ---------------- */
    $('#apkGrid').addEventListener('click', function (e) {
        const card = e.target.closest('.apk-card');
        if (!card) return;
        toast('演示：打开目录 ' + card.dataset.path);
    });

    /* ---------------- 启动 ---------------- */
    render();
})();
