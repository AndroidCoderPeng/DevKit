/**
 * ADB 界面重设计稿 —— 静态交互脚本
 * 仅用于页面演示：主题切换、复制提示、包名选择、右键菜单、搜索过滤、排序
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
        toastTimer = setTimeout(() => el.classList.remove('show'), 1800);
    }

    /* ---------------- 点击复制 ---------------- */
    $$('.info-item.copyable').forEach(function (item) {
        item.addEventListener('click', function () {
            const text = item.dataset.copy || '';
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).catch(() => {
                });
            }
            toast('参数已复制：' + text);
        });
    });

    /* ---------------- 设备切换 ---------------- */
    const deviceProfiles = {
        '192.168.3.126:5555': {
            name: 'HONOR WDY-AN00',
            abi: 'arm64-v8a, armeabi-v7a, armeabi',
            version: '13（API 33）',
            size: '720x1612 · 320 dpi',
            battery: 44
        },
        'emulator-5554': {
            name: 'Android SDK Pixel 6',
            abi: 'x86_64, x86',
            version: '14（API 34）',
            size: '1080 × 2400',
            battery: 100
        },
        '9f2ac8d1': {
            name: 'Google Pixel 7',
            abi: 'arm64-v8a, armeabi-v7a',
            version: '14（API 34）',
            size: '1080 × 2400',
            battery: 78
        }
    };

    $('#deviceSelect').addEventListener('change', function (e) {
        const profile = deviceProfiles[e.target.value];
        if (!profile) return;
        $('#deviceSerial').textContent = e.target.value;
        $('.device-name').textContent = profile.name;
        $('.info-grid .info-item:nth-child(3) .info-value').textContent = profile.abi;
        $('.info-grid .info-item:nth-child(2) .info-value').textContent = profile.version;
        $('.info-grid .info-item:nth-child(4) .info-value').textContent = profile.size;
        $('#batteryPercent').textContent = profile.battery;
        $('#batteryFill').style.width = profile.battery + '%';
        toast('已切换到 ' + profile.name);
    });

    /* ---------------- 刷新设备 ---------------- */
    $('#refreshDevice').addEventListener('click', function () {
        toast('正在执行 adb devices ...');
    });

    /* ---------------- 包名列表：选中 / 右键菜单 ---------------- */
    const ctxMenu = $('#ctxMenu');
    const pkgList = $('#pkgList');

    pkgList.addEventListener('click', function (e) {
        const item = e.target.closest('.pkg-item');
        if (!item) return;
        $$('.pkg-item').forEach((el) => el.classList.remove('selected'));
        item.classList.add('selected');
    });

    pkgList.addEventListener('contextmenu', function (e) {
        const item = e.target.closest('.pkg-item');
        if (!item) return;
        e.preventDefault();
        $$('.pkg-item').forEach((el) => el.classList.remove('selected'));
        item.classList.add('selected');

        const menuWidth = ctxMenu.offsetWidth;
        const menuHeight = ctxMenu.offsetHeight;
        const x = Math.min(e.clientX, window.innerWidth - menuWidth - 8);
        const y = Math.min(e.clientY, window.innerHeight - menuHeight - 8);
        ctxMenu.style.left = x + 'px';
        ctxMenu.style.top = y + 'px';
        ctxMenu.classList.add('show');
    });

    document.addEventListener('click', function (e) {
        if (!ctxMenu.contains(e.target)) ctxMenu.classList.remove('show');
    });

    ctxMenu.addEventListener('click', function (e) {
        const item = e.target.closest('.menu-item');
        if (!item) return;
        const pkg = $('.pkg-item.selected .pkg-name');
        const name = pkg ? pkg.textContent : '';
        ctxMenu.classList.remove('show');
        if (item.dataset.action === 'export') {
            startExport(name);
        } else {
            toast('卸载应用：' + name);
        }
    });

    /* ---------------- 搜索过滤 ---------------- */
    $('#pkgSearch').addEventListener('input', function (e) {
        const keyword = e.target.value.trim().toLowerCase();
        let visible = 0;
        $$('.pkg-item').forEach(function (item) {
            const hit = item.querySelector('.pkg-name').textContent.toLowerCase().indexOf(keyword) !== -1;
            item.style.display = hit ? '' : 'none';
            if (hit) visible++;
        });
        $('#pkgCount').textContent = visible;
    });

    /* ---------------- 排序 ---------------- */
    let ascending = false;
    $('#sortPkg').addEventListener('click', function () {
        const items = $$('.pkg-item').sort(function (a, b) {
            const x = a.querySelector('.pkg-name').textContent;
            const y = b.querySelector('.pkg-name').textContent;
            return ascending ? y.localeCompare(x) : x.localeCompare(y);
        });
        ascending = !ascending;
        items.forEach((item) => pkgList.appendChild(item));
        toast(ascending ? '已按降序排列' : '已按升序排列');
    });

    /* ---------------- 刷新列表 ---------------- */
    $('#refreshPkg').addEventListener('click', function () {
        toast('正在执行 adb shell pm list package -3 ...');
    });

    /* ---------------- 导出进度（演示动画） ---------------- */
    const exportPanel = $('#exportProgress');
    const exportFill = $('#exportFill');
    const exportValue = $('#exportValue');
    let exportTimer = null;

    function startExport(packageName) {
        let progress = 0;
        exportFill.style.width = '0%';
        exportValue.textContent = '0';
        exportPanel.classList.add('show');
        toast('开始导出安装包：' + packageName);

        clearInterval(exportTimer);
        exportTimer = setInterval(function () {
            progress = Math.min(progress + Math.ceil(Math.random() * 9), 100);
            exportFill.style.width = progress + '%';
            exportValue.textContent = progress;
            if (progress >= 100) {
                clearInterval(exportTimer);
                setTimeout(function () {
                    exportPanel.classList.remove('show');
                    toast('导出完成：' + packageName + '.apk');
                }, 700);
            }
        }, 260);
    }
})();
