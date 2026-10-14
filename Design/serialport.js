/* ============================================================
   串口调试助手 · 重设计稿交互脚本（仅用于演示设计效果）
   语义：串口号 + 波特率 + 数据/停止/校验/流控 → 打开串口 →
        接收区按行显示设备数据（带时间戳），发送区支持文本/HEX
        编码、追加换行符与定时循环发送。
   说明：收发数据为前端模拟，不真正占用 COM 端口。
   ============================================================ */
(function () {
    "use strict";

    var state = {
        opened: "opened",           // opened | opening | closed
        view: "text",               // 接收区显示：text | hex
        enc: "text",                // 发送编码：text | hex
        eol: "none",                // 追加换行：none | crlf | lf
        paused: false,              // 接收区暂停显示
        timer: null,                // 定时发送
        rxTimer: null,              // 设备应答模拟
        cmdTimerKey: "",            // 当前处于定时发送的快捷指令
        records: [],
        txCount: 0,
        txBytes: 0,
        rxCount: 0,
        rxBytes: 0,
        startedAt: Date.now() - 751000,
        lastRxTime: "12:03:44.201"
    };

    /* 演示：设备应答池 */
    var RX_POOL = [
        { kind: "text", data: "\r\nOK\r\n" },
        { kind: "text", data: "WIFI CONNECTED" },
        { kind: "text", data: "WIFI GOT IP" },
        { kind: "text", data: "+IPD,13:hello world" },
        { kind: "text", data: "busy p..." },
        { kind: "text", data: "ERROR" },
        { kind: "hex", data: "01 03 02 00 2A 39 8C" }
    ];

    var el = {
        statePill: document.getElementById("statePill"),
        stateDot: document.getElementById("stateDot"),
        statePillText: document.getElementById("statePillText"),
        toolbarHint: document.getElementById("toolbarHint"),
        toolbarHintText: document.getElementById("toolbarHintText"),
        port: document.getElementById("port"),
        portWrap: document.getElementById("portWrap"),
        baudrate: document.getElementById("baudrate"),
        baudWrap: document.getElementById("baudWrap"),
        databits: document.getElementById("databits"),
        stopbits: document.getElementById("stopbits"),
        parity: document.getElementById("parity"),
        flow: document.getElementById("flow"),
        paramLock: document.getElementById("paramLock"),
        btnScan: document.getElementById("btnScan"),
        btnOpen: document.getElementById("btnOpen"),
        btnOpenText: document.getElementById("btnOpenText"),
        connTimer: document.getElementById("connTimer"),
        statTx: document.getElementById("statTx"),
        statTxBytes: document.getElementById("statTxBytes"),
        statRx: document.getElementById("statRx"),
        statRxBytes: document.getElementById("statRxBytes"),
        cmdCount: document.getElementById("cmdCount"),
        cmdList: document.getElementById("cmdList"),
        cmdEmpty: document.getElementById("cmdEmpty"),
        btnAddCmd: document.getElementById("btnAddCmd"),
        rxCount: document.getElementById("rxCount"),
        rxBody: document.getElementById("rxBody"),
        rxEmpty: document.getElementById("rxEmpty"),
        viewSeg: document.getElementById("viewSeg"),
        tsSwitch: document.getElementById("tsSwitch"),
        echoSwitch: document.getElementById("echoSwitch"),
        btnPause: document.getElementById("btnPause"),
        btnSave: document.getElementById("btnSave"),
        btnClear: document.getElementById("btnClear"),
        input: document.getElementById("input"),
        encSeg: document.getElementById("encSeg"),
        eol: document.getElementById("eol"),
        timerSwitch: document.getElementById("timerSwitch"),
        interval: document.getElementById("interval"),
        sendHint: document.getElementById("sendHint"),
        btnSend: document.getElementById("btnSend"),
        statusLeft: document.getElementById("statusLeft"),
        statusRight: document.getElementById("statusRight"),
        toast: document.getElementById("toast")
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

    function nowTime() {
        var d = new Date();
        return pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds()) +
            "." + String(d.getMilliseconds()).padStart(3, "0");
    }

    function textToBytes(text) {
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

    function bytesToUtf8(bytes) {
        var encoded = bytes.map(function (b) {
            return "%" + pad(b.toString(16));
        }).join("");
        try {
            return decodeURIComponent(encoded);
        } catch (e) {
            return bytes.map(function (b) {
                return b >= 32 && b < 127 ? String.fromCharCode(b) : ".";
            }).join("");
        }
    }

    function bytesToHexText(bytes) {
        return bytes.map(function (b) {
            return pad(b.toString(16).toUpperCase());
        }).join(" ");
    }

    function isHexString(str) {
        return /^([0-9a-fA-F]{2})(\s+[0-9a-fA-F]{2})*$/.test(String(str).trim());
    }

    function hexStringToBytes(str) {
        return String(str).trim().split(/\s+/).filter(Boolean).map(function (b) {
            return parseInt(b, 16) & 0xFF;
        });
    }

    function formatSize(bytes) {
        if (bytes < 1024) {
            return bytes + " B";
        }
        return (bytes / 1024).toFixed(1) + " KB";
    }

    /* 把不可见控制符显示成串口助手常见的符号，避免行内断行 */
    function visible(text) {
        return text.replace(/\r/g, "␍").replace(/\n/g, "␊");
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

    /* ---------------- 串口参数摘要 ---------------- */

    function parityShort() {
        var value = el.parity.value;
        return value === "奇校验" ? "O" : value === "偶校验" ? "E" : "N";
    }

    function paramSummary() {
        return el.port.value + " · " + el.baudrate.value + " " + el.databits.value +
            "-" + parityShort() + "-" + el.stopbits.value;
    }

    /* ---------------- 接收区 ---------------- */

    function record(dir, bytes, time) {
        state.records.push({ dir: dir, bytes: bytes, time: time || nowTime() });
    }

    function renderRx() {
        var list = state.records;
        var rxFrames = list.filter(function (r) {
            return r.dir === "rx";
        }).length;

        el.rxCount.textContent = rxFrames;
        el.rxBody.style.display = list.length === 0 ? "none" : "flex";
        el.rxEmpty.classList.toggle("show", list.length === 0);

        if (state.paused) {
            return;
        }

        el.rxBody.classList.toggle("no-time", !el.tsSwitch.checked);
        el.rxBody.innerHTML = list.map(function (r) {
            var arrow = r.dir === "tx" ? "→" : r.dir === "rx" ? "←" : "·";
            var data = state.view === "hex"
                ? bytesToHexText(r.bytes)
                : escapeHtml(visible(bytesToUtf8(r.bytes)));
            return '<div class="rx-line ' + r.dir + '">' +
                '<span class="rx-time">[' + r.time + ']</span>' +
                '<span class="rx-dir">' + arrow + '</span>' +
                '<span class="rx-data">' + (data || "&nbsp;") + '</span>' +
                '</div>';
        }).join("");

        el.rxBody.scrollTop = el.rxBody.scrollHeight;
    }

    function renderStats() {
        el.statTx.textContent = state.txCount;
        el.statTxBytes.textContent = formatSize(state.txBytes);
        el.statRx.textContent = state.rxCount;
        el.statRxBytes.textContent = formatSize(state.rxBytes);
        el.connTimer.textContent = state.opened === "opened" ? durationText() : "—";
        el.statusRight.textContent = "最后接收 " + state.lastRxTime + " · 接收区自动滚动";
    }

    /* ---------------- 打开 / 关闭串口 ---------------- */

    function setLocked(locked) {
        [el.port, el.baudrate, el.databits, el.stopbits, el.parity, el.flow].forEach(function (node) {
            node.disabled = locked;
        });
        [el.portWrap, el.baudWrap].forEach(function (node) {
            node.classList.toggle("locked", locked);
        });
        document.querySelectorAll(".param .select-wrap").forEach(function (node) {
            node.classList.toggle("locked", locked);
        });
        el.paramLock.textContent = locked ? "已锁定" : "可修改";
        el.paramLock.classList.toggle("on", locked);
    }

    function setOpened(status, keepStart) {
        state.opened = status;
        var pill = "state-pill";
        var dot = "dot";

        if (status === "opened") {
            pill += " on";
            dot += " on";
            el.statePillText.textContent = "已打开";
            el.btnOpenText.textContent = "关闭串口";
            el.btnOpen.disabled = false;
            setLocked(true);
            el.btnSend.disabled = false;
            if (!keepStart) {
                state.startedAt = Date.now();
            }
            setStatus("就绪 · " + el.port.value + " 已打开（" + el.baudrate.value + " · " +
                el.databits.value + "-" + parityShort() + "-" + el.stopbits.value + "）", "ok");
            startRxSim();
        } else if (status === "opening") {
            pill += " pending";
            dot += " pending";
            el.statePillText.textContent = "打开中";
            el.btnOpenText.textContent = "打开中…";
            el.btnOpen.disabled = true;
            el.btnSend.disabled = true;
            setStatus("正在打开 " + el.port.value + " …");
        } else {
            el.statePillText.textContent = "已关闭";
            el.btnOpenText.textContent = "打开串口";
            el.btnOpen.disabled = false;
            el.btnSend.disabled = true;
            setLocked(false);
            stopRxSim();
            setStatus("串口已关闭");
        }

        el.statePill.className = pill;
        el.stateDot.className = dot;
        renderStats();
    }

    /* ---------------- 发送 ---------------- */

    function eolBytes() {
        if (state.eol === "crlf") {
            return [0x0D, 0x0A];
        }
        if (state.eol === "lf") {
            return [0x0A];
        }
        return [];
    }

    function currentPayload() {
        var raw = el.input.value;
        if (!raw.trim()) {
            return { error: "发送内容不能为空" };
        }

        var bytes;
        if (state.enc === "hex") {
            if (!isHexString(raw)) {
                return { error: "HEX 格式错误，应为空格分隔的两位十六进制，如 01 03 00 00" };
            }
            bytes = hexStringToBytes(raw);
        } else {
            bytes = textToBytes(raw);
        }
        return { bytes: bytes.concat(eolBytes()) };
    }

    function send() {
        if (state.opened !== "opened") {
            showHint("串口未打开，无法发送数据");
            setStatus("串口未打开", "err");
            return;
        }

        var payload = currentPayload();
        if (payload.error) {
            showHint(payload.error);
            setStatus(payload.error, "err");
            return;
        }

        showHint("");

        if (el.echoSwitch.checked) {
            record("tx", payload.bytes);
        }
        state.txCount++;
        state.txBytes += payload.bytes.length;

        var preview = state.enc === "hex"
            ? bytesToHexText(payload.bytes)
            : visible(bytesToUtf8(payload.bytes));
        el.sendHint.textContent = "上次发送 " + preview + " · " + payload.bytes.length + " B";

        renderRx();
        renderStats();
        setStatus("已发送 " + payload.bytes.length + " B → " + el.port.value, "ok");

        /* 演示：设备处理一下后回一条应答 */
        setTimeout(function () {
            if (state.opened !== "opened") {
                return;
            }
            if (state.enc === "text" && !/^AT/.test(el.input.value.trim().toUpperCase())) {
                return;
            }
            appendRx({ kind: "text", data: "\r\nOK\r\n" });
        }, 320);
    }

    function appendRx(item) {
        var bytes = item.kind === "hex" ? hexStringToBytes(item.data) : textToBytes(item.data);
        record("rx", bytes);
        state.rxCount++;
        state.rxBytes += bytes.length;
        state.lastRxTime = nowTime();
        renderRx();
        renderStats();
    }

    /* ---------------- 设备应答模拟 ---------------- */

    function startRxSim() {
        if (state.rxTimer) {
            return;
        }
        state.rxTimer = setInterval(function () {
            if (state.opened !== "opened") {
                return;
            }
            var pick = RX_POOL[Math.floor(Math.random() * RX_POOL.length)];
            appendRx(pick);
            setStatus("接收 " + (pick.kind === "hex" ? pick.data : visible(pick.data).trim()) +
                " · 共 " + state.rxCount + " 帧");
        }, 3200);
    }

    function stopRxSim() {
        if (state.rxTimer) {
            clearInterval(state.rxTimer);
            state.rxTimer = null;
        }
    }

    /* ---------------- 快捷指令 ---------------- */

    function renderCommands() {
        var items = el.cmdList.querySelectorAll(".cmd-item");
        el.cmdCount.textContent = items.length;
        el.cmdList.style.display = items.length === 0 ? "none" : "flex";
        el.cmdEmpty.hidden = items.length !== 0;
    }

    function sendCommand(value) {
        el.input.value = value;
        /* HEX 形态的指令自动切到 HEX 编码，避免把 HEX 串当文本发出去 */
        if (isHexString(value)) {
            state.enc = "hex";
            el.encSeg.querySelectorAll("button").forEach(function (b) {
                b.classList.toggle("active", b.dataset.enc === "hex");
            });
        }
        send();
    }

    function stopTimerSend() {
        clearInterval(state.timer);
        state.timer = null;
        state.cmdTimerKey = "";
        el.timerSwitch.checked = false;
        el.interval.disabled = true;
        el.cmdList.querySelectorAll(".mini-btn.active").forEach(function (b) {
            b.classList.remove("active");
        });
    }

    /* ---------------- 事件绑定 ---------------- */

    el.btnOpen.addEventListener("click", function () {
        if (state.opened === "opened") {
            setOpened("closed");
            if (state.timer) {
                stopTimerSend();
            }
            record("sys", textToBytes("── " + el.port.value + " 已关闭 ──"));
            renderRx();
            toast("已关闭 " + el.port.value);
            return;
        }
        if (state.opened === "opening") {
            return;
        }

        setOpened("opening");
        setTimeout(function () {
            if (state.opened !== "opening") {
                return;
            }
            setOpened("opened");
            record("sys", textToBytes("── " + el.port.value + " 已打开 · " + paramSummary() + " ──"));
            renderRx();
            toast(el.port.value + " 已打开");
        }, 900);
    });

    el.btnScan.addEventListener("click", function () {
        toast("已扫描到 5 个可用串口（COM1 / COM3 / COM5 / COM7 / COM9）");
        setStatus("扫描串口完成：5 个设备");
    });

    [el.port, el.baudrate, el.databits, el.stopbits, el.parity, el.flow].forEach(function (node) {
        node.addEventListener("change", function () {
            setStatus("串口参数已更新：" + paramSummary());
        });
    });

    /* 显示格式 / 编码 / 换行 */
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
            callback(btn);
        });
    }

    bindSegmented(el.viewSeg, "view", renderRx);
    bindSegmented(el.encSeg, "enc", function () {
        el.input.placeholder = state.enc === "hex"
            ? "HEX 模式：空格分隔的两位十六进制，例如 01 03 00 00 00 01 84 0A"
            : "输入要发送的内容；HEX 模式下用空格分隔，例如 01 03 00 00 00 01 84 0A";
    });
    el.eol.addEventListener("change", function () {
        state.eol = el.eol.value;
        setStatus("追加换行符：" + (state.eol === "none" ? "无"
            : state.eol === "crlf" ? "\\r\\n" : "\\n"));
    });

    el.tsSwitch.addEventListener("change", function () {
        el.rxBody.classList.toggle("no-time", !el.tsSwitch.checked);
        setStatus(el.tsSwitch.checked ? "接收区显示时间戳" : "接收区隐藏时间戳");
    });

    el.echoSwitch.addEventListener("change", function () {
        setStatus(el.echoSwitch.checked ? "接收区已显示发送内容" : "接收区不再显示发送内容");
    });

    el.btnPause.addEventListener("click", function () {
        state.paused = !state.paused;
        el.btnPause.textContent = state.paused ? "继续" : "暂停";
        el.btnPause.classList.toggle("btn-danger", state.paused);
        if (state.paused) {
            setStatus("接收区已暂停显示，后台仍在接收");
        } else {
            renderRx();
            setStatus("接收区已恢复显示");
        }
    });

    el.btnClear.addEventListener("click", function () {
        state.records = [];
        renderRx();
        setStatus("已清空收发记录");
    });

    el.btnSave.addEventListener("click", function () {
        if (state.records.length === 0) {
            setStatus("没有需要保存的记录", "err");
            return;
        }
        setStatus("已保存 " + state.records.length + " 行记录到 serial-COM3-20261014.log", "ok");
        toast("串口记录已保存");
    });

    el.btnSend.addEventListener("click", send);

    el.input.addEventListener("keydown", function (e) {
        if (e.ctrlKey && e.key === "Enter") {
            e.preventDefault();
            send();
        }
    });

    el.input.addEventListener("input", function () {
        el.input.classList.remove("invalid");
        showHint("");
    });

    /* 定时发送 */
    el.timerSwitch.addEventListener("change", function () {
        if (el.timerSwitch.checked) {
            if (state.opened !== "opened") {
                el.timerSwitch.checked = false;
                showHint("请先打开串口，再开启定时发送");
                return;
            }
            if (!/^\d+$/.test(el.interval.value.trim()) || Number(el.interval.value) < 50) {
                el.timerSwitch.checked = false;
                showHint("定时间隔需为不小于 50 的整数（ms）");
                return;
            }
            el.interval.disabled = false;
            state.timer = setInterval(send, Number(el.interval.value));
            setStatus("定时发送已开启，间隔 " + el.interval.value + " ms", "ok");
        } else {
            stopTimerSend();
            setStatus("定时发送已停止");
        }
    });

    el.interval.addEventListener("input", function () {
        if (el.timerSwitch.checked && /^\d+$/.test(el.interval.value.trim())) {
            clearInterval(state.timer);
            state.timer = setInterval(send, Number(el.interval.value));
        }
    });

    /* 快捷指令 */
    el.cmdList.addEventListener("click", function (e) {
        var item = e.target.closest(".cmd-item");
        if (!item) {
            return;
        }
        var value = item.dataset.cmd;
        var mini = e.target.closest(".mini-btn");

        if (mini) {
            e.stopPropagation();
            var act = mini.dataset.act;

            if (act === "send") {
                sendCommand(value);
            } else if (act === "timer") {
                var key = value;
                var already = state.cmdTimerKey === key;
                stopTimerSend();
                if (!already) {
                    el.input.value = value;
                    if (isHexString(value)) {
                        state.enc = "hex";
                        el.encSeg.querySelectorAll("button").forEach(function (b) {
                            b.classList.toggle("active", b.dataset.enc === "hex");
                        });
                    }
                    el.timerSwitch.checked = true;
                    el.interval.disabled = false;
                    mini.classList.add("active");
                    state.cmdTimerKey = key;
                    state.timer = setInterval(send, Number(el.interval.value) || 1000);
                    toast("已对「" + (item.querySelector(".cmd-anno").textContent) + "」开启定时发送");
                }
            } else if (act === "edit") {
                toast("编辑快捷指令：" + value);
            } else {
                var anno = item.querySelector(".cmd-anno").textContent;
                if (state.cmdTimerKey === value) {
                    stopTimerSend();
                }
                item.remove();
                renderCommands();
                toast("已删除快捷指令「" + anno + "」");
            }
            return;
        }

        el.input.value = value;
        el.input.focus();
        toast("已填入发送区：" + value);
    });

    el.btnAddCmd.addEventListener("click", function () {
        toast("新增快捷指令（弹出编辑对话框）");
    });

    /* 演示状态切换 */
    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".demo-btn").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });

            var target = btn.dataset.status;

            if (target === "empty") {
                state.records = [];
                setOpened("opened", true);
                renderRx();
                setStatus("演示状态：空记录");
                return;
            }

            if (target === "opened") {
                state.startedAt = Date.now() - 751000;
                setOpened("opened", true);
                if (state.records.length === 0) {
                    seedRecords();
                    renderRx();
                }
                setStatus("演示状态：" + el.port.value + " 已打开（" + paramSummary() + "）", "ok");
            } else if (target === "opening") {
                setOpened("opening");
                setTimeout(function () {
                    if (state.opened !== "opening") {
                        return;
                    }
                    setOpened("opened");
                    toast("演示：" + el.port.value + " 打开完成");
                }, 1500);
            } else {
                stopTimerSend();
                setOpened("closed");
                setStatus("演示状态：串口已关闭");
            }
        });
    });

    /* ---------------- 初始化 ---------------- */

    function seedRecords() {
        var seed = [
            ["sys", "── COM3 已打开 · 115200 8-N-1 ──"],
            ["tx", "AT+RST"],
            ["rx", "\r\nOK\r\n"],
            ["rx", " ets Jan  8 2013,rst cause:2, boot mode:(3,6)"],
            ["rx", "load 0x40100000, len 2592, room 16"],
            ["rx", "tail 0"],
            ["rx", "chksum 0xf9"],
            ["rx", "ready"],
            ["tx", "AT+GMR"],
            ["rx", "AT version:1.7.4.0(May 11 2020 19:13:04)"],
            ["rx", "SDK version:3.0.4(9532ceb)"],
            ["rx", "compile time:May 27 2020 10:12:17"],
            ["rx", "OK"]
        ];
        var base = new Date(Date.now() - 60000);
        state.records = seed.map(function (row, i) {
            var d = new Date(base.getTime() + i * 140);
            var bytes = textToBytes(row[1]);
            var time = pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds()) +
                "." + String(d.getMilliseconds()).padStart(3, "0");
            if (row[0] === "rx") {
                state.rxCount++;
                state.rxBytes += bytes.length;
                state.lastRxTime = time;
            } else if (row[0] === "tx") {
                state.txCount++;
                state.txBytes += bytes.length;
            }
            return { dir: row[0], bytes: bytes, time: time };
        });
    }

    setInterval(function () {
        if (state.opened === "opened") {
            el.connTimer.textContent = durationText();
        }
    }, 1000);

    seedRecords();
    renderCommands();
    renderRx();
    renderStats();
    setLocked(true);
    el.btnSend.disabled = false;
    setOpened("opened", true);
    setStatus("就绪 · COM3 已打开（115200 · 8-N-1）");
})();
