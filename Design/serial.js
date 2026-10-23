/* ============================================================
   串口调试助手 · 重设计稿交互脚本（仅用于演示设计效果）
   ============================================================ */
(function () {
    "use strict";

    /* 演示用端口列表（真实实现由后端枚举 SerialPort.GetPortNames() + 设备描述） */
    var PORTS = [
        { name: "COM3", desc: "USB-SERIAL CH340" },
        { name: "COM5", desc: "标准串行蓝牙链路" },
        { name: "COM8", desc: "USB 串行设备" }
    ];

    var PARITY_LETTER = {
        none: "N",
        odd: "O",
        even: "E",
        mark: "M",
        space: "S"
    };

    var FLOW_TEXT = {
        none: "无流控",
        xon: "XON/XOFF",
        rts: "RTS/CTS",
        dsr: "DSR/DTR"
    };

    var state = {
        conn: "opened",            // opened | opening | closed
        view: "hex",               // 消息显示编码：hex | ascii
        enc: "hex",                // 发送编码：hex | ascii
        newline: "crlf",           // 换行符：none | crlf | lf | cr
        hasPort: true,             // 是否检测到可用端口
        openedAt: Date.now() - 96000,
        loopTimer: null,
        messages: buildSeedMessages()
    };

    var el = {
        statePill: document.getElementById("statePill"),
        stateDot: document.getElementById("stateDot"),
        statePillText: document.getElementById("statePillText"),
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        port: document.getElementById("port"),
        portWrap: document.getElementById("portWrap"),
        btnRefresh: document.getElementById("btnRefresh"),
        baud: document.getElementById("baud"),
        baudWrap: document.getElementById("baudWrap"),
        baudCustom: document.getElementById("baudCustom"),
        baudCustomWrap: document.getElementById("baudCustomWrap"),
        btnOpen: document.getElementById("btnOpen"),
        btnOpenText: document.getElementById("btnOpenText"),
        dataBits: document.getElementById("dataBits"),
        stopBits: document.getElementById("stopBits"),
        parity: document.getElementById("parity"),
        flow: document.getElementById("flow"),
        paramGroup: document.getElementById("paramGroup"),
        connTimer: document.getElementById("connTimer"),
        statTxCount: document.getElementById("statTxCount"),
        statTxBytes: document.getElementById("statTxBytes"),
        statRxCount: document.getElementById("statRxCount"),
        statRxBytes: document.getElementById("statRxBytes"),
        cmdList: document.getElementById("cmdList"),
        cmdCount: document.getElementById("cmdCount"),
        cmdEmpty: document.getElementById("cmdEmpty"),
        btnAddCmd: document.getElementById("btnAddCmd"),
        msgScroll: document.getElementById("msgScroll"),
        msgEmpty: document.getElementById("msgEmpty"),
        msgCount: document.getElementById("msgCount"),
        viewSeg: document.getElementById("viewSeg"),
        sendSeg: document.getElementById("sendSeg"),
        btnSave: document.getElementById("btnSave"),
        btnClear: document.getElementById("btnClear"),
        input: document.getElementById("input"),
        newline: document.getElementById("newline"),
        btnScript: document.getElementById("btnScript"),
        btnSend: document.getElementById("btnSend"),
        loopSwitch: document.getElementById("loopSwitch"),
        interval: document.getElementById("interval"),
        statusLeft: document.getElementById("statusLeft"),
        statusRight: document.getElementById("statusRight"),
        toast: document.getElementById("toast")
    };

    /* ---------------- 演示数据 ---------------- */

    function buildSeedMessages() {
        return [
            msg("tx", "01 03 00 00 00 02 C4 0B", "12:03:41.120"),
            msg("rx", "01 03 04 00 1F 00 2A 9A 3F", "12:03:41.198"),
            msg("tx", "41 54 0D 0A", "12:03:42.005"),
            msg("rx", "4F 4B 0D 0A", "12:03:42.114"),
            msg("tx", "AA 55 01 01", "12:03:43.260"),
            msg("rx", "AA 55 81 01 00", "12:03:43.322"),
            msg("rx", "24 47 50 52 4D 43 2C 31 32 33 35 31 39 2E 30 30", "12:03:43.640"),
            msg("tx", "01 06 00 01 00 64", "12:03:43.880"),
            msg("rx", "01 06 00 01 00 64 D8 1B", "12:03:44.010"),
            msg("tx", "48 65 6C 6C 6F 20 53 65 72 69 61 6C", "12:03:44.140"),
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

    function isBaud(str) {
        return /^\d+$/.test(String(str)) && Number(str) >= 1 && Number(str) <= 12000000;
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

    /* ---------------- 端口与参数 ---------------- */

    function renderPorts() {
        var previous = el.port.value;
        el.port.innerHTML = "";

        if (!state.hasPort) {
            var none = document.createElement("option");
            none.value = "";
            none.textContent = "无可用串行端口";
            el.port.appendChild(none);
            el.port.disabled = true;
            el.btnOpen.disabled = true;
            el.btnOpen.title = "未检测到可用串行端口";
            return;
        }

        PORTS.forEach(function (p) {
            var opt = document.createElement("option");
            opt.value = p.name;
            opt.textContent = p.name + " · " + p.desc;
            el.port.appendChild(opt);
        });

        el.port.disabled = false;
        el.port.value = PORTS.some(function (p) {
            return p.name === previous;
        }) ? previous : PORTS[0].name;

        el.btnOpen.disabled = false;
        el.btnOpen.title = "";
    }

    function baudValue() {
        return el.baud.value === "custom" ? el.baudCustom.value.trim() : el.baud.value;
    }

    function paramSummary() {
        var parity = PARITY_LETTER[el.parity.value] || "N";
        return baudValue() + " · " + el.dataBits.value + parity + el.stopBits.value +
            " · " + (FLOW_TEXT[el.flow.value] || "无流控");
    }

    function portName() {
        return state.hasPort ? el.port.value : "—";
    }

    function sessionText() {
        return portName() + " · " + paramSummary();
    }

    function renderBaudCustom() {
        el.baudCustomWrap.classList.toggle("show", el.baud.value === "custom");
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

        el.statusRight.textContent = (lastRx ? "最后接收 " + lastRx.time : "暂无接收数据") +
            " · 消息列表自动滚动";
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

    /* ---------------- 串口开关 ---------------- */

    function durationText() {
        var sec = Math.floor((Date.now() - state.openedAt) / 1000);
        return pad(Math.floor(sec / 3600)) + ":" + pad(Math.floor(sec % 3600 / 60)) + ":" + pad(sec % 60);
    }

    function showHint(text) {
        el.toolbarHintText.textContent = text || "";
        el.toolbarHint.classList.toggle("show", !!text);
    }

    function setStatus(text, type) {
        el.statusLeft.textContent = text;
        el.statusLeft.className = type || "";
    }

    function setConn(status) {
        state.conn = status;
        var pillClass = "state-pill";
        var dotClass = "dot";
        var pillText = "";

        if (status === "opened") {
            pillClass += " on";
            dotClass += " on";
            pillText = "已打开";
            state.openedAt = state.openedAt || Date.now();
            el.btnOpenText.textContent = "关闭串口";
            setStatus("已打开 " + sessionText(), "ok");
        } else if (status === "opening") {
            pillClass += " pending";
            dotClass += " pending";
            pillText = "打开中";
            el.btnOpenText.textContent = "取消打开";
            setStatus("正在打开 " + sessionText() + " …");
        } else {
            pillText = "未打开";
            el.btnOpenText.textContent = "打开串口";
            setStatus("未打开 · 当前 " + sessionText());
        }

        el.statePill.className = pillClass;
        el.stateDot.className = dotClass;
        el.statePillText.textContent = pillText;

        var enabled = status === "opened";
        el.btnSend.disabled = !enabled;
        el.input.disabled = !enabled;
        el.loopSwitch.disabled = !enabled;

        /* 串口打开/打开中时锁定配置项：参数改动必须重新打开串口才生效 */
        var locked = status !== "closed";
        el.port.disabled = locked;
        el.btnRefresh.disabled = locked;
        el.baud.disabled = locked;
        el.baudCustom.disabled = locked;
        el.dataBits.disabled = locked;
        el.stopBits.disabled = locked;
        el.parity.disabled = locked;
        el.flow.disabled = locked;
        el.paramGroup.classList.toggle("disabled", locked);

        if (!enabled) {
            stopLoop();
        }

        updateTimer();
    }

    function updateTimer() {
        el.connTimer.textContent = state.conn === "opened" ? durationText() : "—";
    }

    /* ---------------- 发送 ---------------- */

    function newlineBytes() {
        if (state.newline === "crlf") {
            return [0x0D, 0x0A];
        }
        if (state.newline === "lf") {
            return [0x0A];
        }
        if (state.newline === "cr") {
            return [0x0D];
        }
        return [];
    }

    function currentPayload() {
        var raw = el.input.value;
        if (!raw.trim()) {
            return { error: "不能发送空数据" };
        }

        var bytes;
        if (state.enc === "hex") {
            if (!isHexString(raw)) {
                return { error: "HEX 格式错误，应为空格分隔的两位十六进制，如 01 03 00 00" };
            }
            bytes = hexToBytes(raw);
        } else {
            bytes = textToHex(raw);
        }

        bytes = bytes.concat(newlineBytes());
        return { hex: bytesToHex(bytes) };
    }

    function payloadOf(text) {
        var bytes = isHexString(text) ? hexToBytes(text) : textToHex(text);
        return { hex: bytesToHex(bytes.concat(newlineBytes())) };
    }

    /* 演示用模拟下位机应答 */
    function fakeReply(hex) {
        var bytes = hexToBytes(hex);
        var text = bytesToText(bytes).trim().toUpperCase();

        if (text.indexOf("AT") === 0) {
            return "4F 4B 0D 0A";                       /* OK\r\n */
        }
        if (bytes[0] === 0x01 && bytes[1] === 0x03) {
            return "01 03 04 00 1F 00 2A 9A 3F";        /* Modbus 读寄存器应答 */
        }
        if (bytes[0] === 0x01 && bytes[1] === 0x06) {
            return bytesToHex(bytes);                    /* 写寄存器原样应答 */
        }
        return bytesToHex(bytes.map(function (b, i) {
            return i === 1 ? (b | 0x80) : b;
        }));
    }

    function send(text) {
        if (state.conn !== "opened") {
            setStatus("串口未打开，无法发送数据", "err");
            return;
        }

        var payload = text === undefined ? currentPayload() : payloadOf(text);
        if (payload.error) {
            setStatus(payload.error, "err");
            return;
        }

        var sentBytes = hexToBytes(payload.hex).length;
        state.messages.push(msg("tx", payload.hex, nowTime()));
        renderMessages();
        renderStats();
        scrollToBottom();
        setStatus("已发送 " + sentBytes + " B → " + portName(), "ok");

        setTimeout(function () {
            state.messages.push(msg("rx", fakeReply(payload.hex), nowTime()));
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

    /* ---------------- 事件绑定：工具条 ---------------- */

    el.btnOpen.addEventListener("click", function () {
        if (!state.hasPort) {
            showHint("未检测到可用串行端口，请连接设备后点击刷新");
            setStatus("未检测到可用串行端口", "err");
            return;
        }

        if (state.conn === "opened") {
            setConn("closed");
            state.openedAt = 0;
            setStatus("已关闭串口 " + portName());
            return;
        }

        if (state.conn === "opening") {
            setConn("closed");
            setStatus("已取消打开串口");
            return;
        }

        var baudOk = isBaud(baudValue());
        el.baudWrap.classList.toggle("invalid", !baudOk);
        el.baudCustomWrap.classList.toggle("invalid", el.baud.value === "custom" && !baudOk);
        showHint(baudOk ? "" : "波特率需为 1 ~ 12000000 的整数");
        if (!baudOk) {
            setStatus("波特率不合法，无法打开串口", "err");
            return;
        }

        setConn("opening");
        setTimeout(function () {
            if (state.conn !== "opening") {
                return;
            }
            state.openedAt = Date.now();
            setConn("opened");
        }, 900);
    });

    el.btnRefresh.addEventListener("click", function () {
        state.hasPort = true;
        renderPorts();
        showHint("");
        setStatus("已刷新端口列表，发现 " + PORTS.length + " 个串行端口", "ok");
        toast("已刷新端口列表，发现 " + PORTS.length + " 个串行端口");
    });

    el.port.addEventListener("change", function () {
        showHint("");
        if (state.conn === "opened") {
            setStatus("已打开 " + sessionText(), "ok");
            toast("端口已切换为 " + portName() + "，重新打开后生效");
        }
    });

    el.baud.addEventListener("change", function () {
        renderBaudCustom();
        if (el.baud.value === "custom") {
            el.baudCustom.focus();
            el.baudCustom.select();
        }
        if (state.conn === "opened") {
            toast("波特率已修改，重新打开串口后生效");
        }
    });

    el.baudCustom.addEventListener("input", function () {
        toast("波特率已修改，下次打开串口时生效");
    });

    ["dataBits", "stopBits", "parity", "flow"].forEach(function (id) {
        el[id].addEventListener("change", function () {
            toast("参数已修改为 " + paramSummary() + "，下次打开串口时生效");
        });
    });

    /* ---------------- 事件绑定：消息区与发送区 ---------------- */

    el.btnScript.addEventListener("click", function () {
        toast("打开「指令脚本」对话框");
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
        setStatus("已保存 " + state.messages.length + " 条消息到 serial-log-20261014.txt", "ok");
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
            ? "输入要发送的内容；HEX 模式下使用空格分隔，例如 01 03 00 00 00 02"
            : "输入要发送的文本内容，将按 UTF-8 编码发送";
    });

    el.newline.addEventListener("change", function () {
        state.newline = el.newline.value;
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

    el.btnAddCmd.addEventListener("click", function () {
        toast("新增扩展指令（弹出编辑对话框）");
    });

    /* ---------------- 演示状态切换 ---------------- */

    /* 演示状态切换：除点击顶部切换器外，也支持地址栏直达，便于逐个状态评审
       serial.html#opened / #opening / #closed / #noport / #empty */
    function applyDemoStatus(target) {
        document.querySelectorAll(".demo-btn").forEach(function (b) {
            b.classList.toggle("active", b.dataset.status === target);
        });

        if (target === "empty") {
            state.hasPort = true;
            renderPorts();
            state.messages = [];
            state.openedAt = Date.now() - 96000;
            setConn("opened");
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

        if (target === "noport") {
            state.hasPort = false;
            renderPorts();
            state.openedAt = 0;
            setConn("closed");
            showHint("未检测到可用串行端口，请连接设备后点击刷新");
            setStatus("未检测到可用串行端口", "err");
            return;
        }

        state.hasPort = true;
        renderPorts();
        showHint("");

        if (target === "opened") {
            state.openedAt = Date.now() - 96000;
            setConn("opened");
        } else if (target === "opening") {
            setConn("opening");
        } else {
            state.openedAt = 0;
            setConn("closed");
        }
    }

    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            applyDemoStatus(btn.dataset.status);
        });
    });

    /* ---------------- 初始化 ---------------- */

    setInterval(updateTimer, 1000);
    renderBaudCustom();
    renderCommands();

    var hashState = (location.hash || "").replace("#", "");
    var demoStates = ["opened", "opening", "closed", "noport", "empty"];

    if (demoStates.indexOf(hashState) >= 0) {
        applyDemoStatus(hashState);
    } else {
        renderPorts();
        renderMessages();
        renderStats();
        setConn("opened");
        setStatus("就绪 · HEX 模式示例：01 03 00 00 00 02 · Ctrl + Enter 快速发送");
    }

    scrollToBottom();

    window.addEventListener("load", scrollToBottom);
})();
