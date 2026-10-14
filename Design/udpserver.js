/* ============================================================
   UDP 服务端 · 重设计稿交互脚本（仅用于演示设计效果）

   与 TCP 服务端脚本的差异：
   1. 客户端没有 online 布尔量，改成 lastSeen 时间戳 + 空闲超时计算「活跃/静默」；
   2. 没有断开操作，只有「移除来源」与「清空列表」；
   3. 演示里会周期性地模拟收到数据报（含偶发的新来源），
      让「活跃时间 / 新发现 / 未读」这些状态能真实变化。
   ============================================================ */
(function () {
    "use strict";

    var state = {
        listen: "listening",            // listening | binding | stopped
        startedAt: Date.now() - 3731000,
        view: "hex",                    // 消息显示编码：hex | ascii
        enc: "hex",                     // 发送编码：hex | ascii
        broadcast: false,               // 广播全部活跃客户端
        loopTimer: null,
        rxTimer: null,
        selectedId: "c1",
        failCount: 0,                   // 发送失败（无响应 / 端口不可达）
        clients: buildSeedClients()
    };

    var CANDIDATE_POOL = [
        { ip: "192.168.3.88", port: 53012 },
        { ip: "192.168.3.72", port: 51044 },
        { ip: "192.168.3.15", port: 49880 },
        { ip: "10.0.6.77", port: 50120 }
    ];

    var el = {
        statePill: document.getElementById("statePill"),
        stateDot: document.getElementById("stateDot"),
        statePillText: document.getElementById("statePillText"),
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        host: document.getElementById("host"),
        port: document.getElementById("port"),
        portWrap: document.getElementById("portWrap"),
        idleTimeout: document.getElementById("idleTimeout"),
        btnListen: document.getElementById("btnListen"),
        btnListenText: document.getElementById("btnListenText"),
        runTimer: document.getElementById("runTimer"),
        statActive: document.getElementById("statActive"),
        statTotal: document.getElementById("statTotal"),
        statUnread: document.getElementById("statUnread"),
        statTxCount: document.getElementById("statTxCount"),
        statTxBytes: document.getElementById("statTxBytes"),
        statRxCount: document.getElementById("statRxCount"),
        statRxBytes: document.getElementById("statRxBytes"),
        statNew: document.getElementById("statNew"),
        statFail: document.getElementById("statFail"),
        clientList: document.getElementById("clientList"),
        clientEmpty: document.getElementById("clientEmpty"),
        clientCount: document.getElementById("clientCount"),
        btnClearClients: document.getElementById("btnClearClients"),
        peerTag: document.getElementById("peerTag"),
        msgScroll: document.getElementById("msgScroll"),
        msgEmpty: document.getElementById("msgEmpty"),
        msgEmptyTitle: document.getElementById("msgEmptyTitle"),
        msgEmptyDesc: document.getElementById("msgEmptyDesc"),
        msgCount: document.getElementById("msgCount"),
        viewSeg: document.getElementById("viewSeg"),
        sendSeg: document.getElementById("sendSeg"),
        input: document.getElementById("input"),
        btnSend: document.getElementById("btnSend"),
        btnSave: document.getElementById("btnSave"),
        btnClear: document.getElementById("btnClear"),
        loopSwitch: document.getElementById("loopSwitch"),
        interval: document.getElementById("interval"),
        bcastSwitch: document.getElementById("bcastSwitch"),
        bcastHint: document.getElementById("bcastHint"),
        bcastHintText: document.getElementById("bcastHintText"),
        statusLeft: document.getElementById("statusLeft"),
        statusRight: document.getElementById("statusRight"),
        toast: document.getElementById("toast")
    };

    /* ---------------- 演示数据 ---------------- */

    function buildSeedClients() {
        var now = Date.now();
        return [
            client("c1", "192.168.3.11", 52344, now - 1200, 0, [
                msg("rx", "AA 55 01 01", "12:03:41.120"),
                msg("tx", "AA 55 81 01 00", "12:03:41.198"),
                msg("rx", "AA 55 02 00", "12:03:42.005"),
                msg("tx", "AA 55 82 00 06 31 2E 30 2E 33", "12:03:42.114"),
                msg("rx", "AA 55 03 01 FF", "12:03:43.260"),
                msg("tx", "48 65 6C 6C 6F 20 44 65 76 4B 69 74", "12:03:43.660")
            ]),
            client("c2", "192.168.3.27", 51880, now - 4200, 5, [
                msg("rx", "AA 55 01 02 00", "12:03:39.004"),
                msg("rx", "AA 55 01 02 01", "12:03:40.512")
            ]),
            client("c3", "192.168.3.45", 51002, now - 26000, 2, [
                msg("rx", "AA 55 04 02 0A 00", "12:03:37.880")
            ]),
            /* 静默来源：超过空闲超时没再收到数据报，仍然保留在列表里 */
            client("c4", "192.168.3.61", 49777, now - 155000, 0, [
                msg("tx", "AA 55 02 00", "11:58:02.113"),
                msg("rx", "AA 55 82 00 06 31 2E 30 2E 33", "11:58:02.221")
            ])
        ];
    }

    function client(id, ip, port, lastSeen, unread, logs) {
        return { id: id, ip: ip, port: port, lastSeen: lastSeen, unread: unread, logs: logs, isNew: false };
    }

    function msg(dir, hex, time) {
        return { dir: dir, hex: hex, time: time };
    }

    /* ---------------- 工具函数 ---------------- */

    function nowTime() {
        var d = new Date();
        return pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds()) +
            "." + String(d.getMilliseconds()).padStart(3, "0");
    }

    function pad(n) {
        return String(n).padStart(2, "0");
    }

    function hexToBytes(hex) {
        return hex.trim().split(/\s+/).filter(Boolean).map(function (b) {
            return parseInt(b, 16) & 0xFF;
        });
    }

    function bytesToHex(bytes) {
        return bytes.map(function (b) {
            return pad(b.toString(16).toUpperCase());
        }).join(" ");
    }

    function textToHex(text) {
        var bytes = [];
        for (var i = 0; i < text.length; i++) {
            var code = text.charCodeAt(i);
            if (code < 0x80) {
                bytes.push(code);
            } else {
                var encoded = unescape(encodeURIComponent(text.charAt(i)));
                for (var j = 0; j < encoded.length; j++) {
                    bytes.push(encoded.charCodeAt(j) & 0xFF);
                }
            }
        }
        return bytes;
    }

    function bytesToText(bytes) {
        return bytes.map(function (b) {
            return b >= 32 && b < 127 ? String.fromCharCode(b) : ".";
        }).join("");
    }

    function isHexString(str) {
        return /^([0-9a-fA-F]{2})(\s+[0-9a-fA-F]{2})*$/.test(str.trim());
    }

    function isPort(str) {
        return /^\d+$/.test(str) && Number(str) >= 1 && Number(str) <= 65535;
    }

    function isIdleTimeout(str) {
        return /^\d+$/.test(str) && Number(str) >= 5 && Number(str) <= 3600;
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    var toastTimer = null;

    function toast(text, type) {
        el.toast.textContent = text;
        el.toast.className = "toast show" + (type ? " " + type : "");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            el.toast.className = "toast";
        }, 1800);
    }

    /* ---------------- 查询 ---------------- */

    function endpoint() {
        return el.host.value + ":" + el.port.value;
    }

    function idleLimit() {
        return (Number(el.idleTimeout.value) || 60) * 1000;
    }

    /* UDP 无连接：以「最近收到数据报的时间」判断来源是否活跃 */
    function isActive(c) {
        return Date.now() - c.lastSeen < idleLimit();
    }

    function selectedClient() {
        return state.clients.filter(function (c) {
            return c.id === state.selectedId;
        })[0] || null;
    }

    function activeClients() {
        return state.clients.filter(isActive);
    }

    function lastSeenText(c) {
        var sec = Math.max(0, Math.floor((Date.now() - c.lastSeen) / 1000));
        if (sec < 60) {
            return sec + " 秒前";
        }
        if (sec < 3600) {
            return Math.floor(sec / 60) + " 分钟前";
        }
        return Math.floor(sec / 3600) + " 小时前";
    }

    /* ---------------- 渲染 ---------------- */

    function renderClients() {
        el.clientCount.textContent = state.clients.length;
        el.clientList.style.display = state.clients.length === 0 ? "none" : "flex";
        el.clientEmpty.classList.toggle("show", state.clients.length === 0);

        el.clientList.innerHTML = state.clients.map(function (c) {
            var active = isActive(c);
            var cls = "client-item" + (active ? "" : " quiet") + (c.id === state.selectedId ? " selected" : "");

            var unread = c.unread > 0
                ? '<span class="unread">' + (c.unread > 9 ? "9+" : c.unread) + '</span>'
                : "";

            var newTag = c.isNew ? '<span class="new-tag">新</span>' : "";

            /* hover 才出现的迷你操作：复制地址 / 移除来源 */
            var mini = '' +
                '<button class="client-mini copy" data-act="copy" title="复制地址">' +
                '<svg viewBox="0 0 24 24" width="12" height="12" aria-hidden="true">' +
                '<rect x="9" y="9" width="12" height="12" rx="2" fill="none" stroke="currentColor" stroke-width="2"/>' +
                '<path fill="none" stroke="currentColor" stroke-width="2" d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>' +
                '</svg></button>' +
                '<button class="client-mini remove" data-act="remove" title="移除来源（含消息记录）">' +
                '<svg viewBox="0 0 24 24" width="12" height="12" aria-hidden="true">' +
                '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" ' +
                'stroke-linejoin="round" d="M18 6L6 18M6 6l12 12"/></svg></button>';

            var stateText = (active ? "活跃 · " : "静默 · ") + lastSeenText(c) + "收到数据报";

            return '' +
                '<div class="' + cls + '" data-id="' + c.id + '">' +
                '<span class="dot ' + (active ? "on" : "off") + '"></span>' +
                '<span class="client-text">' +
                '<span class="client-addr-row">' +
                '<span class="client-addr">' + escapeHtml(c.ip + ":" + c.port) + '</span>' +
                newTag +
                '</span>' +
                '<span class="client-state">' + stateText + '</span>' +
                '</span>' +
                '<span class="client-side">' + unread + mini + '</span>' +
                '</div>';
        }).join("");

        renderStats();
    }

    function renderMessages() {
        var peer = selectedClient();

        if (peer) {
            el.peerTag.className = "peer";
            el.peerTag.innerHTML = '<span>来源 ' + escapeHtml(peer.ip + ":" + peer.port) + '</span>';
        } else {
            el.peerTag.className = "peer none";
            el.peerTag.innerHTML = "<span>未选中客户端</span>";
        }

        var list = peer ? peer.logs : [];

        el.msgCount.textContent = peer ? peer.logs.length : 0;
        el.msgScroll.style.display = list.length === 0 ? "none" : "flex";
        el.msgEmpty.classList.toggle("show", list.length === 0);

        if (!peer) {
            el.msgEmptyTitle.textContent = "尚未选择客户端";
            el.msgEmptyDesc.textContent = "在左侧「已发现客户端」中点击一个来源，即可查看它的收发记录；悬浮单条消息可复制。";
        } else if (state.listen === "stopped") {
            el.msgEmptyTitle.textContent = "监听已停止";
            el.msgEmptyDesc.textContent = "停止监听不会清空已发现来源与历史消息；重新启动监听后可继续收发。";
        } else if (list.length === 0) {
            el.msgEmptyTitle.textContent = "还没有消息记录";
            el.msgEmptyDesc.textContent = "该来源还没有任何收发记录，可在下方发送一条数据报试试。";
        }

        el.msgScroll.innerHTML = list.map(function (m) {
            var bytes = hexToBytes(m.hex);
            var body = state.view === "hex" ? bytesToHex(bytes) : bytesToText(bytes);
            var isTx = m.dir === "tx";
            return '' +
                '<div class="msg ' + m.dir + '">' +
                '<div class="msg-head">' +
                '<span class="tag tag-' + m.dir + '">' + (isTx ? "发送" : "接收") + '</span>' +
                '<span class="msg-meta">' + escapeHtml(m.time) + '</span>' +
                '<span class="msg-meta">' + bytes.length + ' B</span>' +
                '<button class="msg-copy" data-copy="' + escapeHtml(m.hex) + '" title="复制 HEX 内容">' +
                '<svg viewBox="0 0 24 24" width="11" height="11" aria-hidden="true">' +
                '<rect x="9" y="9" width="12" height="12" rx="2" fill="none" stroke="currentColor" stroke-width="2"/>' +
                '<path fill="none" stroke="currentColor" stroke-width="2" d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>' +
                '</svg>复制</button>' +
                '</div>' +
                '<div class="msg-body">' + escapeHtml(body) + '</div>' +
                '</div>';
        }).join("");

        var lastRx = peer ? peer.logs.filter(function (m) {
            return m.dir === "rx";
        }).pop() : null;
        el.statusRight.textContent = lastRx
            ? "最后接收 " + lastRx.time + " · 消息列表自动滚动"
            : "暂无接收数据 · 消息列表自动滚动";
    }

    function renderStats() {
        var tx = [];
        var rx = [];
        state.clients.forEach(function (c) {
            c.logs.forEach(function (m) {
                (m.dir === "tx" ? tx : rx).push(m);
            });
        });

        var txBytes = tx.reduce(function (sum, m) {
            return sum + hexToBytes(m.hex).length;
        }, 0);
        var rxBytes = rx.reduce(function (sum, m) {
            return sum + hexToBytes(m.hex).length;
        }, 0);

        var unread = state.clients.reduce(function (sum, c) {
            return sum + (c.id === state.selectedId ? 0 : c.unread);
        }, 0);

        el.statActive.textContent = activeClients().length;
        el.statTotal.textContent = "共 " + state.clients.length + " 个";
        el.statUnread.textContent = unread;
        el.statTxCount.textContent = tx.length;
        el.statTxBytes.textContent = txBytes + " B";
        el.statRxCount.textContent = rx.length;
        el.statRxBytes.textContent = rxBytes + " B";
        el.statNew.textContent = state.clients.filter(function (c) {
            return c.isNew;
        }).length;
        el.statFail.textContent = state.failCount;
    }

    function renderListen() {
        var pillClass = "state-pill";
        var dotClass = "dot";
        var pillText = "未监听";
        var btnText = "启动监听";
        var btnClass = "btn btn-primary";

        if (state.listen === "listening") {
            pillClass += " on";
            dotClass += " on";
            pillText = "监听中";
            btnText = "停止监听";
            btnClass = "btn btn-stop";
        } else if (state.listen === "binding") {
            pillClass += " pending";
            dotClass += " pending";
            pillText = "绑定中";
            btnText = "取消绑定";
        }

        el.statePill.className = pillClass;
        el.stateDot.className = dotClass;
        el.statePillText.textContent = pillText;
        el.btnListenText.textContent = btnText;
        el.btnListen.className = btnClass;

        /* 监听未开启时，地址 / 端口 / 空闲超时只读 */
        var idle = state.listen !== "listening";
        el.host.disabled = idle;
        el.port.disabled = idle;
        el.idleTimeout.disabled = idle;

        el.btnClearClients.disabled = state.clients.length === 0;

        updateControls();
        updateTimer();
    }

    /* 发送区可用性：监听中 + （已选中来源 或 广播且存在活跃来源） */
    function canSend() {
        if (state.listen !== "listening") {
            return false;
        }
        if (state.broadcast) {
            return activeClients().length > 0;
        }
        return !!selectedClient();
    }

    function updateControls() {
        var enabled = canSend();
        el.input.disabled = !enabled;
        el.btnSend.disabled = !enabled;
        el.loopSwitch.disabled = !enabled;
        el.bcastSwitch.disabled = state.listen !== "listening";
    }

    function scrollToBottom() {
        el.msgScroll.scrollTop = el.msgScroll.scrollHeight;
    }

    /* ---------------- 监听状态 ---------------- */

    function durationText() {
        var sec = Math.floor((Date.now() - state.startedAt) / 1000);
        return pad(Math.floor(sec / 3600)) + ":" + pad(Math.floor(sec % 3600 / 60)) + ":" + pad(sec % 60);
    }

    function updateTimer() {
        el.runTimer.textContent = state.listen === "listening" ? durationText() : "—";
        /* 每秒刷新一次，让「活跃 → 静默」的迁移在界面上可见 */
        if (state.clients.length > 0) {
            renderClients();
        }
    }

    function showHint(text) {
        el.toolbarHintText.textContent = text || "";
        el.toolbarHint.classList.toggle("show", !!text);
    }

    function setStatus(text, type) {
        el.statusLeft.innerHTML = text;
        el.statusLeft.className = type || "";
    }

    function setListen(status) {
        state.listen = status;

        if (status === "listening") {
            state.startedAt = Date.now();
            setStatus('监听已启动 · <b>' + endpoint() + '</b>', "ok");
        } else if (status === "binding") {
            setStatus("正在绑定 " + endpoint() + " …");
        } else {
            stopLoop();
            el.loopSwitch.checked = false;
            el.interval.disabled = true;
            /* UDP 停止监听不会清空已发现来源，只是不再更新活跃状态 */
            state.clients.forEach(function (c) {
                c.lastSeen = Date.now() - idleLimit() - 1000;
            });
            setStatus("监听已停止，已发现来源列表保留但不再更新");
        }

        renderClients();
        renderMessages();
        renderListen();
    }

    /* ---------------- 发送 ---------------- */

    function currentPayload() {
        var raw = el.input.value;
        if (!raw.trim()) {
            return { error: "不能发送空消息" };
        }

        if (state.enc === "hex") {
            if (!isHexString(raw)) {
                return { error: "HEX 格式错误，应为空格分隔的两位十六进制，如 AA 55 01 01" };
            }
            return { hex: bytesToHex(hexToBytes(raw)) };
        }

        return { hex: bytesToHex(textToHex(raw)) };
    }

    function send(text) {
        if (state.listen !== "listening") {
            setStatus("未启动监听，无法发送数据报", "err");
            return;
        }

        var payload;
        if (text === undefined) {
            payload = currentPayload();
        } else if (isHexString(text)) {
            payload = { hex: bytesToHex(hexToBytes(text)) };
        } else {
            payload = { hex: bytesToHex(textToHex(text)) };
        }

        if (payload.error) {
            setStatus(payload.error, "err");
            return;
        }

        var targets;
        if (state.broadcast) {
            targets = activeClients();
            if (targets.length === 0) {
                setStatus("当前没有活跃客户端，无法广播", "err");
                return;
            }
        } else {
            var peer = selectedClient();
            if (!peer) {
                setStatus("未选中客户端，无法发送数据报", "err");
                return;
            }
            targets = [peer];
        }

        var sentBytes = hexToBytes(payload.hex).length;
        var record = msg("tx", payload.hex, nowTime());

        targets.forEach(function (c) {
            c.logs.push(record);
        });

        renderClients();
        renderMessages();
        scrollToBottom();

        if (targets.length > 1) {
            setStatus("已广播 " + sentBytes + " B 到 " + targets.length + " 个活跃客户端", "ok");
            toast("已广播到 " + targets.length + " 个客户端", "info");
            return;
        }

        var only = targets[0];
        setStatus("已发送 " + sentBytes + " B 到 " + only.ip + ":" + only.port, "ok");

        /* 演示：活跃来源会回应答；静默来源不响应，计入发送失败 */
        if (!isActive(only)) {
            setTimeout(function () {
                state.failCount += 1;
                renderStats();
                setStatus("已发送 " + sentBytes + " B 到 " + only.ip + ":" + only.port + " · 对方无响应（可能端口不可达）", "err");
            }, 600);
            return;
        }

        setTimeout(function () {
            var reply = bytesToHex(hexToBytes(payload.hex).map(function (b, i) {
                return i === 1 ? (b | 0x80) : b;
            }));
            receiveFrom(only, reply);
        }, 280);
    }

    /* 收到一条数据报：更新活跃时间，必要时累加未读 */
    function receiveFrom(c, hex) {
        c.lastSeen = Date.now();
        c.logs.push(msg("rx", hex, nowTime()));
        if (c.id !== state.selectedId) {
            c.unread += 1;
            renderClients();
        } else {
            renderClients();
            renderMessages();
            scrollToBottom();
        }
    }

    function stopLoop() {
        if (state.loopTimer) {
            clearInterval(state.loopTimer);
            state.loopTimer = null;
        }
    }

    /* ---------------- 事件绑定 ---------------- */

    el.btnListen.addEventListener("click", function () {
        if (state.listen === "listening") {
            setListen("stopped");
            return;
        }
        if (state.listen === "binding") {
            setListen("stopped");
            setStatus("已取消绑定");
            return;
        }

        var portOk = isPort(el.port.value.trim());
        var idleOk = isIdleTimeout(el.idleTimeout.value.trim());

        el.portWrap.classList.toggle("invalid", !portOk);
        el.idleTimeout.classList.toggle("invalid", !idleOk);

        if (!portOk) {
            showHint("监听端口需为 1 ~ 65535 的整数");
            setStatus("监听端口需为 1 ~ 65535 的整数", "err");
            return;
        }
        if (!idleOk) {
            showHint("空闲超时需为 5 ~ 3600 秒");
            setStatus("空闲超时需为 5 ~ 3600 秒", "err");
            return;
        }
        showHint("");

        setListen("binding");
        setTimeout(function () {
            if (state.listen === "binding") {
                setListen("listening");
            }
        }, 900);
    });

    el.port.addEventListener("input", function () {
        el.portWrap.classList.remove("invalid");
        showHint("");
    });

    el.idleTimeout.addEventListener("input", function () {
        el.idleTimeout.classList.remove("invalid");
        showHint("");
        renderClients();
    });

    el.btnSend.addEventListener("click", function () {
        send();
    });

    el.input.addEventListener("keydown", function (e) {
        if (e.ctrlKey && e.key === "Enter") {
            e.preventDefault();
            send();
        }
    });

    el.btnClear.addEventListener("click", function () {
        var peer = selectedClient();
        if (!peer) {
            setStatus("未选中客户端，无法清空", "err");
            return;
        }
        peer.logs = [];
        renderMessages();
        renderStats();
        setStatus("已清空 " + peer.ip + ":" + peer.port + " 的消息记录");
    });

    el.btnSave.addEventListener("click", function () {
        var peer = selectedClient();
        if (!peer || peer.logs.length === 0) {
            setStatus("没有需要保存的日志", "err");
            return;
        }
        setStatus("已保存 " + peer.logs.length + " 条消息到 udp-server-log-20261014.txt", "ok");
    });

    el.clientList.addEventListener("click", function (e) {
        var item = e.target.closest(".client-item");
        if (!item) {
            return;
        }

        var target = state.clients.filter(function (c) {
            return c.id === item.dataset.id;
        })[0];
        if (!target) {
            return;
        }

        var act = e.target.closest(".client-mini");

        if (act && act.dataset.act === "copy") {
            copyText(target.ip + ":" + target.port);
            return;
        }

        if (act && act.dataset.act === "remove") {
            state.clients = state.clients.filter(function (c) {
                return c.id !== target.id;
            });
            if (state.selectedId === target.id) {
                state.selectedId = state.clients.length > 0 ? state.clients[0].id : null;
            }
            renderClients();
            renderMessages();
            renderListen();
            toast("已移除来源 " + target.ip + ":" + target.port, "warn");
            return;
        }

        state.selectedId = target.id;
        target.unread = 0;
        target.isNew = false;
        state.broadcast = false;
        el.bcastSwitch.checked = false;
        updateBcastHint();
        renderClients();
        renderMessages();
        updateControls();
        scrollToBottom();
    });

    el.btnClearClients.addEventListener("click", function () {
        if (state.clients.length === 0) {
            setStatus("列表已是空的", "err");
            return;
        }
        var count = state.clients.length;
        state.clients = [];
        state.selectedId = null;
        state.failCount = 0;
        renderClients();
        renderMessages();
        renderListen();
        toast("已清空 " + count + " 个已发现来源", "warn");
    });

    el.loopSwitch.addEventListener("change", function () {
        el.interval.disabled = !el.loopSwitch.checked;
        if (el.loopSwitch.checked) {
            if (!/^\d+$/.test(el.interval.value.trim())) {
                setStatus("时间间隔仅支持正整数", "err");
                el.loopSwitch.checked = false;
                el.interval.disabled = true;
                return;
            }
            state.loopTimer = setInterval(function () {
                send();
            }, Number(el.interval.value));
            setStatus("循环发送已开启，间隔 " + el.interval.value + " ms", "ok");
        } else {
            stopLoop();
            setStatus("循环发送已停止");
        }
    });

    el.bcastSwitch.addEventListener("change", function () {
        state.broadcast = el.bcastSwitch.checked;
        updateBcastHint();
        updateControls();
        setStatus(state.broadcast
            ? "广播模式已开启：将发送给全部活跃客户端"
            : "广播模式已关闭：仅发送给所选客户端");
    });

    function updateBcastHint() {
        var count = activeClients().length;
        el.bcastHint.classList.toggle("show", state.broadcast);
        if (state.broadcast) {
            el.bcastHintText.textContent = count > 0
                ? "广播模式：本次内容将发送给全部 " + count + " 个活跃客户端"
                : "广播模式：当前没有活跃客户端，发送按钮不可用";
        }
    }

    /* 分段控件：显示编码 / 发送编码 */
    function bindSegmented(container, key, callback) {
        container.addEventListener("click", function (e) {
            var btn = e.target.closest("button");
            if (!btn || !btn.dataset[key]) {
                return;
            }
            container.querySelectorAll("button").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });
            state[key] = btn.dataset[key];
            callback();
        });
    }

    bindSegmented(el.viewSeg, "view", renderMessages);
    bindSegmented(el.sendSeg, "enc", function () {
        el.input.placeholder = state.enc === "hex"
            ? "输入要发送的内容；HEX 模式下使用空格分隔，例如 AA 55 01 01"
            : "输入要发送的文本内容，将按 UTF-8 编码发送";
    });

    el.msgScroll.addEventListener("click", function (e) {
        var btn = e.target.closest(".msg-copy");
        if (!btn) {
            return;
        }
        copyText(btn.dataset.copy);
    });

    function copyText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).catch(function () {
            });
        }
        toast("已复制 " + text, "info");
    }

    /* ---------------- 演示：模拟持续收到数据报 ---------------- */

    function simulateReceive() {
        if (state.listen !== "listening" || state.clients.length === 0) {
            return;
        }

        /* 偶发出现新来源，用来演示「新发现 / 自动加入列表」 */
        if (state.clients.length < 6 && Math.random() < 0.28) {
            var pool = CANDIDATE_POOL.filter(function (p) {
                return !state.clients.some(function (c) {
                    return c.ip === p.ip && c.port === p.port;
                });
            });

            if (pool.length > 0) {
                var pick = pool[Math.floor(Math.random() * pool.length)];
                var created = client("n" + Date.now(), pick.ip, pick.port, Date.now(), 1, [
                    msg("rx", "AA 55 01 " + bytesToHex([Math.floor(Math.random() * 256)]), nowTime())
                ]);
                created.isNew = true;
                state.clients.push(created);
                renderClients();
                toast("发现新来源 " + created.ip + ":" + created.port, "info");
                return;
            }
        }

        var target = state.clients[Math.floor(Math.random() * state.clients.length)];
        var body = [0xAA, 0x55, 0x10, Math.floor(Math.random() * 256), Math.floor(Math.random() * 256)];
        receiveFrom(target, bytesToHex(body));

        if (!selectedClient()) {
            renderMessages();
        }
    }

    /* ---------------- 演示状态切换 ----------------
       也支持地址栏直接进入某个状态：
       udpserver.html#listening / #binding / #stopped / #empty / #noselect */
    function applyDemoStatus(target) {
        document.querySelectorAll(".demo-btn").forEach(function (b) {
            b.classList.toggle("active", b.dataset.status === target);
        });

        stopLoop();
        el.loopSwitch.checked = false;
        el.interval.disabled = true;

        if (target === "empty") {
            state.clients = [];
            state.selectedId = null;
            state.failCount = 0;
            setListen("listening");
            setStatus('监听已启动 · <b>' + endpoint() + '</b> · 等待第一个数据报');
            return;
        }

        if (state.clients.length === 0) {
            state.clients = buildSeedClients();
            state.selectedId = "c1";
            state.failCount = 0;
        }

        if (target === "noselect") {
            state.startedAt = Date.now() - 3731000;
            state.listen = "listening";
            state.clients.forEach(function (c) {
                c.lastSeen = Date.now() - 1500;
            });
            state.selectedId = null;
            renderClients();
            renderMessages();
            renderListen();
            setStatus("就绪 · 点击左侧来源查看其消息记录");
            return;
        }

        if (target === "listening") {
            state.startedAt = Date.now() - 3731000;
            state.listen = "listening";
            /* 保留种子数据里的静默来源，让「活跃 / 静默」两种状态同屏可见 */
            state.clients.forEach(function (c, i) {
                if (i < 3) {
                    c.lastSeen = Date.now() - (i + 1) * 1200;
                }
            });
            renderClients();
            renderMessages();
            renderListen();
            setStatus('监听已启动 · <b>' + endpoint() + '</b>', "ok");
            return;
        }

        setListen(target);
    }

    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            applyDemoStatus(btn.dataset.status);
        });
    });

    /* ---------------- 初始化 ---------------- */

    setInterval(updateTimer, 1000);
    updateBcastHint();

    var hashState = (location.hash || "").replace("#", "");
    var demoStates = ["listening", "binding", "stopped", "empty", "noselect"];

    if (demoStates.indexOf(hashState) >= 0) {
        applyDemoStatus(hashState);
    } else {
        renderClients();
        renderMessages();
        renderListen();
        setStatus('就绪 · 监听 <b>' + endpoint() + '</b> · Ctrl + Enter 快速发送');
    }

    scrollToBottom();
    window.addEventListener("load", scrollToBottom);

    /* 模拟收包：每 2.6s 一次，让活跃时间/未读/新发现真实滚动起来 */
    state.rxTimer = setInterval(simulateReceive, 2600);
})();
