/**
 * 截屏导出 · 流程重设计稿 —— 静态交互脚本
 * 仅用于页面演示：加载态、多选、排序、搜索、预览、删除、批量导出进度
 */
(function () {
    'use strict';

    const $ = (sel) => document.querySelector(sel);
    const $$ = (sel) => Array.prototype.slice.call(document.querySelectorAll(sel));

    /* ---------------- Toast ---------------- */
    let toastTimer = null;

    function toast(text, warn) {
        const el = $('#toast');
        el.textContent = text;
        el.classList.toggle('warn', !!warn);
        el.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove('show'), 2000);
    }

    /* ---------------- 数据（模拟 adb 扫描结果） ---------------- */
    const TODAY = '2026-09-26';

    const RAW = [
        ['20260926_142108.png', '2026-09-26 14:21:08', 2.14],
        ['20260926_110435.png', '2026-09-26 11:04:35', 1.87],
        ['20260926_093254.png', '2026-09-26 09:32:54', 3.02],
        ['20260925_203147.png', '2026-09-25 20:31:47', 2.66],
        ['20260925_175502.png', '2026-09-25 17:55:02', 1.53],
        ['20260925_160918.png', '2026-09-25 16:09:18', 2.91],
        ['20260925_104726.png', '2026-09-25 10:47:26', 1.24],
        ['Screenshot_2026-09-24-221340.png', '2026-09-24 22:13:40', 2.38],
        ['Screenshot_2026-09-24-190512.png', '2026-09-24 19:05:12', 3.41],
        ['Screenshot_2026-09-23-163905.png', '2026-09-23 16:39:05', 1.72],
        ['Screenshot_2026-09-22-114810.png', '2026-09-22 11:48:10', 2.05],
        ['Screenshot_2026-09-21-093322.png', '2026-09-21 09:33:22', 1.66]
    ];

    let seed = 0;

    function buildShots() {
        return RAW.map(function (r) {
            seed += 1;
            return {
                id: 'shot-' + seed,
                name: r[0],
                time: r[1],
                size: r[2],
                theme: (seed % 8) + 1
            };
        });
    }

    let shots = buildShots();
    const selected = new Set();
    let ascending = false;      // false = 时间倒序（最新在前）
    let status = 'loading';     // loading | ready
    let exporting = false;
    let lbIndex = 0;

    /* ---------------- 工具 ---------------- */
    function parseTime(s) {
        return new Date(s.time.replace(/-/g, '/')).getTime();
    }

    function shortTime(t) {
        // 2026-09-26 14:21:08 → 09-26 14:21
        return t.slice(5, 16);
    }

    function groupOf(t) {
        const day = t.slice(0, 10);
        if (day === TODAY) return '今天';
        if (day === '2026-09-25') return '昨天';
        return '更早';
    }

    function phoneHTML(shot) {
        return '<div class="phone" data-theme="' + shot.theme + '">' +
            '<div class="phone-bar"><span>' + shot.time.slice(11, 16) + '</span><span class="batt"></span></div>' +
            '<div class="phone-main">' +
            '<div class="ph-row w70"></div>' +
            '<div class="ph-row w45"></div>' +
            '<div class="ph-block short"></div>' +
            '<div class="ph-block"></div>' +
            '<div class="ph-block short"></div>' +
            '</div></div>';
    }

    function visibleShots() {
        const kw = $('#shotSearch').value.trim().toLowerCase();
        let list = shots.slice();
        if (kw) {
            list = list.filter(function (s) {
                return s.name.toLowerCase().indexOf(kw) !== -1
                    || s.time.indexOf(kw) !== -1
                    || shortTime(s.time).indexOf(kw) !== -1;
            });
        }
        list.sort(function (a, b) {
            return ascending ? parseTime(a) - parseTime(b) : parseTime(b) - parseTime(a);
        });
        return list;
    }

    /* ---------------- 渲染 ---------------- */
    function render() {
        const grid = $('#shotGrid');
        $('#totalCount').textContent = shots.length;

        if (status === 'loading') {
            let html = '<div class="shot-grid">';
            for (let i = 0; i < 10; i++) {
                html += '<div class="shot-card"><div class="sk-thumb"></div><div class="sk-line"></div></div>';
            }
            grid.innerHTML = html + '</div>';
            return;
        }

        const list = visibleShots();

        if (shots.length === 0) {
            grid.innerHTML =
                '<div class="empty">' +
                '<svg viewBox="0 0 24 24" width="52" height="52" aria-hidden="true">' +
                '<path fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" ' +
                'd="M3 8V5a2 2 0 0 1 2-2h3m10 0h3a2 2 0 0 1 2 2v3m0 8v3a2 2 0 0 1-2 2h-3M8 21H5a2 2 0 0 1-2-2v-3"/>' +
                '<circle cx="12" cy="12" r="3" fill="none" stroke="currentColor" stroke-width="1.6"/></svg>' +
                '<div class="empty-title">设备上还没有截屏</div>' +
                '<div class="empty-desc">点击右上角「截取屏幕」立即截一张，截屏会自动出现在这里。</div>' +
                '<button class="btn btn-primary btn-sm" id="emptyCapture">立即截屏</button>' +
                '</div>';
            const btn = $('#emptyCapture');
            if (btn) btn.addEventListener('click', capture);
            syncFoot();
            return;
        }

        if (list.length === 0) {
            grid.innerHTML =
                '<div class="empty">' +
                '<svg viewBox="0 0 24 24" width="48" height="48" aria-hidden="true">' +
                '<circle cx="11" cy="11" r="7" fill="none" stroke="currentColor" stroke-width="1.6"/>' +
                '<path stroke="currentColor" stroke-width="1.6" stroke-linecap="round" d="M16.5 16.5L21 21"/></svg>' +
                '<div class="empty-title">没有匹配的截屏</div>' +
                '<div class="empty-desc">试试更换关键词，例如「09-26」或「Screenshot」。</div>' +
                '</div>';
            syncFoot();
            return;
        }

        let html = '<div class="shot-grid">';
        let group = '';
        list.forEach(function (s) {
            const g = groupOf(s.time);
            const count = list.filter(function (x) {
                return groupOf(x.time) === g;
            }).length;
            if (g !== group) {
                group = g;
                html += '<div class="shot-group">' + g + '<span class="count">' + count + '</span><span class="line"></span></div>';
            }
            html += cardHTML(s);
        });
        grid.innerHTML = html + '</div>';
        syncFoot();
    }

    function cardHTML(s) {
        const on = selected.has(s.id);
        return '<figure class="shot-card' + (on ? ' selected' : '') + '" data-id="' + s.id + '">' +
            '<div class="shot-thumb">' +
            phoneHTML(s) +
            '<div class="shot-check">' +
            '<svg viewBox="0 0 24 24" width="12" height="12" aria-hidden="true">' +
            '<path fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" d="M5 12.5l4.5 4.5L19 7"/>' +
            '</svg></div>' +
            '<div class="shot-hover">' +
            '<button class="hover-btn" data-act="preview">预览</button>' +
            '<button class="hover-btn danger" data-act="delete">删除</button>' +
            '</div>' +
            '</div>' +
            '<figcaption class="shot-meta"><b>' + shortTime(s.time) + '</b><span>' + s.size.toFixed(2) + ' MB</span></figcaption>' +
            '</figure>';
    }

    function syncFoot() {
        const n = selected.size;
        $('#selectedTip').innerHTML = '已选 <b>' + n + '</b> 张';
        $('#btnExport').disabled = n === 0 || exporting;
        $('#btnExport').textContent = n > 0 ? '导出选中 (' + n + ')' : '导出选中';
        $('#btnDelete').disabled = n === 0 || exporting;

        const all = visibleShots();
        const allOn = all.length > 0 && all.every(function (s) {
            return selected.has(s.id);
        });
        $('#btnSelectAll').textContent = allOn ? '取消全选' : '全选';
    }

    function setStatus(next) {
        status = next;
        $$('.demo-btn').forEach(function (b) {
            b.classList.toggle('active', b.dataset.status === next);
        });
        render();
    }

    /* ---------------- 选择 ---------------- */
    $('#shotGrid').addEventListener('click', function (e) {
        if (exporting) return;
        const card = e.target.closest('.shot-card');
        if (!card) return;

        const actBtn = e.target.closest('.hover-btn');
        if (actBtn) {
            const id = card.dataset.id;
            if (actBtn.dataset.act === 'preview') {
                openLightbox(id);
            } else {
                askDelete([id]);
            }
            return;
        }

        const id = card.dataset.id;
        if (selected.has(id)) {
            selected.delete(id);
        } else {
            selected.add(id);
        }
        card.classList.toggle('selected', selected.has(id));
        syncFoot();
    });

    $('#shotGrid').addEventListener('dblclick', function (e) {
        const card = e.target.closest('.shot-card');
        if (card) openLightbox(card.dataset.id);
    });

    $('#btnSelectAll').addEventListener('click', function () {
        const list = visibleShots();
        const allOn = list.length > 0 && list.every(function (s) {
            return selected.has(s.id);
        });
        list.forEach(function (s) {
            if (allOn) {
                selected.delete(s.id);
            } else {
                selected.add(s.id);
            }
        });
        render();
    });

    $('#shotSearch').addEventListener('input', render);

    /* ---------------- 排序 / 刷新 / 截屏 ---------------- */
    $('#btnSort').addEventListener('click', function () {
        ascending = !ascending;
        $('#sortText').textContent = ascending ? '时间 ↑' : '时间 ↓';
        render();
        toast(ascending ? '已按时间升序排列' : '已按时间降序排列');
    });

    $('#btnRefresh').addEventListener('click', function () {
        setStatus('loading');
        toast('正在执行 adb shell ls …');
        setTimeout(function () {
            setStatus('ready');
        }, 1100);
    });

    function capture() {
        const now = new Date();
        const pad = (n) => (n < 10 ? '0' + n : '' + n);
        const name = now.getFullYear() + pad(now.getMonth() + 1) + pad(now.getDate()) + '_'
            + pad(now.getHours()) + pad(now.getMinutes()) + pad(now.getSeconds()) + '.png';
        const time = now.getFullYear() + '-' + pad(now.getMonth() + 1) + '-' + pad(now.getDate()) + ' '
            + pad(now.getHours()) + ':' + pad(now.getMinutes()) + ':' + pad(now.getSeconds());
        seed += 1;
        shots.unshift({
            id: 'shot-' + seed,
            name: name,
            time: time,
            size: 2.0 + Math.random(),
            theme: (seed % 8) + 1
        });
        selected.clear();
        render();
        toast('截屏已保存：' + name);
    }

    $('#btnCapture').addEventListener('click', capture);
    $('#quickShot').addEventListener('click', function () {
        openModal();
        capture();
    });

    /* ---------------- 删除 ---------------- */
    let deleteIds = [];

    function askDelete(ids) {
        deleteIds = ids;
        $('#confirmTitle').textContent = '删除截屏';
        $('#confirmText').textContent = '确定要从设备上删除这 ' + ids.length + ' 张截屏吗？此操作不可撤销。';
        $('#confirmMask').classList.add('show');
    }

    $('#btnDelete').addEventListener('click', function () {
        const ids = shots.filter(function (s) {
            return selected.has(s.id);
        }).map(function (s) {
            return s.id;
        });
        if (ids.length === 0) return;
        askDelete(ids);
    });

    $('#confirmCancel').addEventListener('click', function () {
        $('#confirmMask').classList.remove('show');
    });

    $('#confirmOk').addEventListener('click', function () {
        $('#confirmMask').classList.remove('show');
        shots = shots.filter(function (s) {
            return deleteIds.indexOf(s.id) === -1;
        });
        deleteIds.forEach(function (id) {
            selected.delete(id);
        });
        render();
        toast('已删除 ' + deleteIds.length + ' 张截屏');
    });

    /* ---------------- 导出 ---------------- */
    $('#btnExport').addEventListener('click', startExport);

    function startExport() {
        const list = shots.filter(function (s) {
            return selected.has(s.id);
        });
        if (list.length === 0) return;

        exporting = true;
        const total = list.length;
        let done = 0;
        let progress = 0;

        $('#exportProgress').classList.add('show');
        $('#exportTotal').textContent = total;
        $('#exportIndex').textContent = '0';
        $('#exportFill').style.width = '0%';
        $('#exportValue').textContent = '0';
        $('#btnOpenFolder').disabled = true;
        syncFoot();
        toast('开始导出 ' + total + ' 张截屏到 ' + $('#saveDir').value);

        const step = 100 / total;
        const timer = setInterval(function () {
            progress = Math.min(progress + step / 5, 100);
            done = Math.min(Math.ceil(progress / step), total);
            $('#exportFill').style.width = progress + '%';
            $('#exportValue').textContent = Math.round(progress);
            $('#exportIndex').textContent = done;

            if (progress >= 100) {
                clearInterval(timer);
                setTimeout(function () {
                    exporting = false;
                    $('#exportProgress').classList.remove('show');
                    $('#btnOpenFolder').disabled = false;
                    syncFoot();
                    toast('已导出 ' + total + ' 张截屏');
                }, 500);
            }
        }, 90);
    }

    $('#btnBrowse').addEventListener('click', function () {
        toast('演示：此处调用 FolderBrowserDialog 选择目录');
    });

    $('#btnOpenFolder').addEventListener('click', function () {
        toast('演示：explorer.exe /select,' + $('#saveDir').value);
    });

    /* ---------------- 灯箱 ---------------- */
    function openLightbox(id) {
        const list = visibleShots();
        lbIndex = list.findIndex(function (s) {
            return s.id === id;
        });
        if (lbIndex < 0) lbIndex = 0;
        paintLightbox(list);
        $('#lightbox').classList.add('show');
    }

    function paintLightbox(list) {
        if (!list.length) return;
        const s = list[lbIndex];
        $('#lbPhone').innerHTML = phoneHTML(s);
        $('#lbName').textContent = s.name;
        $('#lbMeta').textContent = s.time + ' · ' + s.size.toFixed(2) + ' MB · ' + (lbIndex + 1) + '/' + list.length;
    }

    function moveLightbox(delta) {
        const list = visibleShots();
        lbIndex = (lbIndex + delta + list.length) % list.length;
        paintLightbox(list);
    }

    $('#lbClose').addEventListener('click', closeLightbox);
    $('#lbPrev').addEventListener('click', function () {
        moveLightbox(-1);
    });
    $('#lbNext').addEventListener('click', function () {
        moveLightbox(1);
    });
    $('#lightbox').addEventListener('click', function (e) {
        if (e.target === $('#lightbox')) closeLightbox();
    });

    function closeLightbox() {
        $('#lightbox').classList.remove('show');
    }

    /* ---------------- 弹窗开关 ---------------- */
    function openModal() {
        $('#exportMask').classList.add('show');
    }

    $('#openExport').addEventListener('click', openModal);
    $('#closeModal').addEventListener('click', function () {
        $('#exportMask').classList.remove('show');
    });
    $('#exportMask').addEventListener('click', function (e) {
        if (e.target === $('#exportMask')) $('#exportMask').classList.remove('show');
    });

    /* ---------------- 键盘 ---------------- */
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        if ($('#lightbox').classList.contains('show')) {
            closeLightbox();
        } else if ($('#confirmMask').classList.contains('show')) {
            $('#confirmMask').classList.remove('show');
        } else if ($('#exportMask').classList.contains('show')) {
            $('#exportMask').classList.remove('show');
        }
    });

    /* ---------------- 演示状态切换器 ---------------- */
    $$('.demo-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const s = btn.dataset.status;
            if (s === 'loading') {
                openModal();
                setStatus('loading');
                return;
            }
            if (s === 'empty') {
                openModal();
                shots = [];
                selected.clear();
                setStatus('ready');
                return;
            }
            if (s === 'ready') {
                openModal();
                if (shots.length === 0) {
                    shots = buildShots();
                    selected.clear();
                }
                setStatus('ready');
                return;
            }
            if (s === 'exporting') {
                openModal();
                if (shots.length === 0) shots = buildShots();
                status = 'ready';
                selected.clear();
                shots.slice(0, 5).forEach(function (x) {
                    selected.add(x.id);
                });
                render();
                if (!exporting) startExport();
            }
        });
    });

    /* ---------------- 启动 ---------------- */
    openModal();
    setStatus('loading');
    setTimeout(function () {
        setStatus('ready');
    }, 1000);
})();
