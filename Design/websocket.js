/* ============================================================
   WebSocket 客户端 · 重设计稿交互脚本（仅用于演示设计效果）
   ============================================================ */
(function () {
    "use strict";

    var state = {
        conn: "connected",          // connected | connecting | offline
        scheme: "ws",               // ws | wss
        view: "hex",                // 二进制帧显示编码：hex | ascii
        enc: "ascii",               // 发送编码：ascii（文本帧）| hex（二进制帧）
        connectedAt: Date.now() - 84000,
        loopTimer: null,
        messages: buildSeedMessages()
    };

    var el = {
        statePill: document.getElementById("statePill"),
        stateDot: document.getElementById("stateDot"),
        statePillText: document.getElementById("statePillText"),
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        connTimer: document.getElementById("connTimer"),
        btnConnect: document.getElementById("btnConnect"),
        btnConnectText: document.getElementById("btnConnectText"),
        schemeSeg: document.getElementById("schemeSeg"),
        host: document.getElementById("host"),
        port: document.getElementById("port"),
        path: document.getElementById("path"),
        hostWrap: document.getElementById("hostWrap"),
        portWrap: document.getElementById("portWrap"),
        pathWrap: document.getElementById("pathWrap"),
        statTxCount: document.getElementById("statTxCount"),
        statTxBytes: document.getElementById("statTxBytes"),
        statRxCount: document.getElementById("statRxCount"),
        statRxBytes: document.getElementById("statRxBytes"),
        cmdList: document.getElementById("cmdList"),
        cmdCount: document.getElementById("cmdCount"),
        cmdEmpty: document.getElementById("cmdEmpty"),
        msgScroll: document.getElementById("msgScroll"),
        msgEmpty: document.getElementById("msgEmpty"),
        msgCount: document.getElementById("msgCount"),
        viewSeg: document.getElementById("viewSeg"),
        sendSeg: document.getElementById("sendSeg"),
        input: document.getElementById("input"),
        btnSend: document.getElementById("btnSend"),
        btnSave: document.getElementById("btnSave"),
        btnClear: document.getElementById("btnClear"),
        btnScript: document.getElementById("btnScript"),
        loopSwitch: document.getElementById("loopSwitch"),
        interval: document.getElementById("interval"),
        statusLeft: document.getElementById("statusLeft"),
        statusRight: document.getElementById("statusRight"),
        toast: document.getElementById("toast")
    };

    /* ---------------- 演示数据 ---------------- */

    function buildSeedMessages() {
        return [
            msg("tx", "text", '{"cmd":"subscribe","topic":"device/status"}', "12:03:40.112"),
            msg("rx", "text", '{"code":0,"msg":"ok","data":{"online":12}}', "12:03:40.190"),
            msg("tx", "bin", "AA 55 01 01", "12:03:41.005"),
            msg("rx", "bin", "AA 55 81 01 00", "12:03:41.082"),
            msg("tx", "text", '{"cmd":"ping"}', "12:03:42.300"),
            msg("rx", "text", '{"cmd":"pong","t":1224}', "12:03:42.336"),
            msg("tx", "bin", "AA 55 02 00", "12:03:43.260"),
            msg("rx", "bin", "AA 55 82 00 06 31 2E 30 2E 33", "12:03:43.322"),
            msg("rx", "text", '{"event":"status","id":"A-1024","online":true}', "12:03:44.201")
        ];
    }

    /* kind: text 文本帧 | bin 二进制帧 */
    function msg(dir, kind, content, time) {
        return { dir: dir, kind: kind, content: content, time: time };
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
        var buf = [];
        for (var i = 0; i < bytes.length; i++) {
            var b = bytes[i];
            buf.push(b >= 32 && b < 127 ? String.fromCharCode(b) : ".");
        }
        return buf.join("");
    }

    /* 字节长度：二进制按字节计，文本按 UTF-8 编码长度计 */
    function messageBytes(m) {
        return m.kind === "bin" ? hexToBytes(m.content).length : textToHex(m.content).length;
    }

    function isHexString(str) {
        return /^([0-9a-fA-F]{2})(\s+[0-9a-fA-F]{2})*$/.test(str.trim());
    }

    /* 服务器地址：IPv4 或域名 */
    function isHost(str) {
        return /^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$/.test(str) ||
            /^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?)*$/.test(str);
    }

    function isPort(str) {
        return /^\d+$/.test(str) && Number(str) >= 1 && Number(str) <= 65535;
    }

    /* 路径：以 / 开头，可为空 */
    function isPath(str) {
        return str === "" || /^\/\S*$/.test(str);
    }

    function wsUrl() {
        return state.scheme + "://" + el.host.value.trim() + ":" + el.port.value.trim() + el.path.value.trim();
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

    /* ---------------- 渲染 ---------------- */

    function renderMessages() {
        var list = state.messages;

        el.msgCount.textContent = state.messages.length;
        el.msgScroll.style.display = list.length === 0 ? "none" : "flex";
        el.msgEmpty.classList.toggle("show", list.length === 0);

        var html = list.map(function (m) {
            var isTx = m.dir === "tx";
            var body;
            if (m.kind === "text") {
                body = m.content;
            } else {
                var bytes = hexToBytes(m.content);
                body = state.view === "hex" ? bytesToHex(bytes) : bytesToText(bytes);
            }
            return '' +
                '<div class="msg ' + m.dir + '">' +
                '<div class="msg-head">' +
                '<span class="tag tag-' + m.dir + '">' + (isTx ? "发送" : "接收") + '</span>' +
                '<span class="tag tag-kind">' + (m.kind === "text" ? "文本帧" : "二进制帧") + '</span>' +
                '<span class="msg-meta">' + escapeHtml(m.time) + '</span>' +
                '<span class="msg-meta">' + messageBytes(m) + ' B</span>' +
                '<button class="msg-copy" data-copy="' + escapeHtml(m.content) + '" title="复制内容">' +
                '<svg viewBox="0 0 24 24" width="11" height="11" aria-hidden="true">' +
                '<rect x="9" y="9" width="12" height="12" rx="2" fill="none" stroke="currentColor" stroke-width="2"/>' +
                '<path fill="none" stroke="currentColor" stroke-width="2" d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>' +
                '</svg>复制</button>' +
                '</div>' +
                '<div class="msg-body">' + escapeHtml(body) + '</div>' +
                '</div>';
        }).join("");

        el.msgScroll.innerHTML = html;

        var lastRx = list.filter(function (m) {
            return m.dir === "rx";
        }).pop();
        el.statusRight.textContent = lastRx
            ? "最后接收 " + lastRx.time + " · 消息列表自动滚动"
            : "暂无接收数据 · 消息列表自动滚动";
    }

    function renderStats() {
        var tx = state.messages.filter(function (m) {
            return m.dir === "tx";
        });
        var rx = state.messages.filter(function (m) {
            return m.dir === "rx";
        });

        var txBytes = tx.reduce(function (sum, m) {
            return sum + messageBytes(m);
        }, 0);
        var rxBytes = rx.reduce(function (sum, m) {
            return sum + messageBytes(m);
        }, 0);

        el.statTxCount.textContent = tx.length;
        el.statTxBytes.textContent = txBytes + " B";
        el.statRxCount.textContent = rx.length;
        el.statRxBytes.textContent = rxBytes + " B";
    }

    function renderCommands() {
        var items = el.cmdList.querySelectorAll(".cmd-item");
        el.cmdCount.textContent = items.length;
        el.cmdList.style.display = items.length === 0 ? "none" : "flex";
        el.cmdEmpty.hidden = items.length !== 0;
    }

    function scrollToBottom() {
        el.msgScroll.scrollTop = el.msgScroll.scrollHeight;
    }

    /* ---------------- 连接状态 ---------------- */

    function durationText() {
        var sec = Math.floor((Date.now() - state.connectedAt) / 1000);
        return pad(Math.floor(sec / 3600)) + ":" + pad(Math.floor(sec % 3600 / 60)) + ":" + pad(sec % 60);
    }

    function showHint(text) {
        el.toolbarHintText.textContent = text || "";
        el.toolbarHint.classList.toggle("show", !!text);
    }

    /* 状态栏左侧：最近一次操作结果（连接/发送/保存等主流程反馈） */
    function setStatus(text, type) {
        el.statusLeft.textContent = text;
        el.statusLeft.className = type || "";
    }

    function setConn(status) {
        state.conn = status;
        var pillClass = "state-pill";
        var dotClass = "dot";
        var pillText = "";

        if (status === "connected") {
            pillClass += " on";
            dotClass += " on";
            pillText = "已连接";
            state.connectedAt = state.connectedAt || Date.now();
            el.btnConnectText.textContent = "断开连接";
        } else if (status === "connecting") {
            pillClass += " pending";
            dotClass += " pending";
            pillText = "连接中";
            el.btnConnectText.textContent = "取消连接";
            setStatus("正在连接 " + wsUrl() + " …");
        } else {
            pillText = "未连接";
            el.btnConnectText.textContent = "连接";
            setStatus("未连接");
        }

        el.statePill.className = pillClass;
        el.stateDot.className = dotClass;
        el.statePillText.textContent = pillText;

        var enabled = status === "connected";
        el.btnSend.disabled = !enabled;
        el.input.disabled = !enabled;
        el.loopSwitch.disabled = !enabled;

        if (!enabled) {
            stopLoop();
        }

        updateTimer();
    }

    function updateTimer() {
        el.connTimer.textContent = state.conn === "connected" ? durationText() : "—";
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
            return { kind: "bin", content: bytesToHex(hexToBytes(raw)) };
        }

        return { kind: "text", content: raw };
    }

    function send(text) {
        if (state.conn !== "connected") {
            setStatus("未连接成功，无法发送消息", "err");
            return;
        }

        var payload;
        if (text === undefined) {
            payload = currentPayload();
        } else if (isHexString(text)) {
            payload = { kind: "bin", content: bytesToHex(hexToBytes(text)) };
        } else {
            payload = { kind: "text", content: text };
        }

        if (payload.error) {
            setStatus(payload.error, "err");
            return;
        }

        var sentBytes = messageBytes(payload);
        state.messages.push(msg("tx", payload.kind, payload.content, nowTime()));
        renderMessages();
        renderStats();
        scrollToBottom();
        setStatus("已发送 " + sentBytes + " B · " + (payload.kind === "text" ? "文本帧" : "二进制帧"), "ok");

        /* 演示：模拟对端应答 */
        setTimeout(function () {
            var reply;
            if (payload.kind === "text") {
                reply = '{"code":0,"msg":"ok"}';
            } else {
                reply = bytesToHex(hexToBytes(payload.content).map(function (b, i) {
                    return i === 1 ? (b | 0x80) : b;
                }));
            }
            state.messages.push(msg("rx", payload.kind, reply, nowTime()));
            renderMessages();
            renderStats();
            scrollToBottom();
        }, 260);
    }

    function stopLoop() {
        if (state.loopTimer) {
            clearInterval(state.loopTimer);
            state.loopTimer = null;
        }
    }

    /* ---------------- 事件绑定 ---------------- */

    el.btnConnect.addEventListener("click", function () {
        if (state.conn === "connected") {
            setConn("offline");
            state.connectedAt = 0;
            setStatus("已断开连接");
            return;
        }
        if (state.conn === "connecting") {
            setConn("offline");
            setStatus("已取消连接");
            return;
        }

        var hostOk = isHost(el.host.value.trim());
        var portOk = isPort(el.port.value.trim());
        var pathOk = isPath(el.path.value.trim());
        el.hostWrap.classList.toggle("invalid", !hostOk);
        el.portWrap.classList.toggle("invalid", !portOk);
        el.pathWrap.classList.toggle("invalid", !pathOk);

        var hint = !hostOk ? "服务器地址格式不正确，应为 IPv4 或域名，如 127.0.0.1 或 echo.example.com"
            : !portOk ? "端口号需为 1 ~ 65535 的整数"
                : !pathOk ? "路径需以 / 开头，如 /ws，也可为空" : "";
        showHint(hint);
        if (!hostOk || !portOk || !pathOk) {
            setStatus(hint, "err");
            return;
        }

        setConn("connecting");
        setTimeout(function () {
            if (state.conn !== "connecting") {
                return;
            }
            state.connectedAt = Date.now();
            setConn("connected");
            setStatus("已连接 " + wsUrl(), "ok");
        }, 900);
    });

    [el.host, el.port, el.path].forEach(function (input) {
        input.addEventListener("input", function () {
            el.hostWrap.classList.remove("invalid");
            el.portWrap.classList.remove("invalid");
            el.pathWrap.classList.remove("invalid");
            showHint("");
        });
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
        state.messages = [];
        renderMessages();
        renderStats();
        setStatus("已清空消息记录");
    });

    el.btnSave.addEventListener("click", function () {
        if (state.messages.length === 0) {
            setStatus("没有需要保存的日志", "err");
            return;
        }
        setStatus("已保存 " + state.messages.length + " 条消息到 websocket-log-20261012.txt", "ok");
    });

    el.btnScript.addEventListener("click", function () {
        toast("打开「指令脚本」对话框");
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

    /* 分段控件：协议 / 筛选 / 二进制显示编码 / 发送编码 */
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

    bindSegmented(el.schemeSeg, "scheme", function () {
        if (state.conn === "connected") {
            setStatus("协议已切换为 " + state.scheme.toUpperCase() + "，重新连接后生效", "err");
        }
    });
    bindSegmented(el.viewSeg, "view", renderMessages);
    bindSegmented(el.sendSeg, "enc", function () {
        el.input.placeholder = state.enc === "hex"
            ? "输入要发送的二进制帧内容，使用空格分隔，例如 AA 55 01 01"
            : "输入要发送的文本帧内容（JSON 或纯文本）";
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
        toast("已复制 " + (text.length > 40 ? text.slice(0, 40) + "…" : text));
    });

    /* 扩展指令列表 */
    el.cmdList.addEventListener("click", function (e) {
        var item = e.target.closest(".cmd-item");
        if (!item) {
            return;
        }

        var mini = e.target.closest(".cmd-mini");
        var value = item.dataset.cmd;

        if (mini) {
            e.stopPropagation();
            var act = mini.dataset.act;
            if (act === "send") {
                send(value);
            } else if (act === "edit") {
                toast("编辑指令：" + value);
            } else {
                item.remove();
                renderCommands();
                toast("已删除指令 " + value);
            }
            return;
        }

        /* HEX 指令自动切到 HEX 编码，文本指令切回文本 */
        var hex = isHexString(value);
        state.enc = hex ? "hex" : "ascii";
        el.sendSeg.querySelectorAll("button").forEach(function (b) {
            b.classList.toggle("active", b.dataset.enc === state.enc);
        });
        el.input.placeholder = hex
            ? "输入要发送的二进制帧内容，使用空格分隔，例如 AA 55 01 01"
            : "输入要发送的文本帧内容（JSON 或纯文本）";

        el.input.value = value;
        el.input.focus();
        toast("已填入输入框：" + (value.length > 40 ? value.slice(0, 40) + "…" : value));
    });

    document.getElementById("btnAddCmd").addEventListener("click", function () {
        toast("新增扩展指令（弹出编辑对话框）");
    });

    /* 演示状态切换 */
    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".demo-btn").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });

            var target = btn.dataset.status;
            if (target === "empty") {
                state.messages = [];
                state.connectedAt = Date.now() - 84000;
                setConn("connected");
                renderMessages();
                renderStats();
                return;
            }

            if (state.messages.length === 0) {
                state.messages = buildSeedMessages();
                renderMessages();
                renderStats();
            }

            stopLoop();
            el.loopSwitch.checked = false;
            el.interval.disabled = true;

            if (target === "connected") {
                state.connectedAt = Date.now() - 84000;
                setConn("connected");
            } else if (target === "connecting") {
                setConn("connecting");
            } else {
                state.connectedAt = 0;
                setConn("offline");
            }
        });
    });

    /* ---------------- 初始化 ---------------- */

    setInterval(updateTimer, 1000);
    renderCommands();
    renderMessages();
    renderStats();
    setConn("connected");
    /* 初始状态栏不重复连接信息，给一条使用提示 */
    setStatus("就绪 · 文本帧直接输入，HEX 模式发送二进制帧 · Ctrl + Enter 快速发送");
    scrollToBottom();

    window.addEventListener("load", scrollToBottom);
})();
