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

    /* ---------------- 文件导入导出（adb pull / push）弹窗 ---------------- */
    const ioModal = $('#ioModal');
    const ioTabs = $('#ioTabs');
    const paneExport = $('#paneExport');
    const paneImport = $('#paneImport');
    const ioPrimary = $('#ioPrimary');
    const ioHint = $('#ioHint');
    const ioProgress = $('#ioProgress');
    const ioProgressLabel = $('#ioProgressLabel');
    const ioProgressText = $('#ioProgressText');
    const ioFill = $('#ioFill');
    const dropZone = $('#dropZone');
    const dropFile = $('#dropFile');
    const remotePath = $('#remotePath');
    const localPath = $('#localPath');
    const deviceTarget = $('#deviceTarget');

    let ioMode = 'export';
    let ioTimer = null;
    let ioRunning = false;
    let pickedFile = '';

    function openIoModal() {
        ioModal.hidden = false;
    }

    function closeIoModal() {
        if (ioRunning) return; // 传输进行中不允许关闭
        ioModal.hidden = true;
    }

    $('#ioAction').addEventListener('click', openIoModal);
    $('#ioCancel').addEventListener('click', closeIoModal);
    ioModal.addEventListener('click', function (e) {
        if (e.target === ioModal) closeIoModal();
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !ioModal.hidden) closeIoModal();
    });

    /* 导出 / 导入 分段切换 */
    function switchIoTab(mode) {
        ioMode = mode;
        ioTabs.querySelectorAll('.seg-item').forEach(function (btn) {
            btn.classList.toggle('active', btn.dataset.tab === mode);
        });
        paneExport.hidden = mode !== 'export';
        paneImport.hidden = mode !== 'import';
        ioPrimary.textContent = mode === 'export' ? '开始导出（pull）' : '开始导入（push）';
        ioHint.textContent = mode === 'export'
            ? '路径可为文件或目录，传输速度取决于连接方式（USB 快于无线）'
            : '推送系统目录（/system 等）需要 root 权限，普通文件请推送到 /sdcard';
    }

    ioTabs.addEventListener('click', function (e) {
        const btn = e.target.closest('.seg-item');
        if (!btn || ioRunning) return;
        switchIoTab(btn.dataset.tab);
    });

    switchIoTab('export');

    /* 导出：浏览本地保存位置 */
    $('#browseLocal').addEventListener('click', function () {
        // 演示：真实实现中这里调用 FolderBrowserDialog
        toast('打开本地保存位置选择框（演示）');
    });

    /* 导入：选择本地文件 */
    function setPickedFile(name) {
        pickedFile = name;
        dropFile.hidden = false;
        dropFile.textContent = name + ' · 3.2 MB';
    }

    dropZone.addEventListener('click', function () {
        // 演示：真实实现中这里调用 OpenFileDialog
        setPickedFile(pickedFile || 'IMG_20261024_093012.jpg');
        toast('已选择文件：' + pickedFile);
    });
    dropZone.addEventListener('dragover', function (e) {
        e.preventDefault();
        dropZone.classList.add('dragover');
    });
    dropZone.addEventListener('dragleave', function () {
        dropZone.classList.remove('dragover');
    });
    dropZone.addEventListener('drop', function (e) {
        e.preventDefault();
        dropZone.classList.remove('dragover');
        const file = e.dataTransfer.files && e.dataTransfer.files[0];
        setPickedFile(file ? file.name : 'IMG_20261024_093012.jpg');
        toast('已选择文件：' + pickedFile);
    });

    /* 传输进度（演示动画） */
    function runIoDemo() {
        ioRunning = true;
        ioPrimary.disabled = true;
        $('#ioCancel').disabled = true;
        ioProgress.hidden = false;
        ioProgressLabel.textContent = ioMode === 'export' ? '正在导出（pull）…' : '正在导入（push）…';
        ioHint.textContent = '传输中，请保持设备连接并勿断开 USB 或无线网络';

        let progress = 0;
        clearInterval(ioTimer);
        ioTimer = setInterval(function () {
            progress = Math.min(progress + Math.ceil(Math.random() * 7), 100);
            ioFill.style.width = progress + '%';
            ioProgressText.textContent = progress + '%';
            if (progress >= 100) {
                clearInterval(ioTimer);
                setTimeout(function () {
                    ioRunning = false;
                    ioPrimary.disabled = false;
                    $('#ioCancel').disabled = false;
                    ioProgress.hidden = true;
                    ioFill.style.width = '0%';
                    ioProgressText.textContent = '0%';
                    toast(ioMode === 'export'
                        ? '导出完成：' + remotePath.value.trim() + ' → 电脑'
                        : '导入完成：' + pickedFile + ' → 设备');
                }, 600);
            }
        }, 300);
    }

    ioPrimary.addEventListener('click', function () {
        if (ioRunning) return;
        if (ioMode === 'export' && !remotePath.value.trim()) {
            toast('请先填写设备上的文件路径');
            return;
        }
        if (ioMode === 'import') {
            if (!pickedFile) {
                toast('请先选择要导入的文件');
                return;
            }
            if (!deviceTarget.value.trim()) {
                toast('请先填写设备目标路径');
                return;
            }
        }
        runIoDemo();
    });

    /* ---------------- 导出安装包进度（演示动画） ---------------- */
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
