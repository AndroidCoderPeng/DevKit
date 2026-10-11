/* ============================================================
   扩展指令对话框 · 设计稿演示脚本（仅静态页演示用）
   ============================================================ */

(function () {
    "use strict";

    var cmdInput = document.getElementById("cmdValue");
    var annoInput = document.getElementById("annoValue");
    var cmdWrap = document.getElementById("cmdWrap");
    var encSeg = document.getElementById("encSeg");
    var cmdTip = document.getElementById("cmdTip");
    var byteChip = document.getElementById("byteChip");
    var byteStrip = document.getElementById("byteStrip");
    var btnFormat = document.getElementById("btnFormat");
    var hint = document.getElementById("hint");
    var hintText = document.getElementById("hintText");
    var btnSave = document.getElementById("btnSave");

    var isValid = true;
    var enc = "hex"; // 当前发送编码：hex | ascii
    var hexRe = /^[0-9a-fA-F]{1,2}$/;

    var ENC_META = {
        "hex": {
            tip: "HEX 字节，空格分隔",
            placeholder: "如 aa550101，点格式化 → AA 55 01 01"
        },
        "ascii": {
            tip: "ASCII 文本，原样发送",
            placeholder: "例如：AT+RST\r\n"
        }
    };

    /* ---------- 校验：按编码区分规则（空值不报错，仅拦截格式错误） ---------- */
    function validate(value) {
        var v = value.trim();
        if (!v) return "";

        // ASCII：文本原样发送，即合法
        if (enc === "ascii") return "";

        // HEX：仅允许十六进制字节，空格分隔
        var tokens = v.split(/\s+/);
        for (var i = 0; i < tokens.length; i++) {
            if (!hexRe.test(tokens[i])) {
                return "指令值格式不正确：仅支持十六进制字节（00 - FF），以空格分隔";
            }
        }
        return "";
    }

    /* ---------- 字节可视化条 ----------
       HEX：每字节一个小块，非法字节标红；
       ASCII：每字符一个小块，空格用灰点表示。 */
    function renderByteStrip(tokens) {
        byteStrip.innerHTML = "";
        if (!tokens.length) {
            byteStrip.classList.remove("show");
            return;
        }
        byteStrip.classList.add("show");

        var frag = document.createDocumentFragment();
        tokens.forEach(function (t, i) {
            var cell = document.createElement("span");
            if (enc === "hex") {
                cell.className = "byte-cell" + (hexRe.test(t) ? "" : " bad");
                cell.textContent = t.toUpperCase();
                cell.title = "第 " + (i + 1) + " 字节";
            } else {
                var isSpace = t === " ";
                cell.className = "byte-cell" + (isSpace ? " sp" : "");
                cell.textContent = isSpace ? "·" : t;
                cell.title = isSpace ? "空格" : "字符";
            }
            frag.appendChild(cell);
        });
        byteStrip.appendChild(frag);
    }

    /* ---------- 更新字节数 / 字节条 / 校验 ---------- */
    function refresh() {
        var v = cmdInput.value;
        var count = enc === "hex"
            ? (v.trim() ? v.trim().split(/\s+/).length : 0)
            : v.length;

        // 字节数（ASCII 按 UTF-8 字节数近似展示）
        if (enc === "ascii") {
            var bytes = 0;
            for (var i = 0; i < v.length; i++) {
                var c = v.charCodeAt(i);
                bytes += c < 0x80 ? 1 : (c < 0x800 ? 2 : (c >= 0xd800 && c <= 0xdbff ? 0 : 2));
                if (c >= 0xd800 && c <= 0xdbff && i + 1 < v.length) {
                    var c2 = v.charCodeAt(i + 1);
                    if (c2 >= 0xdc00 && c2 <= 0xdfff) { bytes += 2; i++; }
                }
            }
            byteChip.textContent = bytes + " B";
        } else {
            byteChip.textContent = count + " B";
        }

        // 字节可视化
        var tokens = enc === "hex"
            ? (v.trim() ? v.trim().split(/\s+/) : [])
            : v.split("");
        renderByteStrip(tokens);

        // 校验（空值只禁用保存，不弹错误提示）
        var msg = validate(cmdInput.value);
        isValid = !msg;
        cmdWrap.classList.toggle("invalid", !!msg && v.trim() !== "");
        hint.classList.toggle("show", !!msg);
        hintText.textContent = msg;
        btnSave.disabled = !isValid || v.trim() === "";
    }

    /* ---------- 编码切换：联动提示、placeholder、格式化按钮 ---------- */
    function setEnc(mode) {
        if (enc === mode) return;
        enc = mode;

        encSeg.querySelectorAll("button").forEach(function (b) {
            b.classList.toggle("active", b.dataset.enc === enc);
        });

        var meta = ENC_META[enc];
        cmdTip.textContent = meta.tip;
        cmdInput.placeholder = meta.placeholder;

        // 格式化只对 HEX 有意义，ASCII 模式下隐藏
        btnFormat.hidden = enc === "ascii";

        refresh();
    }

    /* ---------- 格式化（仅 HEX）：大写 + 规范空格分隔 ---------- */
    function format() {
        if (enc !== "hex") return;
        var tokens = cmdInput.value.trim().split(/\s+/).filter(Boolean);
        var fixed = [];
        tokens.forEach(function (t) {
            t = t.toUpperCase();
            if (t.length === 1) t = "0" + t; // 补齐两位
            fixed.push(t);
        });
        cmdInput.value = fixed.join(" ");
        refresh();
    }

    encSeg.addEventListener("click", function (e) {
        var btn = e.target.closest("button");
        if (btn) setEnc(btn.dataset.enc);
    });

    cmdInput.addEventListener("input", refresh);
    btnFormat.addEventListener("click", format);

    /* ---------- 按钮占位（静态稿仅提示，不做真实逻辑） ---------- */
    btnSave.addEventListener("click", function () {
        if (!isValid) return;
        console.log("保存：[" + enc + "]", cmdInput.value, annoInput.value);
    });
    document.getElementById("btnCancel").addEventListener("click", function () {
        console.log("取消");
    });

    /* ---------- 演示状态切换 ---------- */
    var demoTitle = document.querySelector("#demoTitlebar .title");

    var demo = {
        // 编辑模式：预填一条 HEX 指令
        "edit": function () {
            demoTitle.textContent = "编辑扩展指令";
            setEncForce("hex");
            cmdInput.value = "AA 55 03 01 FF";
            annoInput.value = "开启采集";
        },
        // 新增模式：空表单
        "add": function () {
            demoTitle.textContent = "添加扩展指令";
            setEncForce("hex");
            cmdInput.value = "";
            annoInput.value = "";
        },
        // 校验错误：填入非法 HEX
        "invalid": function () {
            demoTitle.textContent = "编辑扩展指令";
            setEncForce("hex");
            cmdInput.value = "AA 55 XY ZZ";
            annoInput.value = "";
        }
    };

    // 演示切换需要强制设置编码（忽略相同值判断）
    function setEncForce(mode) {
        enc = null;
        setEnc(mode);
    }

    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".demo-btn").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });
            (demo[btn.dataset.status] || demo.edit)();
            refresh();
        });
    });

    // 初始态与演示切换器激活项（新增）保持一致
    demo.add();
    refresh();
})();
