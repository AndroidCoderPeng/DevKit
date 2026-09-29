/* ============================================================
   网络信息 · NetConfiguration —— 重设计稿交互（仅演示用）
   对应 NetConfigurationViewModel：CommandItems / TargetAddress /
   IsLoopBoxChecked / OutputResult
   新增（设计稿建议）：Adapters + SelectedAdapter（网络适配器列表）
                       LocalIp / SubnetMask / Gateway / MacAddress / DnsServer
                       IsAutoScrollEnabled / IsRunning / Recents
   ============================================================ */
(function () {
    'use strict';

    var $ = function (id) {
        return document.getElementById(id);
    };

    /* ---------- 模拟数据 ---------- */
    var ADAPTERS = [
        {
            name: '以太网',
            ip: '192.168.3.126', mask: '255.255.255.0', gw: '192.168.3.1',
            mac: '8C-16-45-2A-7F-31', dns: '114.114.114.114',
            type: '有线 · 1000 Mbps', dhcp: '已启用'
        },
        {
            name: 'WLAN',
            ip: '192.168.3.88', mask: '255.255.255.0', gw: '192.168.3.1',
            mac: 'A4-7B-9D-31-2C-5E', dns: '223.5.5.5',
            type: '无线 · Wi-Fi 6 · 866 Mbps', dhcp: '已启用'
        },
        {
            name: 'VMware Network Adapter VMnet8',
            ip: '192.168.56.1', mask: '255.255.255.0', gw: '—',
            mac: '00-50-56-C0-00-08', dns: '—',
            type: '虚拟网卡 · 100 Mbps', dhcp: '已禁用'
        }
    ];

    var COMMANDS = [
        {id: 'ipconfig', label: 'ipconfig', desc: '网络配置', needTarget: false},
        {id: 'ipconfig-all', label: 'ipconfig /all', desc: '完整配置', needTarget: false},
        {id: 'ping', label: 'ping', desc: '连通性', needTarget: true, loop: true},
        {id: 'tracert', label: 'tracert', desc: '路由跟踪', needTarget: true},
        {id: 'netstat', label: 'netstat -an', desc: '端口状态', needTarget: false},
        {id: 'nslookup', label: 'nslookup', desc: '域名解析', needTarget: true},
        {id: 'arp', label: 'arp -a', desc: 'ARP 缓存', needTarget: false},
        {id: 'flushdns', label: 'ipconfig /flushdns', desc: '刷新 DNS', needTarget: false}
    ];

    var state = {
        adapter: ADAPTERS[0],
        command: COMMANDS[0],
        target: '8.8.8.8',
        loop: false,
        autoScroll: true
    };

    var run = {
        running: false,
        timers: [],
        pingRound: 0,
        pingTimes: [],
        lineNo: 3,
        startAt: 0
    };

    /* ---------- 通用工具 ---------- */
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
        toast(msg || '已复制');
    }

    function rand(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    function clearTimers() {
        run.timers.forEach(function (t) {
            clearTimeout(t);
        });
        run.timers = [];
    }

    function later(fn, delay) {
        var t = setTimeout(fn, delay);
        run.timers.push(t);
        return t;
    }

    /* ---------- 本机信息 ---------- */
    function renderAdapter() {
        var select = $('adapterSelect');
        select.innerHTML = '';
        ADAPTERS.forEach(function (item, index) {
            var opt = document.createElement('option');
            opt.value = String(index);
            opt.textContent = item.name + '  —  ' + item.ip;
            select.appendChild(opt);
        });
        select.value = String(ADAPTERS.indexOf(state.adapter));
        renderInfo();
    }

    function renderInfo() {
        var a = state.adapter;
        var map = {ip: a.ip, mask: a.mask, gw: a.gw, mac: a.mac, dns: a.dns, type: a.type, dhcp: a.dhcp};
        Object.keys(map).forEach(function (key) {
            var el = document.querySelector('[data-field="' + key + '"]');
            if (el) el.textContent = map[key];
        });
    }

    /* ---------- 命令列表 ---------- */
    function renderCommands() {
        var list = $('cmdList');
        list.innerHTML = '';
        $('cmdCount').textContent = String(COMMANDS.length);

        COMMANDS.forEach(function (cmd) {
            var btn = document.createElement('button');
            btn.className = 'cmd-item' + (cmd.needTarget ? ' need-target' : '') +
                (cmd.id === state.command.id ? ' active' : '');
            btn.type = 'button';

            var name = document.createElement('span');
            name.className = 'cmd-name';
            name.textContent = cmd.label;

            var desc = document.createElement('span');
            desc.className = 'cmd-desc';
            desc.textContent = cmd.desc;

            btn.appendChild(name);
            btn.appendChild(desc);
            btn.addEventListener('click', function () {
                selectCommand(cmd);
            });
            list.appendChild(btn);
        });
    }

    function selectCommand(cmd) {
        state.command = cmd;
        stopRun(true);

        var items = document.querySelectorAll('.cmd-item');
        for (var i = 0; i < items.length; i++) items[i].classList.remove('active');
        var index = COMMANDS.indexOf(cmd);
        if (items[index]) items[index].classList.add('active');

        $('targetInput').disabled = !cmd.needTarget;
        document.querySelector('.form-actions .topmost').classList.toggle('disabled', !cmd.loop);
        if (!cmd.loop) {
            state.loop = false;
            $('loopToggle').checked = false;
        }

        $('termTitle').textContent = 'cmd.exe — ' + cmd.label;
        $('footLeft').textContent = '已选择 ' + cmd.label + ' · 已就绪';
    }

    /* ---------- 终端输出 ---------- */
    function termBody() {
        return $('termBody');
    }

    function printLine(text, cls) {
        var line = document.createElement('div');
        line.className = 'term-line' + (cls ? ' ' + cls : '');
        line.textContent = text === '' ? ' ' : text;
        run.lineNo += 1;
        termBody().appendChild(line);
        updateStats();
        if (state.autoScroll) termBody().scrollTop = termBody().scrollHeight;
    }

    function printPrompt(text) {
        printLine('C:\\Users\\pengxh> ' + text, 'cmd');
    }

    function updateStats() {
        $('termStats').textContent = '共 ' + run.lineNo + ' 行';
    }

    function setRunning(active) {
        run.running = active;
        var dot = document.querySelector('#termState .dot');
        dot.className = 'dot ' + (active ? 'running' : 'idle');
        $('termState').innerHTML = '<span class="dot ' + (active ? 'running' : 'idle') + '"></span>' +
            (active ? '执行中' : '空闲');

        var btn = $('btnRun');
        btn.classList.toggle('btn-danger', active);
        btn.classList.toggle('btn-primary', !active);
        $('runText').textContent = active ? '停止' : '执行';
        $('footLeft').textContent = active ? '正在执行命令…' : 'cmd.exe · 已就绪';
    }

    /* ---------- 模拟命令输出 ---------- */
    function pingTime() {
        return rand(2, 26);
    }

    function buildPingRound(target) {
        run.pingRound += 1;
        var lines = [];
        if (run.pingRound === 1) {
            lines.push({t: '正在 Ping ' + target + ' 具有 32 字节的数据:', c: ''});
        }
        for (var i = 0; i < 4; i++) {
            var ms = pingTime();
            run.pingTimes.push(ms);
            lines.push({t: '来自 ' + target + ' 的回复: 字节=32 时间=' + ms + 'ms TTL=' + rand(52, 120), c: 'ok'});
        }
        return lines;
    }

    function pingSummary(target) {
        var total = run.pingTimes.length;
        var sent = total + (total % 4 === 0 ? 0 : 4 - total % 4);
        var min = Math.min.apply(null, run.pingTimes);
        var max = Math.max.apply(null, run.pingTimes);
        var avg = Math.round(run.pingTimes.reduce(function (a, b) {
            return a + b;
        }, 0) / total);
        return [
            {t: '', c: ''},
            {t: target + ' 的 Ping 统计信息:', c: ''},
            {t: '    数据包: 已发送 = ' + sent + '，已接收 = ' + total + '，丢失 = 0 (0% 丢失)，', c: ''},
            {t: '往返行程的估计时间(以毫秒为单位):', c: ''},
            {t: '    最短 = ' + min + 'ms，最长 = ' + max + 'ms，平均 = ' + avg + 'ms', c: 'ok'}
        ];
    }

    function buildLines(id, target) {
        var a = state.adapter;
        switch (id) {
            case 'ipconfig':
                return [
                    {t: '', c: ''},
                    {t: 'Windows IP 配置', c: ''},
                    {t: '', c: ''},
                    {t: '以太网适配器 ' + a.name + ':', c: 'hl'},
                    {t: '', c: ''},
                    {t: '   连接特定的 DNS 后缀 . . . . . . . : localdomain', c: ''},
                    {t: '   IPv4 地址 . . . . . . . . . . . . : ' + a.ip, c: 'ok'},
                    {t: '   子网掩码  . . . . . . . . . . . . : ' + a.mask, c: ''},
                    {t: '   默认网关. . . . . . . . . . . . . : ' + a.gw, c: ''},
                    {t: '', c: ''}
                ];

            case 'ipconfig-all':
                return [
                    {t: '', c: ''},
                    {t: 'Windows IP 配置', c: ''},
                    {t: '   主机名  . . . . . . . . . . . . . : pengxh-devkit', c: ''},
                    {t: '', c: ''},
                    {t: '以太网适配器 ' + a.name + ':', c: 'hl'},
                    {t: '', c: ''},
                    {t: '   描述. . . . . . . . . . . . . . . : Realtek PCIe GbE Family Controller', c: ''},
                    {t: '   物理地址. . . . . . . . . . . . . : ' + a.mac, c: 'ok'},
                    {t: '   DHCP 已启用 . . . . . . . . . . . : ' + a.dhcp, c: ''},
                    {t: '   自动配置已启用. . . . . . . . . . : 是', c: ''},
                    {t: '   IPv4 地址 . . . . . . . . . . . . : ' + a.ip + '(首选)', c: 'ok'},
                    {t: '   子网掩码  . . . . . . . . . . . . : ' + a.mask, c: ''},
                    {t: '   默认网关. . . . . . . . . . . . . : ' + a.gw, c: ''},
                    {t: '   DNS 服务器  . . . . . . . . . . . : ' + a.dns, c: 'ok'},
                    {t: '', c: ''}
                ];

            case 'tracert':
                var hops = [
                    '1     3 ms     2 ms     2 ms  ' + a.gw,
                    '2    12 ms    10 ms    11 ms  100.64.0.1',
                    '3    18 ms    17 ms    19 ms  221.13.28.1',
                    '4    26 ms    25 ms    28 ms  219.158.16.101',
                    '5    35 ms    33 ms    36 ms  219.158.5.10',
                    '6    42 ms    44 ms    41 ms  108.170.245.65',
                    '7    48 ms    47 ms    49 ms  8.8.8.8'
                ];
                var out = [
                    {t: '', c: ''},
                    {t: '通过最多 30 个跃点跟踪到 ' + target + ' 的路由', c: ''},
                    {t: '', c: ''}
                ];
                hops.forEach(function (h) {
                    out.push({t: '  ' + h, c: ''});
                });
                out.push({t: '', c: ''});
                out.push({t: '跟踪完成。', c: 'ok'});
                return out;

            case 'netstat':
                var rows = [
                    '  TCP    0.0.0.0:135            0.0.0.0:0              LISTENING',
                    '  TCP    0.0.0.0:445            0.0.0.0:0              LISTENING',
                    '  TCP    0.0.0.0:3306           0.0.0.0:0              LISTENING',
                    '  TCP    0.0.0.0:5037           0.0.0.0:0              LISTENING',
                    '  TCP    ' + a.ip + ':5555     0.0.0.0:0              LISTENING',
                    '  TCP    127.0.0.1:6379         0.0.0.0:0              LISTENING',
                    '  TCP    ' + a.ip + ':52314    110.242.68.66:443      ESTABLISHED',
                    '  TCP    ' + a.ip + ':52340    39.156.66.10:443       ESTABLISHED',
                    '  UDP    0.0.0.0:3702           *:*                    ',
                    '  UDP    0.0.0.0:5353           *:*                    '
                ];
                var net = [
                    {t: '', c: ''},
                    {t: '活动连接', c: 'hl'},
                    {t: '', c: ''},
                    {t: '  协议  本地地址              外部地址              状态', c: 'dim'}
                ];
                rows.forEach(function (r) {
                    net.push({t: r, c: /ESTABLISHED/.test(r) ? 'ok' : ''});
                });
                net.push({t: '', c: ''});
                net.push({t: '监听端口 6 个 · 已建立连接 2 个', c: 'warn'});
                return net;

            case 'nslookup':
                return [
                    {t: '', c: ''},
                    {t: '服务器:  public1.alidns.com', c: ''},
                    {t: 'Address:  ' + a.dns, c: 'ok'},
                    {t: '', c: ''},
                    {t: '非权威应答:', c: ''},
                    {t: '名称:    ' + target, c: 'hl'},
                    {t: 'Addresses:  ' + rand(100, 140) + '.242.68.66', c: 'ok'},
                    {t: '            ' + rand(30, 50) + '.156.66.10', c: 'ok'},
                    {t: '', c: ''}
                ];

            case 'arp':
                var arp = [
                    {t: '', c: ''},
                    {t: '接口: ' + a.ip + ' --- 0xb', c: ''},
                    {t: '  Internet 地址         物理地址              类型', c: 'dim'},
                    {t: '  ' + a.gw + '           ' + a.mac + '     动态', c: ''},
                    {t: '  192.168.3.102          B0-4E-26-11-9A-77     动态', c: ''},
                    {t: '  192.168.3.118          D8-3A-DD-6C-42-10     动态', c: ''},
                    {t: '  192.168.3.255          FF-FF-FF-FF-FF-FF     静态', c: ''},
                    {t: '  224.0.0.22             01-00-5E-00-00-16     静态', c: ''},
                    {t: '', c: ''}
                ];
                return arp;

            case 'flushdns':
                return [
                    {t: '', c: ''},
                    {t: 'Windows IP 配置', c: ''},
                    {t: '', c: ''},
                    {t: '已成功刷新 DNS 解析缓存。', c: 'ok'},
                    {t: '', c: ''}
                ];

            default:
                return [{t: '未知命令', c: 'err'}];
        }
    }

    /* ---------- 顺序打印 ---------- */
    function printSequence(lines, done) {
        var index = 0;

        function step() {
            if (!run.running) return;
            if (index >= lines.length) {
                if (done) done();
                return;
            }
            var item = lines[index++];
            printLine(item.t, item.c);
            later(step, lines.length > 24 ? 26 : 55);
        }

        step();
    }

    function currentTarget() {
        var value = $('targetInput').value.trim();
        return value || '8.8.8.8';
    }

    function commandLine() {
        var cmd = state.command;
        if (cmd.id === 'ping') return 'ping ' + currentTarget() + (state.loop ? ' -t' : '');
        if (cmd.needTarget) return cmd.label + ' ' + currentTarget();
        return cmd.label;
    }

    function startRun() {
        if (state.command.needTarget && !$('targetInput').value.trim()) {
            toast('请先输入目标地址');
            $('targetInput').focus();
            return;
        }

        run.pingRound = 0;
        run.pingTimes = [];
        run.startAt = Date.now();
        setRunning(true);
        printPrompt(commandLine());

        var cmd = state.command;

        if (cmd.id === 'ping') {
            runPingRound(currentTarget());
            return;
        }

        printSequence(buildLines(cmd.id, currentTarget()), function () {
            finishRun();
        });
    }

    function runPingRound(target) {
        var lines = buildPingRound(target);
        printSequence(lines, function () {
            if (!run.running) return;
            if (state.loop) {
                later(function () {
                    runPingRound(target);
                }, 1100);
            } else {
                printSequence(pingSummary(target), finishRun);
            }
        });
    }

    function finishRun() {
        var cost = Date.now() - run.startAt;
        setRunning(false);
        $('footRight').textContent = '执行完成 · 耗时 ' + cost + ' ms';
        if (run.pingTimes.length) {
            var times = run.pingTimes;
            var avg = Math.round(times.reduce(function (a, b) {
                return a + b;
            }, 0) / times.length);
            $('termStats').textContent = '共 ' + run.lineNo + ' 行 · 已发送 ' + times.length +
                ' · 平均 ' + avg + 'ms';
        }
    }

    function stopRun(silent) {
        if (!run.running) {
            if (!silent) return;
            clearTimers();
            return;
        }
        clearTimers();
        setRunning(false);
        printLine('^C', 'warn');
        printLine('已终止当前命令。', 'dim');
        if (!silent) $('footRight').textContent = '用户已终止命令';
        if (state.loop) $('termStats').textContent = '共 ' + run.lineNo + ' 行 · 已停止';
    }

    /* ---------- 事件绑定 ---------- */
    function bindEvents() {
        $('adapterSelect').addEventListener('change', function () {
            state.adapter = ADAPTERS[parseInt(this.value, 10)];
            renderInfo();
            $('footRight').textContent = '已切换到 ' + state.adapter.name;
        });

        $('targetInput').addEventListener('input', function () {
            state.target = this.value;
        });

        $('loopToggle').addEventListener('change', function () {
            state.loop = this.checked;
        });

        $('autoScrollToggle').addEventListener('change', function () {
            state.autoScroll = this.checked;
        });

        $('btnRun').addEventListener('click', function () {
            if (run.running) {
                stopRun(false);
            } else {
                startRun();
            }
        });

        $('btnClear').addEventListener('click', function () {
            clearTimers();
            setRunning(false);
            termBody().innerHTML = '';
            run.lineNo = 0;
            run.pingTimes = [];
            updateStats();
            printLine('输出已清空。', 'dim');
            $('footRight').textContent = '输出已清空';
        });

        $('btnCopyOut').addEventListener('click', function () {
            var text = termBody().innerText.replace(/\u00a0/g, ' ');
            if (!text.trim()) {
                toast('暂无可复制的输出');
                return;
            }
            copyText(text, '输出已复制到剪贴板');
        });

        $('btnRefresh').addEventListener('click', function () {
            var btn = this;
            btn.disabled = true;
            $('footRight').textContent = '正在读取网络适配器…';
            setTimeout(function () {
                btn.disabled = false;
                $('footRight').textContent = '网络信息已刷新';
                toast('网络信息已刷新');
            }, 600);
        });

        $('btnCopyInfo').addEventListener('click', function () {
            var a = state.adapter;
            var text = [
                '适配器: ' + a.name,
                'IPv4 地址: ' + a.ip,
                '子网掩码: ' + a.mask,
                '默认网关: ' + a.gw,
                '物理地址: ' + a.mac,
                'DNS 服务器: ' + a.dns,
                '适配器类型: ' + a.type,
                'DHCP: ' + a.dhcp,
                '连接状态: 已连接'
            ].join('\r\n');
            copyText(text, '本机网络信息已复制');
        });

        var infoGrid = $('infoGrid');
        infoGrid.addEventListener('click', function (e) {
            var item = e.target.closest('.info-item.copyable');
            if (!item) return;
            var value = item.querySelector('.info-value').textContent.trim();
            copyText(value, '已复制 ' + value);
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && (e.target.id === 'targetInput' || e.target.id === 'adapterSelect')) {
                if (!run.running) startRun();
            }
        });
    }

    /* ---------- 初始化 ---------- */
    renderAdapter();
    renderCommands();
    selectCommand(COMMANDS[2]);   // 默认选中 ping
    bindEvents();
    updateStats();
})();
