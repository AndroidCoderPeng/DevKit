/* ============================================================
   扩展指令对话框 · 设计稿演示脚本（仅静态页演示用）
   ============================================================ */

(function () {
    "use strict";

    var cmdInput = document.getElementById("cmdValue");
    var annoInput = document.getElementById("annoValue");
    var cmdWrap = document.getElementById("cmdWrap");
    var byteChip = document.getElementById("byteChip");
    var byteStrip = document.getElementById("byteStrip");
    var hint = document.getElementById("hint");
    var hintText = document.getElementById("hintText");
    var btnSave = document.getElementById("btnSave");

    var isValid = true;
    var hexRe = /^[0-9a-fA-F]{1,2}$/;

    /* ---------- 校验：仅允许十六进制字节，空格分隔 ---------- */
    function validate(value) {
        var v = value.trim();
        if (!v) return "指令值不能为空";
        var tokens = v.split(/\s+/);
        for (var i = 0; i < tokens.length; i++) {
            if (!hexRe.test(tokens[i])) {
                return "指令值格式不正确：仅支持十六进制字节（00 - FF），以空格分隔";
            }
        }
        return "";
    }

    /* ---------- 字节可视化条：每字节一个小块，非法字节标红 ---------- */
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
            cell.className = "byte-cell" + (hexRe.test(t) ? "" : " bad");
            cell.textContent = t.toUpperCase();
            cell.title = "第 " + (i + 1) + " 字节";
            frag.appendChild(cell);
        });
        byteStrip.appendChild(frag);
    }

    /* ---------- 更新字节数 / 字节条 / 校验 ---------- */
    function refresh() {
        var v = cmdInput.value.trim();
        var tokens = v ? v.split(/\s+/) : [];

        // 字节数
        byteChip.textContent = tokens.length + " B";

        // 字节可视化
        renderByteStrip(tokens);

        // 校验
        var msg = validate(cmdInput.value);
        isValid = !msg;
        cmdWrap.classList.toggle("invalid", !!msg && v !== "");
        hint.classList.toggle("show", !!msg);
        hintText.textContent = msg;
        btnSave.disabled = !isValid;
    }

    /* ---------- 格式化：大写 + 规范空格分隔 ---------- */
    function format() {
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

    cmdInput.addEventListener("input", refresh);
    document.getElementById("btnFormat").addEventListener("click", format);

    /* ---------- 按钮占位（静态稿仅提示，不做真实逻辑） ---------- */
    document.getElementById("btnSave").addEventListener("click", function () {
        if (!isValid) return;
        console.log("保存：", cmdInput.value, annoInput.value);
    });
    document.getElementById("btnCancel").addEventListener("click", function () {
        console.log("取消");
    });

    /* ---------- 演示状态切换 ---------- */
    var demoTitle = document.querySelector("#demoTitlebar .title");

    var demo = {
        // 编辑模式：预填一条已有指令
        "edit": function () {
            demoTitle.textContent = "编辑扩展指令";
            cmdInput.value = "AA 55 03 01 FF";
            annoInput.value = "开启采集";
        },
        // 新增模式：空表单
        "add": function () {
            demoTitle.textContent = "添加扩展指令";
            cmdInput.value = "";
            annoInput.value = "";
        },
        // 校验错误：填入非法内容
        "invalid": function () {
            demoTitle.textContent = "编辑扩展指令";
            cmdInput.value = "AA 55 XY ZZ";
            annoInput.value = "";
        }
    };

    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".demo-btn").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });
            (demo[btn.dataset.status] || demo.edit)();
            refresh();
        });
    });

    refresh();
})();
