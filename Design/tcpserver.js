/* ============================================================
   TCP 服务端 · 重设计稿交互脚本（仅用于演示设计效果）
   ============================================================ */
(function () {
    "use strict";

    var state = {
        listen: "listening",            // listening | starting | stopped
        startedAt: Date.now() - 3731000,
        view: "hex",                    // 消息显示编码：hex | ascii
        enc: "hex",                     // 发送编码：hex | ascii
        broadcast: false,               // 广播全部在线客户端
        loopTimer: null,
        selectedId: "c1",
        clients: buildSeedClients()
    };

    var el = {
        statePill: document.getElementById("statePill"),
        stateDot: document.getElementById("stateDot"),
        statePillText: document.getElementById("statePillText"),
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        host: document.getElementById("host"),
        port: document.getElementById("port"),
        portWrap: document.getElementById("portWrap"),
        reuseSwitch: document.getElementById("reuseSwitch"),
        btnListen: document.getElementById("btnListen"),
        btnListenText: document.getElementById("btnListenText"),
        runTimer: document.getElementById("runTimer"),
        statOnline: document.getElementById("statOnline"),
        statTotal: document.getElementById("statTotal"),
        statUnread: document.getElementById("statUnread"),
        statTxCount: document.getElementById("statTxCount"),
        statTxBytes: document.getElementById("statTxBytes"),
        statRxCount: document.getElementById("statRxCount"),
        statRxBytes: document.getElementById("statRxBytes"),
        clientList: document.getElementById("clientList"),
        clientEmpty: document.getElementById("clientEmpty"),
        clientCount: document.getElementById("clientCount"),
        btnDisconnectAll: document.getElementById("btnDisconnectAll"),
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
        return [
            client("c1", "192.168.3.11", 52344, true, 0, [
                msg("tx", "AA 55 01 01", "12:03:41.120"),
                msg("rx", "AA 55 81 01 00", "12:03:41.198"),
                msg("tx", "AA 55 02 00", "12:03:42.005"),
                msg("rx", "AA 55 82 00 06 31 2E 30 2E 33", "12:03:42.114"),
                msg("tx", "48 65 6C 6C 6F 20 44 65 76 4B 69 74", "12:03:43.260"),
                msg("rx", "AA 55 83 01 00 32 30 32 36 30 39 32 39", "12:03:44.010")
            ]),
            client("c2", "192.168.3.27", 51880, true, 5, [
                msg("rx", "AA 55 01 02 00", "12:03:39.004"),
                msg("rx", "AA 55 01 02 01", "12:03:40.512")
            ]),
            client("c3", "192.168.3.45", 51002, true, 2, [
                msg("rx", "AA 55 04 02 0A 00", "12:03:37.880")
            ]),
            client("c4", "192.168.3.61", 49777, false, 0, [
                msg("tx", "AA 55 02 00", "11:58:02.113"),
                msg("rx", "AA 55 82 00 06 31 2E 30 2E 33", "11:58:02.221")
            ])
        ];
    }

    function client(id, ip, port, online, unread, logs) {
        return { id: id, ip: ip, port: port, online: online, unread: unread, logs: logs };
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
                /* UTF-8 编码 */
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

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
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

    /* ---------------- 查询 ---------------- */

    function endpoint() {
        return el.host.value + ":" + el.port.value;
    }

    function selectedClient() {
        return state.clients.filter(function (c) {
            return c.id === state.selectedId;
        })[0] || null;
    }

    function onlineClients() {
        return state.clients.filter(function (c) {
            return c.online;
        });
    }

    /* ---------------- 渲染 ---------------- */

    function renderClients() {
        el.clientCount.textContent = state.clients.length;
        el.clientList.style.display = state.clients.length === 0 ? "none" : "flex";
        el.clientEmpty.classList.toggle("show", state.clients.length === 0);

        el.clientList.innerHTML = state.clients.map(function (c) {
            var cls = "client-item";
            if (!c.online) {
                cls += " offline";
            }
            if (c.id === state.selectedId) {
                cls += " selected";
            }

            var unread = c.unread > 0
                ? '<span class="unread">' + (c.unread > 9 ? "9+" : c.unread) + '</span>'
                : "";

            var mini = c.online
                ? '<button class="client-mini" data-act="disconnect" title="断开该客户端">' +
                '<svg viewBox="0 0 24 24" width="12" height="12" aria-hidden="true">' +
                '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" ' +
                'stroke-linejoin="round" d="M18 6L6 18M6 6l12 12"/>' +
                '</svg></button>'
                : "";

            return '' +
                '<div class="' + cls + '" data-id="' + c.id + '">' +
                '<span class="dot ' + (c.online ? "on" : "off") + '"></span>' +
                '<span class="client-text">' +
                '<span class="client-addr">' + escapeHtml(c.ip + ":" + c.port) + '</span>' +
                '<span class="client-state">' + (c.online ? "已连接" : "已断开") + '</span>' +
                '</span>' +
                '<span class="client-side">' + unread + mini + '</span>' +
                '</div>';
        }).join("");

        renderStats();
    }

    function renderMessages() {
        var peer = selectedClient();

        /* 对端标签 */
        if (peer) {
            el.peerTag.className = "peer";
            el.peerTag.innerHTML = '<span>对端 ' + escapeHtml(peer.ip + ":" + peer.port) + '</span>';
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
            el.msgEmptyDesc.textContent = "在左侧「连接客户端」中点击一个客户端，即可查看它的收发记录；悬浮单条消息可复制。";
        } else if (state.listen === "stopped") {
            el.msgEmptyTitle.textContent = "监听已停止";
            el.msgEmptyDesc.textContent = "启动监听后客户端才能接入，历史消息仍会保留在这里。";
        } else if (list.length === 0) {
            el.msgEmptyTitle.textContent = "还没有消息记录";
            el.msgEmptyDesc.textContent = "该客户端还没有任何收发记录，可在下方发送一条数据试试。";
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
        var online = onlineClients();
        var unread = state.clients.reduce(function (sum, c) {
            return sum + (c.id === state.selectedId ? 0 : c.unread);
        }, 0);

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

        el.statOnline.textContent = online.length;
        el.statTotal.textContent = "共 " + state.clients.length + " 个";
        el.statUnread.textContent = unread;
        el.statTxCount.textContent = tx.length;
        el.statTxBytes.textContent = txBytes + " B";
        el.statRxCount.textContent = rx.length;
        el.statRxBytes.textContent = rxBytes + " B";
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
        } else if (state.listen === "starting") {
            pillClass += " pending";
            dotClass += " pending";
            pillText = "启动中";
            btnText = "取消启动";
        }

        el.statePill.className = pillClass;
        el.stateDot.className = dotClass;
        el.statePillText.textContent = pillText;
        el.btnListenText.textContent = btnText;
        el.btnListen.className = btnClass;

        /* 监听未开启时，地址/端口/复用端口只读 */
        var idle = state.listen !== "listening";
        el.host.disabled = idle;
        el.port.disabled = idle;
        el.reuseSwitch.disabled = idle;

        el.btnDisconnectAll.disabled = onlineClients().length === 0;

        updateControls();
        updateTimer();
    }

    /* 发送区可用性：监听中 + （已选中在线客户端 或 广播且存在在线客户端） */
    function canSend() {
        if (state.listen !== "listening") {
            return false;
        }
        if (state.broadcast) {
            return onlineClients().length > 0;
        }
        var peer = selectedClient();
        return !!peer && peer.online;
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
    }

    function showHint(text) {
        el.toolbarHintText.textContent = text || "";
        el.toolbarHint.classList.toggle("show", !!text);
    }

    /* 状态栏左侧：最近一次操作结果 */
    function setStatus(text, type) {
        el.statusLeft.innerHTML = text;
        el.statusLeft.className = type || "";
    }

    function setListen(status) {
        state.listen = status;

        if (status === "listening") {
            state.startedAt = Date.now();
            state.clients.forEach(function (c) {
                c.online = true;
            });
            setStatus('监听已启动 · <b>' + endpoint() + '</b>', "ok");
        } else if (status === "starting") {
            setStatus("正在启动监听 " + endpoint() + " …");
        } else {
            stopLoop();
            el.loopSwitch.checked = false;
            el.interval.disabled = true;
            state.clients.forEach(function (c) {
                c.online = false;
            });
            setStatus("监听已停止，所有客户端连接已断开");
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
            setStatus("未开启监听，无法发送消息", "err");
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
            targets = onlineClients();
            if (targets.length === 0) {
                setStatus("当前没有在线客户端，无法广播", "err");
                return;
            }
        } else {
            var peer = selectedClient();
            if (!peer) {
                setStatus("未选中客户端，无法发送消息", "err");
                return;
            }
            if (!peer.online) {
                setStatus("所选客户端已断开，请重新选择", "err");
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
            setStatus("已广播 " + sentBytes + " B 到 " + targets.length + " 个客户端", "ok");
            toast("已广播到 " + targets.length + " 个客户端");
        } else {
            var only = targets[0];
            setStatus("已发送 " + sentBytes + " B 到 " + only.ip + ":" + only.port, "ok");

            /* 演示：模拟对端应答 */
            setTimeout(function () {
                if (!only.online) {
                    return;
                }
                var reply = bytesToHex(hexToBytes(payload.hex).map(function (b, i) {
                    return i === 1 ? (b | 0x80) : b;
                }));
                only.logs.push(msg("rx", reply, nowTime()));
                if (only.id !== state.selectedId) {
                    only.unread += 1;
                }
                renderClients();
                if (only.id === state.selectedId) {
                    renderMessages();
                    scrollToBottom();
                }
            }, 280);
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
        if (state.listen === "starting") {
            setListen("stopped");
            setStatus("已取消启动监听");
            return;
        }

        var portOk = isPort(el.port.value.trim());
        el.portWrap.classList.toggle("invalid", !portOk);
        showHint(!portOk ? "监听端口需为 1 ~ 65535 的整数" : "");
        if (!portOk) {
            setStatus("监听端口需为 1 ~ 65535 的整数", "err");
            return;
        }

        setListen("starting");
        setTimeout(function () {
            if (state.listen === "starting") {
                setListen("listening");
            }
        }, 900);
    });

    el.port.addEventListener("input", function () {
        el.portWrap.classList.remove("invalid");
        showHint("");
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
        setStatus("已保存 " + peer.logs.length + " 条消息到 tcp-log-20261012.txt", "ok");
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

        if (e.target.closest(".client-mini")) {
            target.online = false;
            renderClients();
            renderMessages();
            toast("已断开 " + target.ip + ":" + target.port, true);
            return;
        }

        if (!target.online) {
            toast(target.ip + ":" + target.port + " 已断开", true);
            return;
        }

        state.selectedId = target.id;
        target.unread = 0;
        state.broadcast = false;
        el.bcastSwitch.checked = false;
        updateBcastHint();
        renderClients();
        renderMessages();
        updateControls();
        scrollToBottom();
    });

    el.btnDisconnectAll.addEventListener("click", function () {
        var online = onlineClients();
        if (online.length === 0) {
            setStatus("当前没有在线客户端", "err");
            return;
        }
        online.forEach(function (c) {
            c.online = false;
        });
        renderClients();
        renderMessages();
        toast("已断开 " + online.length + " 个客户端", true);
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
            ? "广播模式已开启：将发送给全部在线客户端"
            : "广播模式已关闭：仅发送给所选客户端");
    });

    function updateBcastHint() {
        var count = onlineClients().length;
        el.bcastHint.classList.toggle("show", state.broadcast);
        if (state.broadcast) {
            el.bcastHintText.textContent = count > 0
                ? "广播模式：本次内容将发送给全部 " + count + " 个在线客户端"
                : "广播模式：当前没有在线客户端，发送按钮不可用";
        }
        el.bcastSwitch.parentElement.title = "忽略选中项，发送给全部在线客户端";
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

    /* 消息列表：复制 */
    el.msgScroll.addEventListener("click", function (e) {
        var btn = e.target.closest(".msg-copy");
        if (!btn) {
            return;
        }
        var text = btn.dataset.copy;
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).catch(function () {
            });
        }
        toast("已复制 " + text);
    });

    /* 演示状态切换
       除点击顶部切换器外，也支持用地址栏直接进入某个状态，便于逐个状态评审：
       tcpserver.html#listening / #starting / #stopped / #empty / #noselect */
    function applyDemoStatus(target) {
        document.querySelectorAll(".demo-btn").forEach(function (b) {
            b.classList.toggle("active", b.dataset.status === target);
        });

        if (target === "empty") {
            state.clients = [];
            state.selectedId = null;
            setListen("listening");
            return;
        }

        if (state.clients.length === 0) {
            state.clients = buildSeedClients();
            state.selectedId = "c1";
        }

        stopLoop();
        el.loopSwitch.checked = false;
        el.interval.disabled = true;

        if (target === "noselect") {
            state.startedAt = Date.now() - 3731000;
            state.listen = "listening";
            state.clients.forEach(function (c) {
                c.online = true;
            });
            state.selectedId = null;
            renderClients();
            renderMessages();
            renderListen();
            setStatus("就绪 · 点击左侧客户端查看其消息记录");
            return;
        }

        if (target === "listening") {
            state.startedAt = Date.now() - 3731000;
            state.listen = "listening";
            state.clients.forEach(function (c) {
                c.online = true;
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
    var demoStates = ["listening", "starting", "stopped", "empty", "noselect"];

    if (demoStates.indexOf(hashState) >= 0) {
        applyDemoStatus(hashState);
    } else {
        renderClients();
        renderMessages();
        renderListen();
        /* 初始状态栏不重复监听信息，给一条使用提示 */
        setStatus('就绪 · 监听 <b>' + endpoint() + '</b> · Ctrl + Enter 快速发送');
    }

    scrollToBottom();

    window.addEventListener("load", scrollToBottom);
})();
