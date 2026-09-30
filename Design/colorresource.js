/* ============================================================
   颜色值转换 · ColorResource —— 重设计稿交互（仅演示用）
   对应 ColorResourceViewModel：RedColor / GreenColor / BlueColor /
   AlphaValue / ColorHexValue / IsHexInputEnabled / ColorViewBrush /
   ColorResources（新增：Recents 最近使用）
   ============================================================ */
(function () {
    'use strict';

    var $ = function (id) {
        return document.getElementById(id);
    };

    /* ---------- 状态 ---------- */
    var state = {
        a: 255,
        r: 46,
        g: 124,
        b: 246,
        includeAlpha: true,   // 透明度勾选
        mode: 'rgb2hex',      // rgb2hex | hex2rgb
        query: '',
        recents: ['#16A34A', '#F2994A', '#7C5CFF', '#E5484D', '#22B8CF']
    };

    var DEFAULT = {a: 255, r: 46, g: 124, b: 246};

    var PALETTE = [
        {name: '主色蓝', hex: '#2E7CF6'},
        {name: '成功绿', hex: '#16A34A'},
        {name: '警告橙', hex: '#F2994A'},
        {name: '危险红', hex: '#E5484D'},
        {name: '主题紫', hex: '#7C5CFF'},
        {name: '青色', hex: '#22B8CF'},
        {name: '玫红', hex: '#E64980'},
        {name: '柠檬黄', hex: '#F5C518'},
        {name: 'Android 绿', hex: '#3DDC84'},
        {name: '微信绿', hex: '#07C160'},
        {name: '抖音红', hex: '#FE2C55'},
        {name: '哔哩粉', hex: '#FB7299'},
        {name: '淘宝橙', hex: '#FF5000'},
        {name: 'B 站蓝', hex: '#00A1D6'},
        {name: '深空黑', hex: '#1B1B1B'},
        {name: '次要文字', hex: '#5F6B7A'},
        {name: '占位文字', hex: '#93A0AE'},
        {name: '边框线', hex: '#DFE3EA'},
        {name: '页面底色', hex: '#EEF2F7'},
        {name: '窗口底色', hex: '#F3F9FE'},
        {name: '终端底色', hex: '#0C1016'},
        {name: '终端绿字', hex: '#00FF00'}
    ];

    /* ---------- 工具函数 ---------- */
    function pad2(n) {
        var v = Math.max(0, Math.min(255, n | 0)).toString(16).toUpperCase();
        return v.length < 2 ? '0' + v : v;
    }

    function clamp(n) {
        n = parseInt(n, 10);
        if (isNaN(n)) return 0;
        return Math.max(0, Math.min(255, n));
    }

    function currentHex() {
        var rgb = pad2(state.r) + pad2(state.g) + pad2(state.b);
        return state.includeAlpha ? '#' + pad2(state.a) + rgb : '#' + rgb;
    }

    function luminance(r, g, b) {
        return (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    }

    function normalizeHex(raw) {
        var v = (raw || '').replace('#', '').replace(/[^0-9a-fA-F]/g, '').toUpperCase();
        if (v.length === 3) v = v[0] + v[0] + v[1] + v[1] + v[2] + v[2];
        return v;
    }

    function applyHex(hex) {
        var v = normalizeHex(hex);
        if (v.length === 8) {
            state.a = parseInt(v.slice(0, 2), 16);
            state.r = parseInt(v.slice(2, 4), 16);
            state.g = parseInt(v.slice(4, 6), 16);
            state.b = parseInt(v.slice(6, 8), 16);
        } else if (v.length === 6) {
            state.r = parseInt(v.slice(0, 2), 16);
            state.g = parseInt(v.slice(2, 4), 16);
            state.b = parseInt(v.slice(4, 6), 16);
        } else {
            return false;
        }
        return true;
    }

    var toastTimer = null;

    function toast(msg) {
        var el = $('toast');
        el.textContent = msg;
        el.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            el.classList.remove('show');
        }, 1600);
    }

    function copyText(text, msg) {
        function fallback() {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            try {
                document.execCommand('copy');
            } catch (e) {
                /* ignore */
            }
            document.body.removeChild(ta);
        }

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text)['catch'](fallback);
        } else {
            fallback();
        }
        toast(msg || ('已复制 ' + text));
    }

    /* ---------- 最近使用 ---------- */
    function pushRecent(hex) {
        var v = hex.replace('#', '#').toUpperCase();
        state.recents = state.recents.filter(function (x) {
            return x.toUpperCase() !== v;
        });
        state.recents.unshift(v);
        state.recents = state.recents.slice(0, 6);
        renderRecents();
    }

    function renderRecents() {
        var box = $('recentList');
        box.innerHTML = '';
        if (!state.recents.length) {
            var empty = document.createElement('span');
            empty.className = 'recent-empty';
            empty.textContent = '暂无记录';
            box.appendChild(empty);
            return;
        }

        state.recents.forEach(function (hex) {
            var chip = document.createElement('button');
            chip.className = 'recent-chip';
            chip.title = '点击回填 ' + hex;

            var dot = document.createElement('span');
            dot.className = 'recent-dot';
            dot.style.background = hex;

            var text = document.createElement('span');
            text.textContent = hex;

            chip.appendChild(dot);
            chip.appendChild(text);
            chip.addEventListener('click', function () {
                applyHex(hex);
                render();
            });
            box.appendChild(chip);
        });
    }

    /* ---------- 色板 ---------- */
    function renderPalette() {
        var grid = $('paletteGrid');
        var q = state.query.trim().toUpperCase();
        var list = PALETTE.filter(function (item) {
            if (!q) return true;
            return item.name.toUpperCase().indexOf(q) >= 0 || item.hex.indexOf(q) >= 0;
        });

        grid.innerHTML = '';
        $('swatchCount').textContent = String(list.length);
        $('paletteEmpty').hidden = list.length > 0;

        list.forEach(function (item, index) {
            var btn = document.createElement('button');
            btn.className = 'swatch ' + (luminance(
                parseInt(item.hex.slice(1, 3), 16),
                parseInt(item.hex.slice(3, 5), 16),
                parseInt(item.hex.slice(5, 7), 16)) > 0.66 ? 'is-light' : 'is-dark');
            btn.style.background = item.hex;
            btn.title = item.name + ' · 点击复制并回填';
            if (index === 0) btn.classList.add('active');

            var name = document.createElement('span');
            name.className = 'swatch-name';
            name.textContent = item.name;

            var hex = document.createElement('span');
            hex.className = 'swatch-hex';
            hex.textContent = item.hex;

            var icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            icon.setAttribute('class', 'swatch-copy');
            icon.setAttribute('viewBox', '0 0 24 24');
            icon.setAttribute('width', '14');
            icon.setAttribute('height', '14');
            icon.innerHTML = '<path fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" ' +
                'stroke-linejoin="round" d="M9 9h10v11H9zM5 15H4V4h11v1"/>';

            btn.appendChild(name);
            btn.appendChild(hex);
            btn.appendChild(icon);

            btn.addEventListener('click', function () {
                applyHex(item.hex);
                render();
                pushRecent(item.hex);
                copyText(currentHex());
                var all = grid.querySelectorAll('.swatch');
                for (var i = 0; i < all.length; i++) all[i].classList.remove('active');
                btn.classList.add('active');
            });

            grid.appendChild(btn);
        });
    }

    /* ---------- 渲染 ---------- */
    var CH = ['a', 'r', 'g', 'b'];

    function render() {
        // 预览
        $('previewFill').style.background = 'rgba(' + state.r + ',' + state.g + ',' + state.b + ',' +
            (state.includeAlpha ? (state.a / 255).toFixed(3) : 1) + ')';
        $('previewBadge').textContent = state.includeAlpha ? ('A ' + state.a) : '不透明';

        // HEX 输入
        var hexInput = $('hexInput');
        if (state.mode === 'rgb2hex') {
            hexInput.value = currentHex().slice(1);
        }
        $('hexField').classList.toggle('readonly', state.mode === 'rgb2hex');

        // 通道
        var channelEditable = state.mode === 'rgb2hex';
        CH.forEach(function (key) {
            var row = document.querySelector('.channel.' + key);
            var range = $('range' + key.toUpperCase());
            var num = $('num' + key.toUpperCase());
            var value = state[key];

            if (range.value !== String(value)) range.value = value;
            if (document.activeElement !== num) num.value = value;
            range.style.setProperty('--p', (value / 255 * 100).toFixed(1) + '%');

            var disabled = !channelEditable || (key === 'a' && !state.includeAlpha);
            row.classList.toggle('disabled', disabled);
        });

        // 状态栏
        var modeText = state.mode === 'rgb2hex'
            ? 'RGB → HEX 模式 · 拖动通道实时同步'
            : 'HEX → RGB 模式 · 输入色值反解通道';
        $('statusLeft').textContent = modeText + (state.includeAlpha ? '' : ' · 不含透明通道');
        $('statusRight').textContent = '当前颜色 ' + currentHex();
    }

    /* ---------- 事件绑定 ---------- */
    function bindChannel(key) {
        var range = $('range' + key.toUpperCase());
        var num = $('num' + key.toUpperCase());

        range.addEventListener('input', function () {
            state[key] = clamp(range.value);
            render();
        });

        num.addEventListener('input', function () {
            var raw = num.value;
            if (raw === '') {
                state[key] = 0;
                render();
                return;
            }
            state[key] = clamp(raw);
            render();
        });

        num.addEventListener('blur', function () {
            num.value = state[key];
        });
    }

    function bindEvents() {
        CH.forEach(bindChannel);

        // HEX 输入（HEX → RGB 模式）
        $('hexInput').addEventListener('input', function () {
            if (state.mode !== 'hex2rgb') return;
            var el = this;
            if (applyHex(el.value)) {
                render();
                el.value = normalizeHex(el.value).slice(-(state.includeAlpha ? 8 : 6));
            }
        });

        // 模式切换
        var seg = $('modeSegment');
        seg.addEventListener('click', function (e) {
            var btn = e.target.closest('.seg-btn');
            if (!btn) return;
            var buttons = seg.querySelectorAll('.seg-btn');
            for (var i = 0; i < buttons.length; i++) buttons[i].classList.remove('active');
            btn.classList.add('active');
            state.mode = btn.getAttribute('data-mode');
            render();
            if (state.mode === 'hex2rgb') {
                $('hexInput').value = currentHex().slice(1);
                $('hexInput').focus();
                $('hexInput').select();
            }
        });

        // 透明通道
        $('alphaToggle').addEventListener('change', function () {
            state.includeAlpha = this.checked;
            if (state.mode === 'hex2rgb') $('hexInput').value = currentHex().slice(1);
            render();
        });

        // 复制
        function doCopy() {
            copyText(currentHex());
            applyHex(currentHex());
            pushRecent(currentHex());
        }

        $('btnCopyHex').addEventListener('click', doCopy);
        $('preview').addEventListener('click', doCopy);

        // 随机 / 重置
        $('btnRandom').addEventListener('click', function () {
            state.r = Math.floor(Math.random() * 256);
            state.g = Math.floor(Math.random() * 256);
            state.b = Math.floor(Math.random() * 256);
            state.a = state.includeAlpha ? Math.floor(Math.random() * 156) + 100 : 255;
            render();
        });

        $('btnReset').addEventListener('click', function () {
            state.a = DEFAULT.a;
            state.r = DEFAULT.r;
            state.g = DEFAULT.g;
            state.b = DEFAULT.b;
            $('hexInput').value = currentHex().slice(1);
            render();
            toast('已重置为默认颜色');
        });

        // 搜索
        $('swatchSearch').addEventListener('input', function () {
            state.query = this.value;
            renderPalette();
        });
    }

    /* ---------- 初始化 ---------- */
    renderPalette();
    renderRecents();
    render();
    bindEvents();
    document.body.classList.add('ready');
})();
