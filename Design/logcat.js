/**
 * Logcat · Android 日志 —— 静态交互脚本
 * 仅用于页面演示：实时日志生成、级别过滤、搜索、暂停、清除、自动滚动
 */
(function () {
    'use strict';

    const $ = (sel) => document.querySelector(sel);
    const $$ = (sel) => Array.prototype.slice.call(document.querySelectorAll(sel));

    const logList = $('#logList');
    const logEmpty = $('#logEmpty');

    /* ---------------- 状态 ---------------- */
    let paused = false;
    let storm = false;
    let autoScroll = true;
    let keyword = '';
    let logCount = 0;

    // 默认 Debug 及以上（Verbose 关闭）
    const levelState = { V: false, D: true, I: true, W: true, E: true, F: true };

    const MAX_ROWS = 2000;

    /* ---------------- 数据模板 ---------------- */
    const APPS = [
        { pkg: 'com.tencent.mm', pid: 1234 },
        { pkg: 'com.ss.android.ugc.aweme', pid: 2345 },
        { pkg: 'tv.danmaku.bili', pid: 3456 },
        { pkg: 'com.taobao.taobao', pid: 4567 },
        { pkg: 'com.android.systemui', pid: 1567 },
        { pkg: 'com.google.android.gms', pid: 2678 }
    ];

    const TEMPLATES = {
        V: [
            { tag: 'System.out', msg: (a) => `verbose trace from ${a.pkg}` },
            { tag: 'BluetoothAdapter', msg: () => 'state change: DISCONNECTED' }
        ],
        D: [
            { tag: 'ActivityThread', msg: (a) => `handleBindApplication: ${a.pkg}` },
            { tag: 'InputDispatcher', msg: () => 'Focus entered window: ' + (Math.floor(Math.random() * 90000) + 10000) },
            { tag: 'WindowManager', msg: () => 'relayoutWindow: frame=Rect(0, 0 - 1080, 2400)' },
            { tag: 'ViewRootImpl', msg: () => 'setView: ViewRootImpl@' + Math.random().toString(16).slice(2, 10) },
            { tag: 'Sensors', msg: () => 'Sensor accelerometer activated' },
            { tag: 'AudioTrack', msg: () => 'createTrack_l: sampleRate 48000' }
        ],
        I: [
            { tag: 'zygote64', msg: () => 'Compiler allocated ' + (2 + Math.floor(Math.random() * 8)) + 'MB to compile ' + pick(APPS).pkg },
            { tag: 'Choreographer', msg: () => 'Skipped ' + (1 + Math.floor(Math.random() * 60)) + ' frames! The application may be doing too much work on its main thread.' },
            { tag: 'ActivityManager', msg: (a) => `Start proc ${a.pid}:${a.pkg}/u0a${Math.floor(Math.random() * 200)} for activity` },
            { tag: 'dalvikvm', msg: () => 'GC freed ' + (500 + Math.floor(Math.random() * 9000)) + 'KB, ' + Math.floor(Math.random() * 100) + '% free' },
            { tag: 'art', msg: () => 'Background sticky concurrent mark sweep GC freed ' + (1 + Math.floor(Math.random() * 20)) + 'MB' },
            { tag: 'ConnectivityService', msg: () => 'NetworkAgentInfo [WIFI () - 100] EVENT_NETWORK_INFO_CHANGED' }
        ],
        W: [
            { tag: 'System.err', msg: () => '  at ' + pick(APPS).pkg + '.plugin.' + pick(['Network', 'Ui', 'Data', 'Cache']) + 'Helper.invoke(SourceFile:' + (10 + Math.floor(Math.random() * 900)) + ')' },
            { tag: 'SQLiteLog', msg: () => '(1) no such column: ' + pick(['user_id', 'ext_flag', 'sync_state', 'last_time']) },
            { tag: 'PackageManager', msg: (a) => `Unknown permission android.permission.X in package ${a.pkg}` },
            { tag: 'Resources', msg: () => 'ResourceType: Failure getting entry for 0x' + Math.random().toString(16).slice(2, 10) },
            { tag: 'NotificationService', msg: () => 'Toast already killed. pkg=' + pick(APPS).pkg }
        ],
        E: [
            { tag: 'AndroidRuntime', msg: () => 'FATAL EXCEPTION: ' + pick(['main', 'AsyncTask #1', 'IntentService', 'pool-2-thread-1']) },
            { tag: 'libc', msg: () => 'Fatal signal ' + pick(['11', '6', '7']) + ' (SIGSEGV), code ' + pick(['1', '2']) + ', fault addr 0x' + Math.random().toString(16).slice(2, 10) },
            { tag: 'NetworkSecurityConfig', msg: () => 'No Network Security Config specified, using platform default' },
            { tag: 'SurfaceFlinger', msg: () => 'Failed to create buffer queue' },
            { tag: 'MediaPlayerNative', msg: () => 'error (' + pick(['-38', '-2147483648', '1']) + ', 0)' }
        ],
        F: [
            { tag: 'AndroidRuntime', msg: (a) => `Process: ${a.pkg}, PID: ${a.pid}` },
            { tag: 'DEBUG', msg: () => '*** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***' }
        ]
    };

    function pick(arr) {
        return arr[Math.floor(Math.random() * arr.length)];
    }

    /* ---------------- 时间 / 格式化 ---------------- */
    function nowTime() {
        const d = new Date();
        const pad = (n) => String(n).padStart(2, '0');
        const ms = String(d.getMilliseconds()).padStart(3, '0');
        return pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds()) + '.' + ms;
    }

    /* ---------------- 日志生成 ---------------- */
    function pickLevel() {
        const pool = [];
        const weights = storm
            ? { E: 55, F: 25, W: 12, I: 6, D: 2 }
            : { D: 30, I: 45, W: 15, E: 8, F: 2 };
        Object.keys(weights).forEach((lv) => {
            if (levelState[lv]) {
                for (let i = 0; i < weights[lv]; i++) pool.push(lv);
            }
        });
        if (pool.length === 0) return null;
        return pick(pool);
    }

    function generateLog() {
        const level = pickLevel();
        if (!level) return null;
        const app = pick(APPS);
        const tpl = pick(TEMPLATES[level]);
        return {
            level: level,
            time: nowTime(),
            pid: app.pid + '-' + (app.pid + Math.floor(Math.random() * 60)),
            tag: tpl.tag,
            msg: tpl.msg(app)
        };
    }

    /* ---------------- 渲染 ---------------- */
    function buildRow(log) {
        const row = document.createElement('div');
        row.className = 'log-row row-' + log.level.toLowerCase();
        row.dataset.level = log.level;
        row.dataset.search = (log.tag + ' ' + log.msg).toLowerCase();

        row.innerHTML =
            '<span class="log-time">' + log.time + '</span>' +
            '<span class="log-level lv-' + log.level.toLowerCase() + '">' + log.level + '</span>' +
            '<span class="log-pid">' + log.pid + '</span>' +
            '<span class="log-tag" title="' + log.tag + '">' + log.tag + '</span>' +
            '<span class="log-msg">' + escapeHtml(log.msg) + '</span>';
        return row;
    }

    function escapeHtml(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function appendLog(log) {
        const row = buildRow(log);
        if (!levelState[log.level] || !matchKeyword(row.dataset.search)) {
            row.style.display = 'none';
        }
        logList.appendChild(row);
        logCount++;

        // 限制 DOM 规模
        while (logList.childElementCount > MAX_ROWS) {
            logList.removeChild(logList.firstElementChild);
            logCount--;
        }

        updateStats();
        scrollToBottom();
    }

    function matchKeyword(haystack) {
        return haystack.indexOf(keyword) !== -1;
    }

    function scrollToBottom() {
        if (!autoScroll) return;
        logList.scrollTop = logList.scrollHeight;
    }

    /* ---------------- 过滤 ---------------- */
    function applyFilter() {
        $$('.log-row').forEach(function (row) {
            const on = levelState[row.dataset.level] && matchKeyword(row.dataset.search);
            row.style.display = on ? '' : 'none';
        });
        updateEmpty();
    }

    function updateEmpty() {
        const empty = logList.childElementCount === 0;
        logEmpty.hidden = !empty;
    }

    function updateStats() {
        const counts = { D: 0, I: 0, W: 0, E: 0, F: 0 };
        $$('.log-row').forEach(function (row) {
            const lv = row.dataset.level;
            if (counts[lv] !== undefined) counts[lv]++;
        });
        $('#logCount').textContent = logCount;
        $('#footStats').textContent =
            'D ' + counts.D + ' · I ' + counts.I + ' · W ' + counts.W + ' · E ' + counts.E + ' · F ' + counts.F;
    }

    function updateLevelText() {
        const list = ['D', 'I', 'W', 'E', 'F'].filter((lv) => levelState[lv]);
        $('#levelText').textContent = list.length === 5 ? 'Debug 及以上' : list.join(' · ');
    }

    /* ---------------- 主循环 ---------------- */
    let timer = null;

    function startTimer() {
        stopTimer();
        const interval = storm ? 260 : 640;
        timer = setInterval(function () {
            if (paused) return;
            const n = storm ? (1 + Math.floor(Math.random() * 3)) : 1;
            for (let i = 0; i < n; i++) {
                const log = generateLog();
                if (log) appendLog(log);
            }
        }, interval);
    }

    function stopTimer() {
        if (timer) {
            clearInterval(timer);
            timer = null;
        }
    }

    /* ---------------- 交互事件 ---------------- */
    // 级别 chips
    $('#levelChips').addEventListener('click', function (e) {
        const chip = e.target.closest('.level-chip');
        if (!chip) return;
        const lv = chip.dataset.level;
        levelState[lv] = !levelState[lv];
        chip.classList.toggle('active', levelState[lv]);
        applyFilter();
        updateLevelText();
    });

    // 搜索
    $('#logSearch').addEventListener('input', function (e) {
        keyword = e.target.value.trim().toLowerCase();
        applyFilter();
    });

    // 暂停 / 继续
    $('#btnPause').addEventListener('click', function () {
        paused = !paused;
        $('#pauseText').textContent = paused ? '继续' : '暂停';
        $('#footState').innerHTML = paused
            ? '<span class="dot"></span> 已暂停 · adb logcat'
            : '<span class="dot"></span> 正在监听 · adb logcat';
        document.querySelector('.log-foot').classList.toggle('paused', paused);
    });

    // 清除
    $('#btnClear').addEventListener('click', function () {
        logList.innerHTML = '';
        logCount = 0;
        updateStats();
        updateEmpty();
    });

    // 自动滚动
    $('#autoScroll').addEventListener('change', function (e) {
        autoScroll = e.target.checked;
        if (autoScroll) scrollToBottom();
    });

    // 关闭（演示）
    $('#closeModal').addEventListener('click', function () {
        $('#logcatMask').classList.remove('show');
    });

    /* ---------------- 演示状态切换 ---------------- */
    $$('.demo-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const s = btn.dataset.status;
            $$('.demo-btn').forEach((b) => b.classList.toggle('active', b === btn));
            $('#logcatMask').classList.add('show');

            if (s === 'streaming') {
                paused = false;
                storm = false;
            } else if (s === 'paused') {
                paused = true;
                storm = false;
            } else if (s === 'storm') {
                paused = false;
                storm = true;
            } else if (s === 'empty') {
                paused = true;
                storm = false;
                logList.innerHTML = '';
                logCount = 0;
                updateStats();
                updateEmpty();
            }
            startTimer();
        });
    });

    /* ---------------- 启动 ---------------- */
    // 预填一段历史日志
    (function seed() {
        for (let i = 0; i < 40; i++) {
            const log = generateLog();
            if (log) appendLog(log);
        }
    })();

    updateLevelText();
    updateEmpty();
    startTimer();
})();
