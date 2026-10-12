/* ============================================================
   UDP 客户端 · 重设计稿交互脚本（仅用于演示设计效果）
   UDP 无连接、无握手：没有「连接中/已连接」这类状态机，
   只有「对端是否已指定」这一个配置事实，应用对端后即时生效。
   ============================================================ */
(function () {
    "use strict";

    var state = {
        peerReady: false,           // 对端是否已指定（唯一状态，非连接状态机）
        view: "hex",                // 消息显示编码：hex | ascii
        enc: "hex",                 // 发送编码：hex | ascii
        loopTimer: null,
        messages: buildSeedMessages()
    };

    var el = {
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        btnApply: document.getElementById("btnApply"),
        host: document.getElementById("host"),
        port: document.getElementById("port"),
        hostWrap: document.getElementById("hostWrap"),
        portWrap: document.getElementById("portWrap"),
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
            msg("tx", "AA 55 01 01", "12:03:41.120"),
            msg("rx", "AA 55 81 01 00", "12:03:41.198"),
            msg("tx", "AA 55 02 00", "12:03:42.005"),
            msg("rx", "AA 55 82 00 06 31 2E 30 2E 33", "12:03:42.114"),
            msg("tx", "48 65 6C 6C 6F 20 44 65 76 4B 69 74", "12:03:43.260"),
            msg("rx", "48 65 6C 6C 6F 20 55 44 50 0A", "12:03:43.322"),
            msg("tx", "AA 55 03 01 FF", "12:03:43.880"),
            msg("rx", "AA 55 83 01 00 32 30 32 36 30 39 32 39", "12:03:44.010"),
            msg("tx", "AA 55 02 00", "12:03:44.140"),
            msg("rx", "AA 55 82 00 06 31 2E 30 2E 33", "12:03:44.196"),
            msg("rx", "AA 55 04 02 0A 00", "12:03:44.201")
        ];
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
        var buf = [];
        for (var i = 0; i < bytes.length; i++) {
            var b = bytes[i];
            buf.push(b >= 32 && b < 127 ? String.fromCharCode(b) : ".");
        }
        return buf.join("");
    }

    function isHexString(str) {
        return /^([0-9a-fA-F]{2})(\s+[0-9a-fA-F]{2})*$/.test(str.trim());
    }

    function isIp(str) {
        return /^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$/.test(str);
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

    /* ---------------- 渲染 ---------------- */

    function renderMessages() {
        var list = state.messages;

        el.msgCount.textContent = state.messages.length;
        el.msgScroll.style.display = list.length === 0 ? "none" : "flex";
        el.msgEmpty.classList.toggle("show", list.length === 0);

        var html = list.map(function (m) {
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
            return sum + hexToBytes(m.hex).length;
        }, 0);
        var rxBytes = rx.reduce(function (sum, m) {
            return sum + hexToBytes(m.hex).length;
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

    /* ---------------- 对端指定状态 ---------------- */

    function address() {
        return el.host.value.trim() + ":" + el.port.value.trim();
    }

    function showHint(text) {
        el.toolbarHintText.textContent = text || "";
        el.toolbarHint.classList.toggle("show", !!text);
    }

    /* 状态栏左侧：最近一次操作结果（应用对端/发送/保存等主流程反馈） */
    function setStatus(text, type) {
        el.statusLeft.textContent = text;
        el.statusLeft.className = type || "";
    }

    function setPeerReady(ready) {
        state.peerReady = ready;

        /* 对端是否已指定：不重复展示地址，仅通过发送区可用性 + 状态栏提示体现 */
        var enabled = ready;
        el.btnSend.disabled = !enabled;
        el.input.disabled = !enabled;
        el.loopSwitch.disabled = !enabled;

        if (!enabled) {
            stopLoop();
        }
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
        if (!state.peerReady) {
            setStatus("尚未指定对端，无法发送消息", "err");
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

        var sentBytes = hexToBytes(payload.hex).length;
        state.messages.push(msg("tx", payload.hex, nowTime()));
        renderMessages();
        renderStats();
        scrollToBottom();
        setStatus("已发送 " + sentBytes + " B 到 " + address(), "ok");

        /* 演示：模拟对端数据报回包 */
        setTimeout(function () {
            var reply = bytesToHex(hexToBytes(payload.hex).map(function (b, i) {
                return i === 1 ? (b | 0x80) : b;
            }));
            state.messages.push(msg("rx", reply, nowTime()));
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

    /* 应用对端：校验通过即时生效，无握手、无过程态 */
    el.btnApply.addEventListener("click", function () {
        var hostOk = isIp(el.host.value.trim());
        var portOk = isPort(el.port.value.trim());
        el.hostWrap.classList.toggle("invalid", !hostOk);
        el.portWrap.classList.toggle("invalid", !portOk);
        showHint(!hostOk ? "对端地址格式不正确，应为 IPv4 地址，如 127.0.0.1"
            : !portOk ? "端口号需为 1 ~ 65535 的整数" : "");
        if (!hostOk || !portOk) {
            setStatus(!hostOk ? "对端地址格式不正确" : "端口号需为 1 ~ 65535 的整数", "err");
            return;
        }

        setPeerReady(true);
        setStatus("对端已指定 " + address() + "，可直接发送数据报", "ok");
    });

    el.host.addEventListener("input", function () {
        el.hostWrap.classList.remove("invalid");
        showHint("");
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
        setStatus("已保存 " + state.messages.length + " 条消息到 udp-log-20261012.txt", "ok");
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

    /* 分段控件：筛选 / 显示编码 / 发送编码 */
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

        el.input.value = value;
        el.input.focus();
        toast("已填入输入框：" + value);
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

            var ready = target === "ready";
            setPeerReady(ready);
            setStatus(ready
                ? "对端已指定 " + address() + "，可直接发送数据报"
                : "对端未指定，发送已停用",
                ready ? "ok" : "");
        });
    });

    /* ---------------- 初始化 ---------------- */

    renderCommands();
    renderMessages();
    renderStats();
    setPeerReady(true);
    /* 初始状态栏不重复对端信息，给一条使用提示 */
    setStatus("就绪 · HEX 模式示例：AA 55 01 01 · Ctrl + Enter 快速发送");
    scrollToBottom();

    window.addEventListener("load", scrollToBottom);
})();
