/* ============================================================
   SFTP 客户端 · 重设计稿交互脚本（仅用于演示设计效果）
   定位：只做远程文件传输（SFTP）的图形化替代，不做交互式终端。
   说明：文件系统内容为前端模拟，不建立真实 SSH 连接。
   ============================================================ */
(function () {
    "use strict";

    var state = {
        conn: "connected",          // connected | connecting | offline
        auth: "password",           // password | key
        tab: "web-01",              // 当前会话标签
        xferPaused: false,
        xfers: [],                  // 传输队列
        upBytes: 41943040,          // 本次会话上传累计
        upFiles: 1,
        downBytes: 39111885,        // 本次会话下载累计
        downFiles: 3,
        startedAt: Date.now() - 11564000,
        sessions: [
            { id: "web-01", alias: "web-01", user: "root", host: "192.168.1.10", port: "22", remoteCwd: "/root/www" },
            { id: "db-02", alias: "db-02", user: "root", host: "192.168.1.21", port: "22", remoteCwd: "/root/logs" }
        ],
        bookmarks: [
            { pane: "local", path: "D:\\Work\\build", anno: "构建产物（本地上传源）" },
            { pane: "local", path: "D:\\Work\\docs", anno: "文档目录" },
            { pane: "remote", path: "/root/www", anno: "站点根目录" },
            { pane: "remote", path: "/root/logs", anno: "运行日志" },
            { pane: "remote", path: "/root/docker", anno: "容器配置" }
        ]
    };

    var el = {
        statePill: document.getElementById("statePill"),
        stateDot: document.getElementById("stateDot"),
        statePillText: document.getElementById("statePillText"),
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        host: document.getElementById("host"),
        hostWrap: document.getElementById("hostWrap"),
        port: document.getElementById("port"),
        portWrap: document.getElementById("portWrap"),
        user: document.getElementById("user"),
        userWrap: document.getElementById("userWrap"),
        authSeg: document.getElementById("authSeg"),
        keyWrap: document.getElementById("keyWrap"),
        keyfile: document.getElementById("keyfile"),
        btnConnect: document.getElementById("btnConnect"),
        btnConnectText: document.getElementById("btnConnectText"),
        connTimer: document.getElementById("connTimer"),
        statUp: document.getElementById("statUp"),
        statUpCount: document.getElementById("statUpCount"),
        statDown: document.getElementById("statDown"),
        statDownCount: document.getElementById("statDownCount"),
        statQueue: document.getElementById("statQueue"),
        bmCount: document.getElementById("bmCount"),
        bmList: document.getElementById("bmList"),
        bmEmpty: document.getElementById("bmEmpty"),
        btnAddBm: document.getElementById("btnAddBm"),
        tabbar: document.getElementById("tabbar"),
        btnNewTab: document.getElementById("btnNewTab"),
        syncDir: document.getElementById("syncDir"),
        btnXferPauseAll: document.getElementById("btnXferPauseAll"),
        btnXferClear: document.getElementById("btnXferClear"),
        localPane: document.getElementById("localPane"),
        remotePane: document.getElementById("remotePane"),
        localList: document.getElementById("localList"),
        remoteList: document.getElementById("remoteList"),
        localCrumbs: document.getElementById("localCrumbs"),
        remoteCrumbs: document.getElementById("remoteCrumbs"),
        localDropTarget: document.getElementById("localDropTarget"),
        remoteDropTarget: document.getElementById("remoteDropTarget"),
        btnUpload: document.getElementById("btnUpload"),
        btnDownload: document.getElementById("btnDownload"),
        xfer: document.getElementById("xfer"),
        xferCount: document.getElementById("xferCount"),
        xferSummary: document.getElementById("xferSummary"),
        xferList: document.getElementById("xferList"),
        xferEmpty: document.getElementById("xferEmpty"),
        btnXferCollapse: document.getElementById("btnXferCollapse"),
        statusLeft: document.getElementById("statusLeft"),
        statusRight: document.getElementById("statusRight"),
        toast: document.getElementById("toast"),
        authMask: document.getElementById("authMask"),
        authTitle: document.getElementById("authTitle"),
        authDesc: document.getElementById("authDesc"),
        authLabel: document.getElementById("authLabel"),
        authPassword: document.getElementById("authPassword"),
        authRemember: document.getElementById("authRemember"),
        btnTogglePwd: document.getElementById("btnTogglePwd"),
        btnAuthOk: document.getElementById("btnAuthOk"),
        btnAuthCancel: document.getElementById("btnAuthCancel"),
        btnAuthClose: document.getElementById("btnAuthClose")
    };

    /* ---------------- 工具函数 ---------------- */

    function pad(n) {
        return String(n).padStart(2, "0");
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function formatSize(bytes) {
        if (bytes < 1024) {
            return bytes + " B";
        }
        if (bytes < 1024 * 1024) {
            return (bytes / 1024).toFixed(1) + " KB";
        }
        if (bytes < 1024 * 1024 * 1024) {
            return (bytes / 1024 / 1024).toFixed(1) + " MB";
        }
        return (bytes / 1024 / 1024 / 1024).toFixed(2) + " GB";
    }

    function nowTime() {
        var d = new Date();
        return pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds()) +
            "." + String(d.getMilliseconds()).padStart(3, "0");
    }

    function durationText() {
        var sec = Math.floor((Date.now() - state.startedAt) / 1000);
        return pad(Math.floor(sec / 3600)) + ":" + pad(Math.floor(sec % 3600 / 60)) + ":" + pad(sec % 60);
    }

    var toastTimer = null;

    function toast(text, warn) {
        el.toast.textContent = text;
        el.toast.className = "toast show" + (warn ? " warn" : "");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            el.toast.className = "toast";
        }, 1800);
    }

    function setStatus(text, type) {
        el.statusLeft.textContent = text;
        el.statusLeft.className = type || "";
    }

    function showHint(text) {
        el.toolbarHintText.textContent = text || "";
        el.toolbarHint.classList.toggle("show", !!text);
    }

    var ICON_X = '<svg viewBox="0 0 24 24" width="11" height="11" aria-hidden="true">' +
        '<path fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" d="M6 6l12 12M18 6L6 18"/></svg>';

    var ICON_GO = '<svg viewBox="0 0 24 24" width="12" height="12" aria-hidden="true">' +
        '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" ' +
        'd="M5 12h13m0 0l-5-5m5 5l-5 5"/></svg>';

    var ICON_FOLDER = '<svg viewBox="0 0 24 24" width="13" height="13" aria-hidden="true">' +
        '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" ' +
        'd="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>';

    var ICON_FILE = '<svg viewBox="0 0 24 24" width="13" height="13" aria-hidden="true">' +
        '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" ' +
        'd="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/>' +
        '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" d="M14 3v5h5"/></svg>';

    var ICON_UP = '<svg viewBox="0 0 24 24" width="13" height="13" aria-hidden="true">' +
        '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" ' +
        'd="M12 19V5m0 0l-6 6m6-6l6 6"/></svg>';

    /* ---------------- 会话 ---------------------------- */

    function activeSession() {
        for (var i = 0; i < state.sessions.length; i++) {
            if (state.sessions[i].id === state.tab) {
                return state.sessions[i];
            }
        }
        return state.sessions[0];
    }

    function renderTabs() {
        el.tabbar.querySelectorAll(".session-tab").forEach(function (t) {
            t.classList.toggle("active", t.dataset.tab === state.tab);
            var dot = t.querySelector(".dot-mini");
            if (dot) {
                dot.classList.toggle("on", state.conn === "connected");
            }
        });
    }

    function switchTab(id) {
        var cur = activeSession();
        if (cur) {
            cur.remoteCwd = PANES.remote.cwd;
        }
        state.tab = id;
        var next = activeSession();
        PANES.remote.cwd = next.remoteCwd || "/root";
        renderFiles("remote");
        renderTabs();
        setStatus("已切换到会话 " + next.user + "@" + next.alias);
    }

    /* ---------------- 本次会话统计 ---------------- */

    function renderSession() {
        var running = state.xfers.filter(function (x) {
            return x.status === "running";
        }).length;
        var paused = state.xfers.filter(function (x) {
            return x.status === "paused";
        }).length;
        var done = state.xfers.filter(function (x) {
            return x.status === "done";
        }).length;

        el.connTimer.textContent = state.conn === "connected" ? durationText() : "—";
        el.statUp.textContent = formatSize(state.upBytes);
        el.statUpCount.textContent = state.upFiles + " 个文件";
        el.statDown.textContent = formatSize(state.downBytes);
        el.statDownCount.textContent = state.downFiles + " 个文件";

        if (state.xfers.length === 0) {
            el.statQueue.textContent = "空闲";
        } else if (running + paused > 0) {
            el.statQueue.textContent = (running + paused) + " 个进行中";
        } else {
            el.statQueue.textContent = done + " 个已完成";
        }
    }

    /* ---------------- 常用目录 ---------------- */

    function renderBookmarks() {
        var list = state.bookmarks;
        el.bmCount.textContent = list.length;
        el.bmList.style.display = list.length === 0 ? "none" : "flex";
        el.bmEmpty.hidden = list.length !== 0;

        el.bmList.innerHTML = list.map(function (b, i) {
            return '<div class="bm-item" data-index="' + i + '">' +
                '<span class="bm-tag ' + b.pane + '">' + (b.pane === "local" ? "本地" : "远端") + '</span>' +
                '<span class="bm-text">' +
                '<span class="bm-path">' + escapeHtml(b.path) + '</span>' +
                '<span class="bm-anno">' + escapeHtml(b.anno) + '</span>' +
                '</span>' +
                '<span class="bm-actions">' +
                '<button class="mini-btn go" data-act="go" title="跳转到该目录">' + ICON_GO + '</button>' +
                '<button class="mini-btn del" data-act="del" title="移除">' + ICON_X + '</button>' +
                '</span>' +
                '</div>';
        }).join("");
    }

    function gotoBookmark(index) {
        var b = state.bookmarks[index];
        if (!b) {
            return;
        }
        PANES[b.pane].cwd = b.path;
        PANES[b.pane].selected = {};
        renderFiles(b.pane);
        toast("已跳转到 " + b.path);
        setStatus("已跳转到" + (b.pane === "local" ? "本地" : "远端") + "目录 " + b.path);
    }

    /* ================= 文件系统（演示数据） ================= */

    var PANES = {
        local: { cwd: "D:\\Work", root: "D:\\Work", entries: [], selected: {} },
        remote: { cwd: "/root/www", root: "/root", entries: [], selected: {} }
    };

    var FS = {
        local: {
            "D:\\Work": [
                { name: "app", dir: true },
                { name: "docs", dir: true },
                { name: "build", dir: true },
                { name: "release-notes.md", size: 12480, time: "10-14 09:22" },
                { name: "deploy-key.pem", size: 1675, time: "10-12 18:04" }
            ],
            "D:\\Work\\app": [
                { name: "src", dir: true },
                { name: "build", dir: true },
                { name: "app-release.apk", size: 40055603, time: "10-14 10:41" },
                { name: "gradle.properties", size: 1024, time: "10-11 20:33" }
            ],
            "D:\\Work\\build": [
                { name: "mapping.txt", size: 1183924, time: "10-14 10:40" },
                { name: "app-release.apk", size: 40055603, time: "10-14 10:41" },
                { name: "output-metadata.json", size: 812, time: "10-14 10:41" }
            ],
            "D:\\Work\\docs": [
                { name: "api.md", size: 20480, time: "10-13 15:10" },
                { name: "changelog.md", size: 9216, time: "10-14 08:55" }
            ]
        },
        remote: {
            "/root": [
                { name: "www", dir: true, mode: "drwxr-xr-x", time: "10-14 09:11" },
                { name: "logs", dir: true, mode: "drwxr-xr-x", time: "10-14 12:01" },
                { name: "docker", dir: true, mode: "drwxr-xr-x", time: "10-10 17:42" },
                { name: ".bashrc", size: 3174, mode: "-rw-r--r--", time: "09-02 11:20" },
                { name: "deploy.sh", size: 2457, mode: "-rwxr-xr-x", time: "10-14 09:10" }
            ],
            "/root/www": [
                { name: "assets", dir: true, mode: "drwxr-xr-x", time: "10-14 09:11" },
                { name: "index.html", size: 5120, mode: "-rw-r--r--", time: "10-14 09:11" },
                { name: "app-release.apk", size: 39062500, mode: "-rw-r--r--", time: "10-13 21:07" }
            ],
            "/root/logs": [
                { name: "app.log", size: 5242880, mode: "-rw-r--r--", time: "10-14 12:01" },
                { name: "nginx.log", size: 1258291, mode: "-rw-r--r--", time: "10-14 11:58" }
            ],
            "/root/docker": [
                { name: "docker-compose.yml", size: 1832, mode: "-rw-r--r--", time: "10-10 17:42" },
                { name: "nginx.conf", size: 964, mode: "-rw-r--r--", time: "10-10 17:44" }
            ]
        }
    };

    var PANE_EL = {
        local: { box: el.localPane, list: el.localList, crumbs: el.localCrumbs },
        remote: { box: el.remotePane, list: el.remoteList, crumbs: el.remoteCrumbs }
    };

    function joinPath(key, cwd, name) {
        if (key === "local") {
            return cwd + "\\" + name;
        }
        return (cwd === "/" ? "" : cwd) + "/" + name;
    }

    function parentPath(key, cwd) {
        if (key === "local") {
            var i = cwd.lastIndexOf("\\");
            return i <= 0 ? cwd : cwd.slice(0, i);
        }
        if (cwd === "/") {
            return "/";
        }
        var j = cwd.lastIndexOf("/");
        return j <= 0 ? "/" : cwd.slice(0, j);
    }

    function getEntries(key, cwd) {
        var map = FS[key];
        if (!map[cwd]) {
            map[cwd] = key === "remote"
                ? [
                    { name: "data", dir: true, mode: "drwxr-xr-x", time: "10-13 17:22" },
                    { name: "readme.txt", size: 1280, mode: "-rw-r--r--", time: "10-14 09:00" }
                ]
                : [
                    { name: "tmp", dir: true, time: "10-13 17:22" },
                    { name: "notes.txt", size: 2048, time: "10-14 09:00" }
                ];
        }
        return map[cwd];
    }

    function updateDropTargets() {
        el.localDropTarget.textContent = PANES.remote.cwd;
        el.remoteDropTarget.textContent = PANES.local.cwd;
    }

    function renderCrumbs(key) {
        var p = PANES[key];
        var sep = key === "local" ? "\\" : "/";
        var parts = p.cwd.split(/[\\/]/).filter(function (s) {
            return s.length > 0;
        });
        var acc = "";
        var html = parts.map(function (seg, i) {
            acc = i === 0 ? (key === "local" ? seg : "/" + seg) : acc + sep + seg;
            return '<button data-crumb="' + escapeHtml(acc) + '">' + escapeHtml(seg) + '</button>';
        }).join('<span class="crumb-sep">' + sep + '</span>');
        PANE_EL[key].crumbs.innerHTML = html || '<button data-crumb="/">/</button>';
    }

    function renderFiles(key) {
        var p = PANES[key];
        var entries = getEntries(key, p.cwd);
        var isRemote = key === "remote";
        p.entries = entries;

        function rowHtml(item, isUp) {
            var cls = "file-row" + (item.dir ? " dir" : "") + (isUp ? " up" : "");
            if (p.selected[item.name]) {
                cls += " selected";
            }
            var icon = isUp ? ICON_UP : (item.dir ? ICON_FOLDER : ICON_FILE);
            var modeCell = escapeHtml(item.mode || (item.dir ? "drwxr-xr-x" : "-rw-r--r--"));
            return '<div class="' + cls + '" data-name="' + escapeHtml(item.name) + '"' +
                (item.dir ? ' data-dir="1"' : "") + (isUp ? ' data-up="1"' : "") + '>' +
                '<span class="file-icon' + (item.dir ? " folder" : "") + '">' + icon + '</span>' +
                '<span class="file-name">' + escapeHtml(item.name) + '</span>' +
                (isRemote ? '<span class="file-mode">' + modeCell + '</span>' : '') +
                '<span class="file-size">' + (item.dir ? "—" : formatSize(item.size || 0)) + '</span>' +
                '<span class="file-time">' + escapeHtml(item.time || "—") + '</span>' +
                '</div>';
        }

        var html = '<div class="file-row head"><span></span><span>名称</span>' +
            (isRemote ? '<span class="file-mode">权限</span>' : '') +
            '<span class="file-size">大小</span><span class="file-time">修改时间</span></div>';

        if (p.cwd !== p.root) {
            html += rowHtml({ name: "..", dir: true }, true);
        }
        html += entries.map(function (e) {
            return rowHtml(e, false);
        }).join("");

        PANE_EL[key].list.innerHTML = html;
        renderCrumbs(key);
        updateDropTargets();
    }

    function renderPanes() {
        renderFiles("local");
        renderFiles("remote");
    }

    function goUp(key) {
        var p = PANES[key];
        var parent = parentPath(key, p.cwd);
        if (parent === p.cwd) {
            toast("已经是根目录", true);
            return;
        }
        p.cwd = parent;
        p.selected = {};
        renderFiles(key);
    }

    /* ================= 传输队列 ================= */

    var ACCEL = 2.4;            /* 进度加速系数（演示用，让任务几秒内跑完） */
    var XFER_TICK = 120;        /* 队列刷新间隔（ms） */
    var xferSeq = 0;
    var xferTimer = null;

    function pushXfer(dir, name, size) {
        var localPath = PANES.local.cwd + "\\" + name;
        var remotePath = (PANES.remote.cwd === "/" ? "" : PANES.remote.cwd) + "/" + name;
        state.xfers.push({
            id: ++xferSeq,
            dir: dir,
            name: name,
            from: dir === "up" ? localPath : remotePath,
            to: dir === "up" ? remotePath : localPath,
            size: size,
            loaded: 0,
            speed: (8 + Math.random() * 10) * 1024 * 1024,
            status: "running"
        });
    }

    function queueTransfer(dir, sourceKey) {
        if (state.conn !== "connected") {
            toast("请先连接 SSH 会话", true);
            return;
        }
        var p = PANES[sourceKey];
        var picked = p.entries.filter(function (e) {
            return !!p.selected[e.name];
        });
        if (picked.length === 0) {
            toast("请先选中要" + (dir === "up" ? "上传" : "下载") + "的文件", true);
            return;
        }
        picked.forEach(function (e) {
            pushXfer(dir, e.name, e.size || Math.round((0.6 + Math.random() * 2.4) * 1024 * 1024));
        });
        p.selected = {};
        renderFiles(sourceKey);
        renderXfers();
        startXferTimer();
        toast("已加入 " + picked.length + " 个" + (dir === "up" ? "上传" : "下载") + "任务");
        setStatus("已加入 " + picked.length + " 个" + (dir === "up" ? "上传" : "下载") + "任务", "ok");
        el.statusRight.textContent = "最后传输 " + nowTime() + " · 队列自动刷新";
    }

    function xferItemHtml(x) {
        var pct = x.size ? Math.min(100, x.loaded / x.size * 100) : 0;
        var meta;
        if (x.status === "done") {
            meta = '<span class="xfer-meta ok">已完成 · ' + formatSize(x.size) + '</span>';
        } else if (x.status === "paused") {
            meta = '<span class="xfer-meta">已暂停 · ' + formatSize(x.loaded) + ' / ' + formatSize(x.size) + '</span>';
        } else {
            var left = Math.max(1, Math.ceil((x.size - x.loaded) / (x.speed * ACCEL)));
            meta = '<span class="xfer-meta">' + formatSize(x.loaded) + ' / ' + formatSize(x.size) +
                ' · ' + formatSize(x.speed) + '/s · 剩余 ' + left + 's</span>';
        }
        return '<div class="xfer-item ' + x.dir + ' ' + x.status + '" data-id="' + x.id + '">' +
            '<span class="xfer-arrow ' + x.dir + '">' + (x.dir === "up" ? "&#8593;" : "&#8595;") + '</span>' +
            '<span class="xfer-file">' +
            '<span class="xfer-name">' + escapeHtml(x.name) + '</span>' +
            '<span class="xfer-target">' + escapeHtml(x.from) + ' → ' + escapeHtml(x.to) + '</span>' +
            '</span>' +
            '<span class="xfer-bar"><i style="width:' + pct.toFixed(1) + '%"></i></span>' +
            meta +
            '<button class="mini-btn del" data-act="cancel" title="取消任务">' + ICON_X + '</button>' +
            '</div>';
    }

    function renderXfers() {
        var running = state.xfers.filter(function (x) {
            return x.status === "running";
        });
        var paused = state.xfers.filter(function (x) {
            return x.status === "paused";
        });

        el.xferCount.textContent = state.xfers.length;

        if (state.xfers.length === 0) {
            el.xferSummary.textContent = "空闲";
        } else if (running.length) {
            var speed = running.reduce(function (s, x) {
                return s + x.speed;
            }, 0);
            el.xferSummary.textContent = running.length + " 个传输中 · " + formatSize(speed) + "/s" +
                (paused.length ? " · " + paused.length + " 个已暂停" : "");
        } else if (paused.length) {
            el.xferSummary.textContent = paused.length + " 个已暂停";
        } else {
            el.xferSummary.textContent = "全部完成 · 共 " + state.xfers.length + " 个任务";
        }

        el.xferList.style.display = state.xfers.length === 0 ? "none" : "flex";
        el.xferEmpty.hidden = state.xfers.length !== 0;
        el.xferList.innerHTML = state.xfers.map(xferItemHtml).join("");
        renderSession();
    }

    function startXferTimer() {
        if (xferTimer) {
            return;
        }
        xferTimer = setInterval(function () {
            var alive = 0;
            state.xfers.forEach(function (x) {
                if (x.status !== "running") {
                    return;
                }
                alive++;
                x.loaded = Math.min(x.size, x.loaded + x.speed * ACCEL * XFER_TICK / 1000);
                if (x.loaded >= x.size) {
                    x.status = "done";
                    if (x.dir === "up") {
                        state.upBytes += x.size;
                        state.upFiles++;
                    } else {
                        state.downBytes += x.size;
                        state.downFiles++;
                    }
                    el.statusRight.textContent = "最后传输 " + nowTime() + " · 队列自动刷新";
                }
            });
            renderXfers();
            if (alive === 0) {
                stopXferTimer();
            }
        }, XFER_TICK);
    }

    function stopXferTimer() {
        if (xferTimer) {
            clearInterval(xferTimer);
            xferTimer = null;
        }
    }

    /* ================= 连接与认证 ================= */

    function address() {
        return el.user.value.trim() + "@" + el.host.value.trim() + ":" + el.port.value.trim();
    }

    function renderConnection() {
        var connected = state.conn === "connected";
        el.btnUpload.disabled = !connected;
        el.btnDownload.disabled = !connected;
        el.btnXferPauseAll.disabled = !connected;
        el.connTimer.textContent = connected ? durationText() : "—";
        renderTabs();
    }

    function setConn(status, keepStart) {
        state.conn = status;
        var pillClass = "state-pill";
        var dotClass = "dot";

        if (status === "connected") {
            pillClass += " on";
            dotClass += " on";
            el.statePillText.textContent = "已连接";
            el.btnConnectText.textContent = "断开连接";
            if (!keepStart) {
                state.startedAt = Date.now();
            }
            setStatus("已连接 " + address(), "ok");
        } else if (status === "connecting") {
            pillClass += " pending";
            dotClass += " pending";
            el.statePillText.textContent = "认证中";
            el.btnConnectText.textContent = "取消连接";
            setStatus("正在认证 " + address() + " …");
        } else {
            el.statePillText.textContent = "未连接";
            el.btnConnectText.textContent = "连接";
            setStatus("未连接");
        }

        el.statePill.className = pillClass;
        el.stateDot.className = dotClass;
        renderConnection();
    }

    function openAuthDialog() {
        var isKey = state.auth === "key";
        el.authTitle.textContent = isKey ? "SSH 密钥认证" : "SSH 密码认证";
        el.authLabel.textContent = isKey ? "私钥口令（Passphrase）" : "登录密码";
        el.authPassword.value = isKey ? "" : "admin@2026";
        el.authPassword.placeholder = isKey ? "私钥未加密时可留空" : "请输入密码";
        el.authDesc.textContent = isKey
            ? "正在连接 " + address() + "，将使用私钥 " + (el.keyfile.value || "（未选择文件）") +
              " 完成公钥认证；若私钥已加密，请输入对应口令。"
            : "正在连接 " + address() + "，请输入登录密码完成认证。密码仅用于本次会话，不会明文保存到磁盘。";
        el.authMask.classList.add("show");
        el.authPassword.focus();
    }

    function closeAuthDialog() {
        el.authMask.classList.remove("show");
    }

    /* ================= 事件绑定 ================= */

    el.btnConnect.addEventListener("click", function () {
        if (state.conn === "connected") {
            setConn("offline");
            setStatus("已断开连接");
            return;
        }
        if (state.conn === "connecting") {
            setConn("offline");
            setStatus("已取消连接");
            return;
        }

        var hostOk = el.host.value.trim().length > 0;
        var portOk = /^\d+$/.test(el.port.value.trim()) && Number(el.port.value) >= 1 && Number(el.port.value) <= 65535;
        var userOk = el.user.value.trim().length > 0;
        var keyOk = state.auth !== "key" || el.keyfile.value.trim().length > 0;

        el.hostWrap.classList.toggle("invalid", !hostOk);
        el.portWrap.classList.toggle("invalid", !portOk);
        el.userWrap.classList.toggle("invalid", !userOk);
        el.keyWrap.classList.toggle("invalid", !keyOk);

        showHint(!hostOk ? "请填写主机地址，例如 192.168.1.10 或 example.com"
            : !portOk ? "端口号需为 1 ~ 65535 的整数"
                : !userOk ? "请填写登录用户名"
                    : !keyOk ? "私钥认证需要选择私钥文件" : "");

        if (!hostOk || !portOk || !userOk || !keyOk) {
            setStatus("连接参数不完整，请检查工具条输入", "err");
            return;
        }

        openAuthDialog();
    });

    [el.host, el.port, el.user].forEach(function (input) {
        input.addEventListener("input", function () {
            el.hostWrap.classList.remove("invalid");
            el.portWrap.classList.remove("invalid");
            el.userWrap.classList.remove("invalid");
            showHint("");
        });
    });

    el.authSeg.addEventListener("click", function (e) {
        var btn = e.target.closest("button");
        if (!btn) {
            return;
        }
        el.authSeg.querySelectorAll("button").forEach(function (b) {
            b.classList.toggle("active", b === btn);
        });
        state.auth = btn.dataset.auth;
        el.keyWrap.hidden = state.auth !== "key";
        setStatus(state.auth === "key" ? "认证方式：公钥（私钥文件）" : "认证方式：密码");
    });

    el.btnAuthOk.addEventListener("click", function () {
        closeAuthDialog();
        setConn("connecting");
        setTimeout(function () {
            if (state.conn !== "connecting") {
                return;
            }
            var s = activeSession();
            s.user = el.user.value.trim();
            s.host = el.host.value.trim();
            setConn("connected");
            renderSession();
            toast("SSH 会话已建立，可以开始传文件");
        }, 1200);
    });

    el.btnAuthCancel.addEventListener("click", closeAuthDialog);
    el.btnAuthClose.addEventListener("click", closeAuthDialog);

    el.authMask.addEventListener("click", function (e) {
        if (e.target === el.authMask) {
            closeAuthDialog();
        }
    });

    el.btnTogglePwd.addEventListener("click", function () {
        el.authPassword.type = el.authPassword.type === "password" ? "text" : "password";
    });

    /* 会话标签 */
    el.tabbar.addEventListener("click", function (e) {
        var closeBtn = e.target.closest(".tab-close");
        var tab = e.target.closest(".session-tab");
        if (!tab) {
            return;
        }

        if (closeBtn) {
            e.stopPropagation();
            var tabs = el.tabbar.querySelectorAll(".session-tab");
            if (tabs.length <= 1) {
                toast("至少保留一个会话标签", true);
                return;
            }
            var closingId = tab.dataset.tab;
            var wasActive = closingId === state.tab;
            tab.remove();
            state.sessions = state.sessions.filter(function (s) {
                return s.id !== closingId;
            });
            if (wasActive) {
                switchTab(el.tabbar.querySelector(".session-tab").dataset.tab);
            }
            toast("已关闭会话 " + closingId);
            return;
        }

        switchTab(tab.dataset.tab);
    });

    el.btnNewTab.addEventListener("click", function () {
        toast("新建 SSH 会话（弹出连接配置对话框）");
    });

    /* 常用目录 */
    el.bmList.addEventListener("click", function (e) {
        var item = e.target.closest(".bm-item");
        if (!item) {
            return;
        }
        var index = Number(item.dataset.index);
        var mini = e.target.closest(".mini-btn");

        if (mini && mini.dataset.act === "del") {
            var removed = state.bookmarks[index];
            state.bookmarks.splice(index, 1);
            renderBookmarks();
            toast("已移除常用目录 " + removed.path);
            return;
        }

        gotoBookmark(index);
    });

    el.btnAddBm.addEventListener("click", function () {
        toast("新增常用目录（弹出路径选择对话框）");
    });

    el.syncDir.addEventListener("change", function () {
        toast(el.syncDir.checked ? "目录同步已开启" : "目录同步已关闭");
    });

    /* ---------- 文件面板交互 ---------- */

    function bindPane(key) {
        var p = PANES[key];
        var box = PANE_EL[key].box;

        box.addEventListener("click", function (e) {
            var tool = e.target.closest(".icon-btn");
            if (tool) {
                if (tool.dataset.act === "up") {
                    goUp(key);
                } else if (tool.dataset.act === "refresh") {
                    renderFiles(key);
                    toast("已刷新 " + p.cwd);
                } else {
                    toast("新建文件夹（弹出命名对话框）");
                }
                return;
            }

            var crumb = e.target.closest("[data-crumb]");
            if (crumb) {
                p.cwd = crumb.dataset.crumb;
                p.selected = {};
                renderFiles(key);
                return;
            }

            var row = e.target.closest(".file-row");
            if (!row || row.classList.contains("head")) {
                return;
            }
            if (row.dataset.up) {
                goUp(key);
                return;
            }

            var name = row.dataset.name;
            var multi = e.ctrlKey || e.metaKey;
            if (!multi) {
                p.selected = {};
                p.selected[name] = true;
            } else if (p.selected[name]) {
                delete p.selected[name];
            } else {
                p.selected[name] = true;
            }
            renderFiles(key);
            setStatus("已选中 " + Object.keys(p.selected).length + " 项 · " + p.cwd);
        });

        box.addEventListener("dblclick", function (e) {
            var row = e.target.closest(".file-row");
            if (!row || row.classList.contains("head") || row.dataset.up) {
                return;
            }
            var name = row.dataset.name;
            if (row.dataset.dir) {
                p.cwd = joinPath(key, p.cwd, name);
                p.selected = {};
                renderFiles(key);
                toast("已进入 " + p.cwd);
                return;
            }
            /* 双击文件 = 直接传输（本地→上传，远端→下载） */
            p.selected = {};
            p.selected[name] = true;
            renderFiles(key);
            queueTransfer(key === "local" ? "up" : "down", key);
        });

        /* 从资源管理器拖拽文件到面板 */
        box.addEventListener("dragover", function (e) {
            e.preventDefault();
            box.classList.add("drop-active");
        });

        box.addEventListener("dragleave", function (e) {
            if (!box.contains(e.relatedTarget)) {
                box.classList.remove("drop-active");
            }
        });

        box.addEventListener("drop", function (e) {
            e.preventDefault();
            box.classList.remove("drop-active");
            if (state.conn !== "connected") {
                toast("请先连接 SSH 会话", true);
                return;
            }
            var dir = key === "local" ? "up" : "down";
            var files = e.dataTransfer ? e.dataTransfer.files : null;
            if (!files || files.length === 0) {
                toast("未读取到文件，请从资源管理器拖入", true);
                return;
            }
            for (var i = 0; i < files.length; i++) {
                pushXfer(dir, files[i].name, files[i].size || 1024 * 1024);
            }
            renderXfers();
            startXferTimer();
            toast("已加入 " + files.length + " 个" + (dir === "up" ? "上传" : "下载") + "任务");
        });
    }

    bindPane("local");
    bindPane("remote");

    /* 中间箭头：上传 / 下载 */
    el.btnUpload.addEventListener("click", function () {
        queueTransfer("up", "local");
    });

    el.btnDownload.addEventListener("click", function () {
        queueTransfer("down", "remote");
    });

    /* 队列操作 */
    el.xferList.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-act='cancel']");
        if (!btn) {
            return;
        }
        var item = btn.closest(".xfer-item");
        var id = Number(item.dataset.id);
        state.xfers = state.xfers.filter(function (x) {
            return x.id !== id;
        });
        renderXfers();
        toast("已取消传输任务");
    });

    el.btnXferPauseAll.addEventListener("click", function () {
        state.xferPaused = !state.xferPaused;
        state.xfers.forEach(function (x) {
            if (state.xferPaused && x.status === "running") {
                x.status = "paused";
            } else if (!state.xferPaused && x.status === "paused") {
                x.status = "running";
            }
        });
        el.btnXferPauseAll.textContent = state.xferPaused ? "全部继续" : "全部暂停";
        renderXfers();
        if (!state.xferPaused) {
            startXferTimer();
        }
    });

    el.btnXferClear.addEventListener("click", function () {
        state.xfers = [];
        state.xferPaused = false;
        el.btnXferPauseAll.textContent = "全部暂停";
        stopXferTimer();
        renderXfers();
        toast("传输队列已清空");
    });

    el.btnXferCollapse.addEventListener("click", function () {
        var collapsed = el.xfer.classList.toggle("collapsed");
        el.btnXferCollapse.textContent = collapsed ? "展开" : "收起";
    });

    /* 阻止浏览器直接打开被拖入的文件 */
    ["dragover", "drop"].forEach(function (evt) {
        window.addEventListener(evt, function (e) {
            e.preventDefault();
        });
    });

    /* ---------- 演示状态切换 ---------- */
    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".demo-btn").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });

            var target = btn.dataset.status;

            if (target === "empty") {
                state.xfers = [];
                state.xferPaused = false;
                el.btnXferPauseAll.textContent = "全部暂停";
                stopXferTimer();
                setConn("connected", true);
                renderXfers();
                renderSession();
                setStatus("演示状态：空队列");
                return;
            }

            if (target === "connected") {
                state.startedAt = Date.now() - 11564000;
                setConn("connected", true);
                renderSession();
                setStatus("演示状态：已连接 " + address(), "ok");
            } else if (target === "connecting") {
                setConn("connecting");
            } else {
                setConn("offline");
                setStatus("演示状态：未连接");
            }
        });
    });

    /* ---------------- 初始化 ---------------- */

    setInterval(function () {
        if (state.conn === "connected") {
            el.connTimer.textContent = durationText();
        }
    }, 1000);

    renderBookmarks();
    renderPanes();
    renderXfers();
    renderSession();
    setConn("connected", true);
    setStatus("就绪 · 已连接到 " + address() + " · 支持拖拽上传，不再需要 scp");
})();
