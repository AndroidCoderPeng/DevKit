/* ============================================================
   指令脚本对话框 · 设计稿演示脚本（仅静态页演示用）
   ============================================================ */

(function () {
    "use strict";

    var libList = document.getElementById("libList");
    var libCount = document.getElementById("libCount");
    var libEmpty = document.getElementById("libEmpty");
    var queueList = document.getElementById("queueList");
    var queueCount = document.getElementById("queueCount");
    var queueEmpty = document.getElementById("queueEmpty");
    var intervalInput = document.getElementById("interval");
    var etaValue = document.getElementById("etaValue");
    var btnOk = document.getElementById("btnOk");

    /* ---------- 演示数据 ---------- */
    var library = [
        { value: "AA 55 01 01", memo: "握手请求" },
        { value: "AA 55 02 00", memo: "查询设备状态" },
        { value: "AA 55 03 01 FF", memo: "开启采集" },
        { value: "AA 55 03 00 00", memo: "停止采集" }
    ];

    // 队列初始预填两条，方便直接看到效果
    var queue = [library[0], library[2]];

    var dragIndex = -1;
    var dragFromLib = false;

    /* ---------- 渲染 ---------- */
    function render() {
        renderLibrary();
        renderQueue();
        renderMeta();
    }

    function renderLibrary() {
        libList.innerHTML = "";
        libCount.textContent = library.length;

        library.forEach(function (cmd) {
            var row = document.createElement("div");
            row.className = "lib-item";

            var text = document.createElement("span");
            text.className = "lib-text";
            text.innerHTML =
                '<span class="lib-value"></span><span class="lib-memo"></span>';
            text.querySelector(".lib-value").textContent = cmd.value;
            text.querySelector(".lib-memo").textContent = cmd.memo || "（无备注）";

            row.appendChild(text);
            libList.appendChild(row);

            // 双击加入执行队列
            row.addEventListener("dblclick", function () {
                queue.push(cmd);
                render();
            });

            // 也可以直接拖入右侧队列
            row.draggable = true;
            row.addEventListener("dragstart", function () {
                dragIndex = library.indexOf(cmd);
                dragFromLib = true;
            });
            row.addEventListener("dragend", function () {
                dragIndex = -1;
                dragFromLib = false;
            });
        });

        libEmpty.hidden = library.length > 0;
        libList.style.display = library.length ? "flex" : "none";
    }

    function renderQueue() {
        queueList.innerHTML = "";
        queueCount.textContent = queue.length;

        queue.forEach(function (cmd, index) {
            var row = document.createElement("div");
            row.className = "queue-item";
            row.draggable = true;
            row.dataset.index = index;

            var grip = document.createElement("span");
            grip.className = "queue-grip";
            grip.textContent = "⋮⋮";
            grip.title = "拖动调整顺序";

            var badge = document.createElement("span");
            badge.className = "queue-index";
            badge.textContent = index + 1;

            var text = document.createElement("span");
            text.className = "queue-text";
            text.innerHTML =
                '<span class="queue-value"></span><span class="queue-memo"></span>';
            text.querySelector(".queue-value").textContent = cmd.value;
            text.querySelector(".queue-memo").textContent = cmd.memo || "（无备注）";

            var remove = document.createElement("button");
            remove.className = "queue-remove";
            remove.title = "从队列移除";
            remove.innerHTML =
                '<svg viewBox="0 0 24 24" width="13" height="13" aria-hidden="true">' +
                '<path fill="none" stroke="currentColor" stroke-width="2" ' +
                'stroke-linecap="round" stroke-linejoin="round" d="M6 6l12 12M18 6L6 18"/></svg>';
            remove.addEventListener("click", function () {
                queue.splice(index, 1);
                render();
            });

            row.appendChild(grip);
            row.appendChild(badge);
            row.appendChild(text);
            row.appendChild(remove);

            /* ---------- HTML5 拖拽排序 ---------- */
            row.addEventListener("dragstart", function () {
                dragIndex = index;
                row.classList.add("dragging");
            });
            row.addEventListener("dragend", function () {
                dragIndex = -1;
                render();
            });
            row.addEventListener("dragover", function (e) {
                e.preventDefault();
                row.classList.add("drag-over");
            });
            row.addEventListener("dragleave", function () {
                row.classList.remove("drag-over");
            });
            row.addEventListener("drop", function (e) {
                e.preventDefault();
                if (dragIndex < 0) return;
                if (dragFromLib) {
                    // 从指令库拖入：插入到该位置
                    queue.splice(index, 0, library[dragIndex]);
                } else if (dragIndex !== index) {
                    // 队列内部拖动：调整顺序
                    var moved = queue.splice(dragIndex, 1)[0];
                    queue.splice(index, 0, moved);
                }
                dragFromLib = false;
                render();
            });

            queueList.appendChild(row);
        });

        queueEmpty.style.display = queue.length ? "none" : "flex";
    }

    // 空队列时也允许直接拖到队列区域
    queueList.addEventListener("dragover", function (e) {
        e.preventDefault();
    });
    queueList.addEventListener("drop", function (e) {
        e.preventDefault();
        if (dragIndex < 0 || !dragFromLib) return;
        queue.push(library[dragIndex]);
        dragIndex = -1;
        dragFromLib = false;
        render();
    });

    /* ---------- 执行间隔校验 + 预计总耗时 ---------- */
    function renderMeta() {
        var raw = intervalInput.value.trim();
        var ms = Number(raw);
        var valid = raw !== "" && /^\d+$/.test(raw) && ms > 0;
        intervalInput.classList.toggle("invalid", raw !== "" && !valid);

        var seconds = queue.length * (valid ? ms : 0) / 1000;
        etaValue.textContent = seconds >= 1
            ? seconds.toFixed(1) + " s"
            : (seconds * 1000).toFixed(0) + " ms";

        // 队列为空或间隔非法时禁用确定
        btnOk.disabled = queue.length === 0 || !valid;
    }

    intervalInput.addEventListener("input", renderMeta);

    /* ---------- 清空队列 ---------- */
    document.getElementById("btnClearQueue").addEventListener("click", function () {
        queue = [];
        render();
    });

    /* ---------- 按钮占位（静态稿仅提示，不做真实逻辑） ---------- */
    btnOk.addEventListener("click", function () {
        if (btnOk.disabled) return;
        console.log("确定：", queue.map(function (c) { return c.value; }),
            "间隔", intervalInput.value, "ms");
    });
    document.getElementById("btnCancel").addEventListener("click", function () {
        console.log("取消");
    });

    /* ---------- 演示状态切换 ---------- */
    var demo = {
        // 正常：队列预填两条
        "normal": function () {
            queue = [library[0], library[2]];
        },
        // 空队列：禁用确定按钮
        "empty": function () {
            queue = [];
        }
    };

    document.querySelectorAll(".demo-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".demo-btn").forEach(function (b) {
                b.classList.toggle("active", b === btn);
            });
            (demo[btn.dataset.status] || demo.normal)();
            render();
        });
    });

    render();
})();
