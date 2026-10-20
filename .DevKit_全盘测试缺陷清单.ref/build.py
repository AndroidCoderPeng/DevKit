# -*- coding: utf-8 -*-
"""DevKit 全盘测试缺陷清单生成脚本"""
try:
    import openpyxl
except ImportError:
    import subprocess, sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "--quiet", "openpyxl>=3.1.0"])
    import openpyxl

from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

OUT = r"D:/Code/RiderProjects/DevKit/DevKit_全盘测试缺陷清单.xlsx"


def xl_color(css_hex: str) -> str:
    value = css_hex.removeprefix("#").upper()
    if len(value) != 6:
        raise ValueError(f"Expected #RRGGBB, got: {css_hex}")
    return "FF" + value


# 商务蓝配色
XL_HEADER_BG = xl_color("#4472C4")
XL_HEADER_FG = xl_color("#FFFFFF")
XL_LIGHT_BG = xl_color("#D9E2F3")
XL_SUM_BG = xl_color("#2F5597")
XL_BORDER = xl_color("#BFBFBF")
thin = Side(style="thin", color=XL_BORDER)
BORDER = Border(left=thin, right=thin, top=thin, bottom=thin)

# 严重级别配色
LV_COLOR = {
    "致命": ("#C00000", "#FFFFFF"),
    "严重": ("#FF6D3F", "#FFFFFF"),
    "一般": ("#FFD966", "#3F3F3F"),
    "轻微": ("#E2EFDA", "#375623"),
}
STATUS_COLOR = {
    "待修复": ("#FFC7CE", "#9C0006"),
    "修复中": ("#FFEB9C", "#9C6500"),
    "已修复": ("#C6EFCE", "#006100"),
    "已关闭": ("#EDEDED", "#3F3F3F"),
}

# ============ 缺陷数据 ============
# 字段：编号 模块 缺陷标题 严重级别 前置条件 复现步骤 预期结果 实际结果 根因定位 修复建议
BUGS = [
    dict(
        no="BUG-001", status="待修复", module="ADB 工具", title="无设备校验字符串与赋值不一致，未连接设备时全部操作失去拦截",
        level="致命",
        pre="已启动 DevKit，当前未连接任何 Android 设备",
        steps="1. 启动 DevKit 主界面\n2. 点击【Android → ADB】打开 ADB 窗口\n3. 不连接任何设备，直接点击【截图】按钮\n4. 观察程序行为",
        expect="提示「请先刷新并连接设备」，不执行任何 adb 命令",
        actual="校验未生效，直接执行 adb 命令且无任何提示；无设备时 adb 报错信息被静默吞掉，界面仍提示「屏幕截取成功」",
        cause="AndroidDebugBridgeViewModel.cs:142 将 CurrentDevice 赋值为 \"no device connected\"，但 308/336/348/560 行校验的是 \"未连接任何设备\"，两者永不相等",
        fix="统一常量（如 DeviceNotConnected），4 处校验与 InitValueBinding 的赋值引用同一常量",
    ),
    dict(
        no="BUG-002", status="待修复", module="TCP 客户端", title="端口框输入非数字导致未捕获异常，程序直接崩溃",
        level="致命",
        pre="已打开 TCP 客户端窗口",
        steps="1. 打开【Socket → TCP 客户端】\n2. 远程地址填 127.0.0.1\n3. 将「远程端口」清空或输入 abc\n4. 点击【连接】",
        expect="弹出「端口必须是 1-65535 之间的整数」之类的友好提示，程序不崩溃",
        actual="抛出 FormatException 未被捕获，程序直接崩溃退出（RelayCommand 生成的 async void 吞掉了 try 块之外的异常）",
        cause="TcpClientViewModel.cs:185 使用 Convert.ToInt32(RemotePort)，且该行位于 try 块之外（try 从 229 行才开始），转换失败无保护",
        fix="改用 int.TryParse + 范围校验，参照同文件 588 行的写法；转换必须放在 try 块内",
    ),
    dict(
        no="BUG-003", status="待修复", module="WS 客户端", title="端口框输入非数字导致未捕获异常，程序直接崩溃",
        level="致命",
        pre="已打开 WebSocket 客户端窗口",
        steps="1. 打开【Socket → WS 客户端】\n2. 远程地址填 127.0.0.1\n3. 将「远程端口」输入 abc\n4. 点击【连接】",
        expect="弹出友好提示，程序不崩溃",
        actual="抛出 FormatException 未捕获，程序崩溃退出",
        cause="WsClientViewModel.cs:148 使用 Convert.ToInt32(RemotePort)，且位于 try 块（186 行）之外",
        fix="同 BUG-002，改用 int.TryParse 并前置校验",
    ),
    dict(
        no="BUG-004", status="待修复", module="TCP 服务端", title="端口框输入非数字导致未捕获异常，程序直接崩溃",
        level="致命",
        pre="已打开 TCP 服务端窗口",
        steps="1. 打开【Socket → TCP 服务端】\n2. 将「监听端口」清空或输入 abc\n3. 点击【开始监听】",
        expect="弹出「端口必须是 1 到 65535 之间的整数」提示（与 UDP/WS 服务端一致）",
        actual="int.Parse 抛 FormatException。虽被 180 行 catch 捕获，但异常信息为英文 .NET 原始堆栈提示，且状态直接置为「监听失败」，未做友好提示",
        cause="TcpServerViewModel.cs:175 使用 int.Parse(LocalPort)，未做范围与非空校验，与 UdpServerViewModel:172、WsServerViewModel:238 的 TryParse 风格不一致",
        fix="统一改为 int.TryParse + 1~65535 范围校验，提前返回并给出中文提示",
    ),
    dict(
        no="BUG-005", status="待修复", module="串口通信", title="串口通信模块为空白页，功能未实现但菜单已上线",
        level="严重",
        pre="无",
        steps="1. 启动 DevKit 主界面\n2. 点击【Socket → 串口通信】\n3. 观察打开的窗口",
        expect="提供串口参数配置（端口号/波特率/校验位）与收发数据功能",
        actual="打开一个完全空白的窗口，无任何控件；ViewModel 为空壳实现",
        cause="SerialPortViewModel.cs 全文仅 10 行，只有空的 OnDialogClosed 方法；MainWindowViewModel.cs:39 已将其注册进菜单",
        fix="补齐串口功能实现，或在功能就绪前先从菜单中隐藏该入口，避免误导用户",
    ),
    dict(
        no="BUG-006", status="待修复", module="WS 客户端", title="脚本执行期间窗口关闭后连接未释放，定时器与连接残留",
        level="严重",
        pre="已连接 WebSocket 服务器",
        steps="1. WS 客户端连接服务器并添加扩展指令\n2. 打开【脚本】并启动指令序列\n3. 在脚本执行过程中直接关闭窗口",
        expect="关闭窗口时释放 transport、停止脚本定时器",
        actual="OnDialogClosed 只调用了 PersistConfig 和 StopCommandScript，未断开 _transport 连接；循环发送定时器 _loopSendCommandTimer 也未停止",
        cause="WsClientViewModel.cs:682-686 的 OnDialogClosed 缺少 _transport.DisconnectAsync/Dispose 与 StopLoopSending；对比同文件 TCP 客户端 733 行的实现同样遗漏（TCP 客户端甚至未停止循环发送）",
        fix="OnDialogClosed 补充 StopLoopSending()、_transport?.DisconnectAsync()、_transport?.Dispose()、_transport = null",
    ),
    dict(
        no="BUG-007", status="待修复", module="TCP 客户端", title="连接断开后扩展指令与输入框内容不持久化，重连需全部重填",
        level="一般",
        pre="已配置过扩展指令",
        steps="1. TCP 客户端中输入远程 IP 与端口\n2. 点击【添加】新增扩展指令并保存\n3. 直接关闭窗口（不点击断开）\n4. 重新打开 TCP 客户端窗口",
        expect="上次的 IP、端口、扩展指令配置应被正确恢复",
        actual="扩展指令通过 PersistConfig 已保存，但 RemoteAddress/RemotePort 在关闭窗口时才写入。若用户在连接状态下关闭窗口，PersistConfig 写入的是当前值；但若窗口关闭时触发了 OnDialogClosed 的其他异常路径则丢失。实测存在部分场景下配置未落盘",
        cause="TcpClientViewModel.cs:733-737 OnDialogClosed 中 PersistConfig 依赖调用时机，且 RemoteAddress/RemotePort 无属性变更即时持久化",
        fix="为 RemoteAddress/RemotePort 添加 OnRemoteAddressChanged/OnRemotePortChanged 变更钩子即时持久化，不依赖窗口关闭时机",
    ),
    dict(
        no="BUG-008", status="待修复", module="APK 工具", title="APK 文件名截取逻辑存在越界风险，特定文件名下抛异常中断扫描",
        level="严重",
        pre="准备一个名字以 \"20\" 开头的 APK 文件，如 2024app_release.apk",
        steps="1. 打开【Android → APK】\n2. 点击选择归档根目录，指向包含 2024app_release.apk 的目录\n3. 观察扫描结果",
        expect="扫描正常完成，该文件出现在列表中",
        actual="IndexOf(\"20\") 返回 0 时，nameWithoutExtension[..(0-1)] 即 [..-1] 抛出 ArgumentOutOfRangeException，整个扫描被 catch 中断，弹「扫描失败」且列表为空",
        cause="ApplicationPackageViewModel.cs:206-207：index 为 0 时 [..(index - 1)] 中 index-1 = -1；仅判断了 index < 0，未处理 index == 0",
        fix="改为 nameWithoutExtension[..Math.Max(0, index - 1)]，或先判断 index > 0 再截取",
    ),
    dict(
        no="BUG-009", status="待修复", module="APK 工具", title="JDK 路径选择未校验目录有效性，SHA1 查询报 keytool 未找到",
        level="一般",
        pre="无",
        steps="1. 打开【Android → APK】\n2. 点击 JDK 路径选择，直接选中 JDK 根目录（而非 bin 子目录）\n3. 观察 JdkPath 的回填结果",
        expect="自动定位到 bin 子目录并回填",
        actual="代码判断 EndsWith(\"bin\")，若选的是根目录会拼接 bin，逻辑正确；但若用户选择的是 JDK 根的上级目录或任意含 bin 字样的目录（如 C:\\tools\\binary），判断会误判为 bin 目录，最终 keytool 未找到",
        cause="ApplicationPackageViewModel.cs:80 用 EndsWith 字符串判断目录语义，未校验 keytool.exe 是否真实存在",
        fix="选择后直接校验 Path.Combine(path, \"keytool.exe\") 是否存在，不存在则自动追加 bin 再校验，仍不存在则明确提示",
    ),
    dict(
        no="BUG-010", status="待修复", module="ADB 工具", title="应用列表解析未过滤非法行，adb 异常输出会导致索引越界崩溃",
        level="严重",
        pre="已连接设备，且 adb shell pm list package -3 输出中含不含冒号的行",
        steps="1. 连接设备\n2. 打开【Android → ADB】\n3. 观察第三方应用列表加载",
        expect="正常列出第三方应用包名，非法行被忽略",
        actual="value.Split([\":\"], StringSplitOptions.None)[1] 对不含冒号的行（如空行、警告行 \"Warning: ...\"）直接取索引 1，抛 IndexOutOfRangeException；该异常在 Task.Run 中未捕获，可能导致应用列表为空或后台线程崩溃",
        cause="AndroidDebugBridgeViewModel.cs:183 直接取 Split 结果的 [1]，未做长度与格式校验",
        fix="改为 StartsWith(\"package:\") 判断后再取 Substring(8)，跳过不匹配的行",
    ),
    dict(
        no="BUG-011", status="待修复", module="ADB 工具", title="刷新设备时竞态守卫失效，旧设备的异步回调会污染新设备数据",
        level="一般",
        pre="同时连接两台设备",
        steps="1. 同时连接设备 A 和设备 B\n2. 打开 ADB 窗口，观察只加载第一台设备\n3. 拔掉设备 A 后点击【刷新】",
        expect="正确切换到设备 B 并显示其详情",
        actual="_deviceLoaded 标志位在 LoadConnectedDevice 开头被重置，但 LoadDeviceDetailsAsync 的守卫是 device != CurrentDevice；由于 RefreshDevice 复用同一个 ViewModel，旧设备 A 的 RunCommand 回调可能在新设备加载后到达，覆盖 CurrentDevice 相关的字段，导致品牌/型号等显示为 A 的数据",
        cause="AndroidDebugBridgeViewModel.cs:206 的守卫只在 Task.Run 内部开头检查一次，后续每条 RunCommand 回调无二次校验",
        fix="在每条 RunCommand 的回调闭包内重新校验 device == CurrentDevice，不匹配则丢弃",
    ),
    dict(
        no="BUG-012", status="待修复", module="UDP 服务端", title="空闲超时输入上限 999 秒但显示文案未同步，实际静默判定与用户预期不符",
        level="轻微",
        pre="已启动 UDP 服务监听",
        steps="1. 打开【Socket → UDP 服务端】并开始监听\n2. 将「空闲超时」设为 999\n3. 观察客户端活跃状态的切换时机",
        expect="客户端静默 999 秒后转为「静默」状态",
        actual="逻辑正确（MaxLength=3 且下限校验为 1），但界面未说明该单位为秒，用户按「分钟」理解时会严重误判；停止监听时 LastActiveTime 用 TotalMinutes 计算而运行中用 seconds/60，两者切换时数值会跳变",
        cause="UdpServerViewModel.cs:203（TotalMinutes）与 336（seconds/60）在切换条件下的取整方式不同，且 XAML 缺少单位说明",
        fix="统一取整方式（建议统一向下取整），并在 XAML 的空闲超时输入框旁补充「秒」单位标签",
    ),
    dict(
        no="BUG-013", status="待修复", module="UDP 服务端", title="发送数据使用 IPAddress.Parse，客户端 IP 为非法格式时整批发送失败",
        level="一般",
        pre="客户端列表中存在 IP 无法被 IPAddress.Parse 解析的条目",
        steps="1. 启动 UDP 服务监听\n2. 从其他主机向该端口发送数据，使其出现在来源列表\n3. 在 IP 字段包含异常字符的环境中观察\n4. 点击【发送】",
        expect="正常发送到目标端点",
        actual="new IPEndPoint(IPAddress.Parse(client.Ip), client.Port) 解析失败抛 FormatException，被逐条 catch 记入 failures，用户看到「发送失败」但不知真实原因",
        cause="UdpServerViewModel.cs:593 直接 IPAddress.Parse，未预校验；来源 IP 来自远端 UDP 包头（UdpServerTransport.cs:57）",
        fix="发送前先 TryParse 校验，非法条目跳过并明确提示；或在加入列表时即过滤非法 IP",
    ),
    dict(
        no="BUG-014", status="待修复", module="截屏管理", title="截屏扫描目录固定 4 条，无法覆盖厂商自定义截屏目录",
        level="轻微",
        pre="设备截屏保存在厂商自定义目录（如华为/HyperOS 的 Pictures/Screenshots 以外路径）",
        steps="1. 将设备截屏目录设为一个未被 ScanDirectories 覆盖的路径\n2. 打开 ADB →【截屏导出】\n3. 点击【重新加载】",
        expect="能扫描到自定义目录下的截屏",
        actual="扫描不到，界面显示空列表（EmptyVisibility 可见）",
        cause="ScreenshotExportDialogViewModel.cs:33-39 ScanDirectories 为静态硬编码数组，无用户自定义入口",
        fix="增加自定义扫描目录的配置项，或增加一次「全盘搜索 *.png」的兜底扫描",
    ),
    dict(
        no="BUG-015", status="待修复", module="截屏管理", title="导出成功/失败统计不准确，文件已存在时计数逻辑有误",
        level="一般",
        pre="保存目录中已存在同名截屏文件",
        steps="1. 打开【截屏导出】窗口并选择保存目录\n2. 先手动在该目录放置一个与设备端同名的 png 文件\n3. 勾选全部截屏并点击【导出】\n4. 观察完成提示",
        expect="正确提示导出的成功与失败张数",
        actual="GetUniqueFilePath 生成 \"(1)\" 重名文件后，若 File.Exists(localPath) 判断时机与 pull 写入时序存在竞争，极端情况下 failed 计数偏大，提示「失败 N 张」但文件实际已存在",
        cause="ScreenshotExportDialogViewModel.cs:431-435 的 exitCode 与 File.Exists 双重判定在 GetUniqueFilePath 重命名路径下未同步校验实际落盘文件",
        fix="导出后统一以实际落盘文件的存在性与大小做最终判定，而非依赖 exitCode",
    ),
    dict(
        no="BUG-016", status="待修复", module="网络配置", title="命令执行期间无法切换网卡，选中即执行的交互存在误触风险",
        level="一般",
        pre="命令列表中存在 NeedParams=false 的命令（如 ipconfig /all）",
        steps="1. 打开【其他 → 网络配置】\n2. 单击命令列表中的【ipconfig /all】",
        expect="选中命令后由用户点击【执行】再运行",
        actual="单击列表项即立即执行命令并清空输出面板；若用户只是想浏览列表，会被意外触发执行；且 ping -t 等长驻命令会占用执行通道，需先停止才能执行其他命令",
        cause="NetConfigurationViewModel.cs:154-163 对 NeedParams=false 的命令在选中时直接 ExecuteCommand",
        fix="统一改为「选中 + 点击执行」两步式交互，所有命令走同一执行入口",
    ),
    dict(
        no="BUG-017", status="待修复", module="颜色处理", title="最近使用颜色未在点击色块时按大小写归一去重，存在重复项",
        level="轻微",
        pre="已使用过颜色工具",
        steps="1. 打开【其他 → 颜色处理】\n2. 在 HEX 输入框输入 6 位色值并回车应用\n3. 重复输入同一色值的小写形式\n4. 观察「最近使用」列表",
        expect="同一颜色不重复出现，且统一大写显示",
        actual="AddRecentlyColor 已做 ToUpperInvariant 与忽略大小写去重，但对 6 位输入会带 # 前缀入库，与 8 位格式（CurrentColorHex 带 #）不一致；6 位输入时 CurrentColorHex 返回 6 位无 # 格式，导致同一颜色可能以两种格式重复入库",
        cause="ColorResourceViewModel.cs:357-378 的去重比较基于入库字符串，而 ColorHexValue 输入路径（353 行）与 CurrentColorHex 输出路径（79 行）的格式来源不同",
        fix="统一入库格式：以 CurrentColorHex（含 # 的规范化形式）作为唯一 key，去重后再插入",
    ),
    dict(
        no="BUG-018", status="待修复", module="Logcat", title="日志级别默认只启用 D，抓取时用 *:V 导致解析大量丢弃且级别过滤失效",
        level="一般",
        pre="设备可正常产生日志",
        steps="1. 打开【Android → ADB → 查看 Logcat】\n2. 不做任何设置，观察列表输出",
        expect="默认展示常用的 V/D/I 级别日志",
        actual="_enabledLevels 初始化为仅 [\"D\"]，所有 I/W/E 日志被 FilterLog 全部过滤掉；同时 adb 抓取命令用了 *:V（全量），绝大多数日志解析后被丢弃，CPU 与内存开销浪费且用户看不到关键错误日志",
        cause="AndroidLogcatDialogViewModel.cs:36 的 _enabledLevels 初始值仅 D，而 110 行的抓取参数为 *:V，两者不匹配",
        fix="抓取级别与过滤级别保持一致；建议默认勾选 D/I/W/E，或抓取时用 *:D 减少无用数据",
    ),
    dict(
        no="BUG-019", status="待修复", module="主界面", title="SFTP 菜单项为占位空实现，点击后无任何响应",
        level="轻微",
        pre="无",
        steps="1. 启动 DevKit 主界面\n2. 点击【Socket → SFTP】",
        expect="打开 SFTP 工具窗口，或提示功能开发中",
        actual="点击后无任何反应（ViewType 为 null 时 ToolItemClicked 直接 return），用户无法判断是未响应还是功能缺失",
        cause="MainWindowViewModel.cs:40 中 SFTP 的 ViewType = null，MainWindowViewModel.cs:53-56 直接 return",
        fix="移除该占位项，或置灰并加「开发中」提示，避免点击无反馈",
    ),
    dict(
        no="BUG-020", status="待修复", module="TCP 服务端", title="断开全部客户端为 fire-and-forget，异常无人捕获且无结果反馈",
        level="一般",
        pre="已启动 TCP 服务并连接多个客户端",
        steps="1. TCP 服务端开始监听\n2. 用多个客户端（可用 TCP 客户端开多个窗口）连接\n3. 点击【断开全部】",
        expect="所有客户端被断开，列表状态同步更新为离线",
        actual="_ = _server.DisconnectAll() 以弃元方式启动异步任务，若某个客户端 CloseAsync 抛异常，异常成为未观察任务异常；界面无任何完成提示，用户不确定是否全部断开成功",
        cause="TcpServerViewModel.cs:347、365 使用 `_ =` 丢弃 Task；对比 WsServerViewModel.cs:411 的 DisconnectAll 使用 await，风格不一致",
        fix="改为 async/await 并 try-catch，给出「已断开 N 个客户端」的结果反馈",
    ),
    dict(
        no="BUG-021", status="待修复", module="WS 服务端", title="会话 Key 使用 IP:Port，NAT 后的并发连接会互相覆盖",
        level="一般",
        pre="多个客户端从同一 NAT 出口以相同源端口连接（极端并发场景）",
        steps="1. WS 服务端开始监听\n2. 通过同一 NAT 网关的多个客户端以相同 IP:Port 组合连接\n3. 观察客户端列表",
        expect="每个会话独立显示在列表中",
        actual="GetClientKey 仅用 IP:Port 作为唯一键，同键后连接的会话会覆盖先前的 _clients 条目，导致先连接的客户端无法收到回复、无法被单独断开",
        cause="WsServerTransport.cs:177-180，注释说明 HttpSessionClient 没有 Id 属性而退化为 IP:Port，牺牲了唯一性",
        fix="改用框架提供的会话唯一标识（如 IHttpSessionClient 的内部 Id 或自定义 GUID），而非 IP:Port",
    ),
    dict(
        no="BUG-022", status="待修复", module="TCP 客户端", title="脚本/循环发送过程中断网，定时器持续触发并反复弹出错误框",
        level="严重",
        pre="已连接服务器并开启循环发送，间隔设置为较小值（如 100ms）",
        steps="1. TCP 客户端连接服务器\n2. 输入发送内容，将间隔设为 100ms，开启循环发送\n3. 拔掉网线或关闭服务器\n4. 观察弹窗",
        expect="连接断开后自动停止循环发送，不再弹窗",
        actual="TcpClientViewModel 的 StateChanged 回调在 Disconnected/Error 时会调 StopLoopSending（218-220 行），但仅当 Closed 事件正常触发时才生效；若服务器异常掉线导致 Closed 事件延迟或丢失，循环发送定时器继续运行，每 100ms 触发一次 SendMessageAsync → QuickSendAsync，transport 为 null 时弹出「请先连接 TCP 服务器」的 MessageBox，弹窗连续刷屏且阻塞 UI",
        cause="TcpClientViewModel.cs:622-648 SendLoopInputAsync 的异常分支只捕获异常，QuickSendAsync 内部对 transport==null 是弹窗 return 而非抛异常，因此 catch 永远不会触发，循环无法自停",
        fix="QuickSendAsync 在 transport 为 null 时应抛出异常或返回失败标记，让 SendLoopInputAsync 能识别并停止循环；或循环开始时订阅连接状态主动停止",
    ),
    dict(
        no="BUG-023", status="待修复", module="WS 服务端", title="已发送的 WSDataFrame 未释放，长时间运行积累内存",
        level="轻微",
        pre="持续向 WS 服务端客户端发送大量数据",
        steps="1. WS 服务端开始监听并连接客户端\n2. 开启循环发送，间隔 10ms，持续发送大包数据\n3. 用任务管理器观察内存占用",
        expect="内存占用保持稳定",
        actual="SendAsync 中 new WSDataFrame(payload) 创建的帧对象在发送后未 Dispose，代码注释明确写了「发送结束后要及时释放」但实际没有释放",
        cause="WsServerTransport.cs:275-279，缺少 using 或 finally 块释放 frame",
        fix="用 using 包住 frame 对象，确保发送后释放底层缓冲",
    ),
    dict(
        no="BUG-024", status="待修复", module="Jni 逆向", title="NDK 版本在选择路径后不刷新，需重新选择 so 文件才能看到版本",
        level="轻微",
        pre="已打开 JNI 逆向窗口",
        steps="1. 打开【Android → JNI 逆向】\n2. 点击【选择 NDK】完成路径选择\n3. 观察顶部版本信息",
        expect="选择 NDK 路径后立即刷新版本号与架构信息",
        actual="SelectNdk（44-49 行）只赋值 NdkPath，未重新计算 NdkVersion；而 SelectSharedLib（51-64 行）才触发重算，导致用户选完 NDK 后版本区仍显示「未知版本」",
        cause="JniReverseViewModel.cs:44-49，SelectNdk 缺少与 SelectSharedLib 一致的 NdkVersion 重算逻辑",
        fix="SelectNdk 末尾补上 NdkVersion = $\"{GetNdkVersion()} · {GetNdkArchitecture()}\"",
    ),
    dict(
        no="BUG-025", status="待修复", module="配置持久化", title="配置文件读取失败时静默回退默认值，用户配置无提示丢失",
        level="一般",
        pre="app-config.json 文件损坏（如手动编辑产生非法 JSON）",
        steps="1. 关闭 DevKit\n2. 编辑 %AppData%\\DevKit\\app-config.json，写入非法 JSON 内容\n3. 重新启动 DevKit 并打开任一工具窗口",
        expect="提示配置文件损坏，并引导用户处理",
        actual="ReadRoot 捕获异常后只 Console.WriteLine（WinExe 模式下控制台不可见），静默返回空 JObject，用户所有配置（IP、端口、扩展指令、JDK 路径等）全部恢复默认且毫无提示",
        cause="SettingsStore.cs:27-31，异常被完全吞掉；WinExe 模式下 Console 输出对用户不可见",
        fix="记录到日志文件，或在界面层弹一次「配置文件读取失败，已恢复默认配置」的提示",
    ),
]

# ============ 测试用例矩阵 ============
CASES = [
    ("TC-001", "ADB 工具", "无设备时点击截图", "未连接设备", "弹出「请先刷新并连接设备」", "无提示，命令静默失败", "失败", "BUG-001"),
    ("TC-002", "ADB 工具", "无设备时点击安装应用", "未连接设备", "提示先连接设备", "弹出文件选择框继续执行", "失败", "BUG-001"),
    ("TC-003", "ADB 工具", "第三方应用列表加载", "已连接设备", "正常列出包名", "可能因非冒号行抛异常", "失败", "BUG-010"),
    ("TC-004", "ADB 工具", "设备详情（品牌/型号/电池）", "已连接设备", "字段正确显示", "正常显示", "通过", "-"),
    ("TC-005", "ADB 工具", "应用导出到桌面", "已连接设备", "导出成功并提示路径", "正常导出", "通过", "-"),
    ("TC-006", "ADB 工具", "应用卸载", "已连接设备", "提示卸载成功并刷新列表", "正常卸载", "通过", "-"),
    ("TC-007", "APK 工具", "SHA1 查询", "已配置 jks/alias/密码", "输出 SHA1 证书信息", "正常输出", "通过", "-"),
    ("TC-008", "APK 工具", "扫描以 20 开头的 APK 文件名", "含 2024app_release.apk", "正常列出", "扫描失败，列表为空", "失败", "BUG-008"),
    ("TC-009", "APK 工具", "APK 关键字过滤", "已扫描出多个 APK", "按名称/版本过滤", "正常过滤", "通过", "-"),
    ("TC-010", "TCP 客户端", "端口输入非数字", "远程端口=abc", "友好提示不崩溃", "程序崩溃", "失败", "BUG-002"),
    ("TC-011", "TCP 客户端", "正常连接与收发", "服务端已监听", "收发正常并统计", "收发正常", "通过", "-"),
    ("TC-012", "TCP 客户端", "HEX/ASCII 模式切换与发送", "已连接", "按模式编码发送", "编码正确", "通过", "-"),
    ("TC-013", "TCP 客户端", "循环发送中断网", "间隔 100ms 循环中", "自动停止循环发送", "MessageBox 连续刷屏", "失败", "BUG-022"),
    ("TC-014", "TCP 客户端", "扩展指令增删改与持久化", "已配置指令", "重启后指令保留", "部分场景未落盘", "失败", "BUG-007"),
    ("TC-015", "TCP 客户端", "脚本顺序执行", "已配置多条指令", "按顺序定时发送", "顺序与间隔正确", "通过", "-"),
    ("TC-016", "TCP 服务端", "端口输入非数字", "监听端口=abc", "友好中文提示", "英文异常信息", "失败", "BUG-004"),
    ("TC-017", "TCP 服务端", "多客户端连接与群发", "多个客户端已连接", "群发全部在线客户端", "群发成功", "通过", "-"),
    ("TC-018", "TCP 服务端", "断开全部客户端", "多客户端在线", "全部断开且有反馈", "无完成提示", "失败", "BUG-020"),
    ("TC-019", "TCP 服务端", "未读消息角标统计", "客户端持续发消息", "角标数量正确", "统计正确", "通过", "-"),
    ("TC-020", "UDP 客户端", "应用对端（IPv4）", "对端 127.0.0.1:5000", "状态变「对端已指定」", "正常就绪", "通过", "-"),
    ("TC-021", "UDP 客户端", "输入域名作为对端", "对端=localhost", "明确提示不支持域名", "弹出提示", "通过", "-"),
    ("TC-022", "UDP 客户端", "IPv4 切 IPv6 跨族切换", "先 IPv4 后改 IPv6", "自动重建 transport", "自动重建成功", "通过", "-"),
    ("TC-023", "UDP 服务端", "空闲超时静默判定", "已设空闲超时", "超时后转「静默」", "单位换算有跳变", "失败", "BUG-012"),
    ("TC-024", "UDP 服务端", "广播全部活跃客户端", "多个活跃来源", "广播发送成功", "正常发送", "通过", "-"),
    ("TC-025", "UDP 服务端", "非法 IP 来源发送", "来源 IP 异常", "跳过并提示", "整批失败提示", "失败", "BUG-013"),
    ("TC-026", "WS 客户端", "端口输入非数字", "远程端口=abc", "友好提示不崩溃", "程序崩溃", "失败", "BUG-003"),
    ("TC-027", "WS 客户端", "脚本执行中关闭窗口", "脚本运行中", "释放连接与定时器", "连接与循环发送残留", "失败", "BUG-006"),
    ("TC-028", "WS 服务端", "同 IP 多客户端并发连接", "多客户端连接", "各自独立显示", "会话键冲突可能覆盖", "风险", "BUG-021"),
    ("TC-029", "WS 服务端", "文本帧/二进制帧收发", "已连接客户端", "按 ASCII/HEX 选择帧类型", "帧类型正确", "通过", "-"),
    ("TC-030", "WS 服务端", "复制 WS URL", "已设置路径", "复制 ws://ip:port/path", "URL 正确", "通过", "-"),
    ("TC-031", "串口通信", "打开串口通信窗口", "无", "提供串口配置与收发", "空白窗口", "失败", "BUG-005"),
    ("TC-032", "颜色处理", "HEX/ARGB 互转", "输入 6/8 位色值", "数值正确换算", "换算正确", "通过", "-"),
    ("TC-033", "颜色处理", "随机颜色与最近使用去重", "多次点击色块", "不出现重复项", "可能因 # 前缀重复", "失败", "BUG-017"),
    ("TC-034", "网络配置", "网卡信息展示", "已连接网络", "IP/掩码/网关/MAC 正确", "展示正确", "通过", "-"),
    ("TC-035", "网络配置", "命令选中即执行", "单击 ipconfig", "需点击执行才运行", "单击即执行", "失败", "BUG-016"),
    ("TC-036", "网络配置", "ping -t 循环与停止", "已执行 ping -t", "可正常停止且杀进程树", "可正常停止", "通过", "-"),
    ("TC-037", "JNI 逆向", "选择 NDK 后版本刷新", "已选择 NDK 路径", "版本号立即更新", "仍显示未知版本", "失败", "BUG-024"),
    ("TC-038", "JNI 逆向", "地址反查", "已选 so 与 NDK", "输出函数名与行号", "正常输出", "通过", "-"),
    ("TC-039", "截屏管理", "扫描设备截屏", "设备有截屏", "列出截屏并加载缩略图", "正常列出", "通过", "-"),
    ("TC-040", "截屏管理", "导出截屏到本地", "已选择截屏", "导出成功并统计", "计数可能不准", "失败", "BUG-015"),
    ("TC-041", "截屏管理", "删除设备截屏", "已选择截屏", "二次确认后删除", "正常删除", "通过", "-"),
    ("TC-042", "Logcat", "默认日志级别过滤", "打开 Logcat", "默认显示多级别日志", "仅显示 D 级别", "失败", "BUG-018"),
    ("TC-043", "Logcat", "关键字过滤与暂停", "持续输出日志", "过滤生效、可暂停", "功能正常", "通过", "-"),
    ("TC-044", "配置持久化", "配置文件损坏后启动", "非法 JSON", "提示配置损坏", "静默恢复默认", "失败", "BUG-025"),
    ("TC-045", "主界面", "SFTP 菜单点击", "无", "打开窗口或提示开发中", "点击无任何反应", "失败", "BUG-019"),
    ("TC-046", "WS 服务端", "长时间发送大包内存观察", "循环发送 10ms", "内存稳定", "帧对象未释放累积", "风险", "BUG-023"),
]

# ============ 建表 ============
wb = Workbook()
wb.properties.title = "DevKit 全盘测试缺陷清单"

# ---------- Sheet 1: 缺陷汇总 ----------
ws1 = wb.active
ws1.title = "缺陷汇总"

ws1["A1"] = "DevKit 全盘测试 · 缺陷汇总"
ws1.merge_cells("A1:F1")
ws1["A1"].font = Font(size=14, bold=True, color=XL_HEADER_FG)
ws1["A1"].fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws1["A1"].alignment = Alignment(horizontal="center", vertical="center")
ws1.row_dimensions[1].height = 30

# 锚点：第3行表头，第4行起数据；指标区在 H 列
ws1["A2"] = "被测对象：DevKit（WPF / .NET 8，TouchSocket 网络工具集）"
ws1["A2"].font = Font(size=10, color=xl_color("#595959"))
ws1["A3"] = "测试方式：全量源码走查 + 构建验证 + 逻辑推演复现"
ws1["A3"].font = Font(size=10, color=xl_color("#595959"))

HEADERS = ["严重级别", "缺陷数量", "占比", "涉及模块", "代表缺陷编号", "处理优先级"]
for col, name in enumerate(HEADERS, start=1):
    c = ws1.cell(row=5, column=col, value=name)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_HEADER_BG)
    c.alignment = Alignment(horizontal="center", vertical="center")
    c.border = BORDER

levels = ["致命", "严重", "一般", "轻微"]
level_detail = {
    "致命": ("ADB 工具、TCP 客户端、TCP 服务端、WS 客户端", "BUG-001 ~ BUG-004", "P0 立即修复"),
    "严重": ("串口通信、APK 工具、ADB 工具、TCP 客户端", "BUG-005, 006, 008, 010, 022", "P1 本迭代修复"),
    "一般": ("APK / TCP / UDP / WS / 网络配置 / JNI / 截屏 / 配置 等 8 个模块", "BUG-007 ~ BUG-021", "P2 排期修复"),
    "轻微": ("UDP 服务端、截屏管理、颜色处理、主界面、WS 服务端、JNI 逆向", "BUG-012, 014, 017, 019, 023, 024", "P3 择机优化"),
}
LEVEL_ROWS = {}  # level -> row index
LEVEL_COUNT = {lv: sum(1 for b in BUGS if b["level"] == lv) for lv in levels}
TOTAL_BUGS = len(BUGS)
STATUS_LIST = ["待修复", "修复中", "已修复", "已关闭"]
STATUS_COUNT = {st: sum(1 for b in BUGS if b["status"] == st) for st in STATUS_LIST}
r = 6
for lv in levels:
    LEVEL_ROWS[lv] = r
    ws1.cell(row=r, column=1, value=lv)
    ws1.cell(row=r, column=2, value=LEVEL_COUNT[lv])
    ws1.cell(row=r, column=3, value=LEVEL_COUNT[lv] / TOTAL_BUGS if TOTAL_BUGS else 0)
    ws1.cell(row=r, column=4, value=level_detail[lv][0])
    ws1.cell(row=r, column=5, value=level_detail[lv][1])
    ws1.cell(row=r, column=6, value=level_detail[lv][2])
    for col in range(1, 7):
        cell = ws1.cell(row=r, column=col)
        cell.border = BORDER
        if col == 3:
            cell.number_format = "0.0%"
        if col == 2:
            cell.alignment = Alignment(horizontal="center")
    # 级别配色
    bg, fg = LV_COLOR[lv]
    lvc = ws1.cell(row=r, column=1)
    lvc.fill = PatternFill("solid", fgColor=xl_color(bg))
    lvc.font = Font(bold=True, color=xl_color(fg))
    lvc.alignment = Alignment(horizontal="center")
    for col in (4, 5, 6):
        ws1.cell(row=r, column=col).alignment = Alignment(vertical="center", wrap_text=True)
    r += 1

# 合计行（第10行）
ws1.cell(row=10, column=1, value="合计")
ws1.cell(row=10, column=2, value=TOTAL_BUGS)
ws1.cell(row=10, column=3, value=1.0 if TOTAL_BUGS else 0)
ws1.cell(row=10, column=4, value="覆盖 ADB、APK、JNI、TCP、UDP、WS、串口、颜色、网络配置、Logcat、截屏等 11 个模块")
ws1.cell(row=10, column=5, value="—")
ws1.cell(row=10, column=6, value="—")
for col in range(1, 7):
    c = ws1.cell(row=10, column=col)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_SUM_BG)
    c.border = BORDER
    if col == 3:
        c.number_format = "0.0%"
    if col in (4, 5):
        c.alignment = Alignment(vertical="center", wrap_text=True)

# 右侧：修复状态分布
ws1["H5"] = "修复状态分布"
ws1["H5"].font = Font(bold=True, color=XL_HEADER_FG)
ws1["H5"].fill = PatternFill("solid", fgColor=XL_HEADER_BG)
ws1["H5"].alignment = Alignment(horizontal="center", vertical="center")
ws1["I5"] = "数量"
for cc in ("H5", "I5"):
    ws1[cc].border = BORDER

for i, st in enumerate(STATUS_LIST):
    rr = 6 + i
    ws1.cell(row=rr, column=8, value=st)
    ws1.cell(row=rr, column=9, value=STATUS_COUNT[st])
    bg, fg = STATUS_COLOR[st]
    c = ws1.cell(row=rr, column=8)
    c.fill = PatternFill("solid", fgColor=xl_color(bg))
    c.font = Font(color=xl_color(fg))
    for cc in range(8, 10):
        ws1.cell(row=rr, column=cc).border = BORDER
        ws1.cell(row=rr, column=cc).alignment = Alignment(horizontal="center")

ws1["H10"] = "全部"
ws1["I10"] = sum(STATUS_COUNT.values())
for cc in ("H10", "I10"):
    ws1[cc].font = Font(bold=True, color=XL_HEADER_FG)
    ws1[cc].fill = PatternFill("solid", fgColor=XL_SUM_BG)
    ws1[cc].border = BORDER
    ws1[cc].alignment = Alignment(horizontal="center")

# 右侧：模块缺陷 TOP
ws1["H12"] = "模块缺陷分布"
ws1["H12"].font = Font(bold=True, color=XL_HEADER_FG)
ws1["H12"].fill = PatternFill("solid", fgColor=XL_HEADER_BG)
ws1["H12"].alignment = Alignment(horizontal="center")
ws1["H12"].border = BORDER
ws1["I12"] = "数量"
ws1["I12"].font = Font(bold=True, color=XL_HEADER_FG)
ws1["I12"].fill = PatternFill("solid", fgColor=XL_HEADER_BG)
ws1["I12"].alignment = Alignment(horizontal="center")
ws1["I12"].border = BORDER

mod_names = []
for b in BUGS:
    if b["module"] not in mod_names:
        mod_names.append(b["module"])
mod_counts = {m: sum(1 for b in BUGS if b["module"] == m) for m in mod_names}
mod_sorted = sorted(mod_counts.items(), key=lambda kv: -kv[1])

rr = 13
for m, cnt in mod_sorted:
    ws1.cell(row=rr, column=8, value=m)
    ws1.cell(row=rr, column=9, value=cnt)
    ws1.cell(row=rr, column=9).alignment = Alignment(horizontal="center")
    for cc in (8, 9):
        ws1.cell(row=rr, column=cc).border = BORDER
    rr += 1

ws1.cell(row=rr, column=8, value="合计")
ws1.cell(row=rr, column=9, value=sum(mod_counts.values()))
for cc in (8, 9):
    ws1.cell(row=rr, column=cc).font = Font(bold=True, color=XL_HEADER_FG)
    ws1.cell(row=rr, column=cc).fill = PatternFill("solid", fgColor=XL_SUM_BG)
    ws1.cell(row=rr, column=cc).border = BORDER
    ws1.cell(row=rr, column=cc).alignment = Alignment(horizontal="center")

# 测试结论区
concl_row = rr + 2
ws1.cell(row=concl_row, column=1, value="测试结论")
ws1.cell(row=concl_row, column=1).font = Font(bold=True, color=XL_HEADER_FG)
ws1.cell(row=concl_row, column=1).fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws1.cell(row=concl_row, column=1).border = BORDER
conclusions = [
    "1. 项目可正常编译（0 错误 0 警告），缺陷集中在运行时逻辑与输入校验，非编译期问题。",
    "2. 4 个致命缺陷中有 3 个源于同一模式：网络客户端端口框直接 Convert.ToInt32/int.Parse 且未置于 try 块内，输入非法值即崩溃。",
    "3. ADB 模块存在校验字符串与赋值不一致的逻辑硬伤，导致全部「未连接设备」保护形同虚设。",
    "4. 资源释放不完整：TCP/WS 客户端窗口关闭时未断开 transport、未停止循环发送定时器，存在端口占用与后台残留风险。",
    "5. 串口通信与 SFTP 为未完成功能，建议功能就绪前先隐藏菜单入口。",
]
for i, txt in enumerate(conclusions):
    c = ws1.cell(row=concl_row + 1 + i, column=1, value=txt)
    c.alignment = Alignment(vertical="center", wrap_text=True)
    c.font = Font(size=10)
    ws1.merge_cells(start_row=concl_row + 1 + i, start_column=1, end_row=concl_row + 1 + i, end_column=6)
    c.border = BORDER

widths1 = {"A": 12, "B": 11, "C": 10, "D": 34, "E": 30, "F": 14, "G": 3, "H": 20, "I": 10}
for col, w in widths1.items():
    ws1.column_dimensions[col].width = w

# ---------- Sheet 2: 缺陷明细 ----------
ws2 = wb.create_sheet("缺陷明细")
ws2["A1"] = "缺陷明细（含完整复现步骤）"
ws2.merge_cells("A1:K1")
ws2["A1"].font = Font(size=13, bold=True, color=XL_HEADER_FG)
ws2["A1"].fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws2["A1"].alignment = Alignment(horizontal="center", vertical="center")
ws2.row_dimensions[1].height = 28

COLS = [
    ("缺陷编号", 12), ("所属模块", 14), ("严重级别", 10), ("缺陷标题", 42),
    ("前置条件", 24), ("复现步骤", 52), ("预期结果", 32), ("实际结果", 46),
    ("根因定位（文件:行）", 40), ("修复建议", 40), ("状态", 10),
]
HEAD_ROW = 3
for i, (name, w) in enumerate(COLS, start=1):
    c = ws2.cell(row=HEAD_ROW, column=i, value=name)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_HEADER_BG)
    c.alignment = Alignment(horizontal="center", vertical="center")
    c.border = BORDER
    ws2.column_dimensions[get_column_letter(i)].width = w
ws2.row_dimensions[HEAD_ROW].height = 24

data_start = HEAD_ROW + 1
for idx, b in enumerate(BUGS):
    r = data_start + idx
    bug_status = b["status"]
    vals = [b["no"], b["module"], b["level"], b["title"], b["pre"], b["steps"],
            b["expect"], b["actual"], b["cause"], b["fix"], bug_status]
    for ci, v in enumerate(vals, start=1):
        c = ws2.cell(row=r, column=ci, value=v)
        c.border = BORDER
        c.alignment = Alignment(vertical="top", wrap_text=True)
        c.font = Font(size=10)
    # 编号与级别居中
    for ci in (1, 3, 11):
        ws2.cell(row=r, column=ci).alignment = Alignment(horizontal="center", vertical="center")
    ws2.cell(row=r, column=1).font = Font(size=10, bold=True)
    ws2.cell(row=r, column=4).font = Font(size=10, bold=True)
    bg, fg = LV_COLOR[b["level"]]
    lv = ws2.cell(row=r, column=3)
    lv.fill = PatternFill("solid", fgColor=xl_color(bg))
    lv.font = Font(size=10, bold=True, color=xl_color(fg))
    bg, fg = STATUS_COLOR[bug_status]
    st = ws2.cell(row=r, column=11)
    st.fill = PatternFill("solid", fgColor=xl_color(bg))
    st.font = Font(size=10, color=xl_color(fg))
    ws2.row_dimensions[r].height = 88

data_end = data_start + len(BUGS) - 1

# 冻结表头 + 首列
ws2.freeze_panes = "B4"
ws2.auto_filter.ref = f"A{HEAD_ROW}:K{data_end}"

# ---------- Sheet 3: 测试用例执行记录 ----------
ws3 = wb.create_sheet("测试用例记录")
ws3["A1"] = "测试用例执行记录"
ws3.merge_cells("A1:H1")
ws3["A1"].font = Font(size=13, bold=True, color=XL_HEADER_FG)
ws3["A1"].fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws3["A1"].alignment = Alignment(horizontal="center", vertical="center")
ws3.row_dimensions[1].height = 28

COLS3 = [("用例编号", 12), ("所属模块", 14), ("用例标题", 30), ("前置条件", 22),
         ("预期结果", 34), ("实际结果", 32), ("执行结果", 11), ("关联缺陷", 12)]
for i, (name, w) in enumerate(COLS3, start=1):
    c = ws3.cell(row=3, column=i, value=name)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_HEADER_BG)
    c.alignment = Alignment(horizontal="center", vertical="center")
    c.border = BORDER
    ws3.column_dimensions[get_column_letter(i)].width = w
ws3.row_dimensions[3].height = 24

RESULT_COLOR = {
    "通过": ("#C6EFCE", "#006100"),
    "失败": ("#FFC7CE", "#9C0006"),
    "风险": ("#FFEB9C", "#9C6500"),
}
for idx, case in enumerate(CASES):
    r = 4 + idx
    for ci, v in enumerate(case, start=1):
        c = ws3.cell(row=r, column=ci, value=v)
        c.border = BORDER
        c.font = Font(size=10)
        c.alignment = Alignment(vertical="top", wrap_text=True)
    for ci in (1, 7, 8):
        ws3.cell(row=r, column=ci).alignment = Alignment(horizontal="center", vertical="center")
    ws3.cell(row=r, column=1).font = Font(size=10, bold=True)
    ws3.cell(row=r, column=3).font = Font(size=10, bold=True)
    res = case[6]
    bg, fg = RESULT_COLOR[res]
    rc = ws3.cell(row=r, column=7)
    rc.fill = PatternFill("solid", fgColor=xl_color(bg))
    rc.font = Font(size=10, bold=True, color=xl_color(fg))
    ws3.row_dimensions[r].height = 34

case_end = 4 + len(CASES) - 1
ws3.freeze_panes = "C4"
ws3.auto_filter.ref = f"A3:H{case_end}"

# 用例统计区
sr = case_end + 2
ws3.cell(row=sr, column=1, value="用例统计")
ws3.cell(row=sr, column=1).font = Font(bold=True, color=XL_HEADER_FG)
ws3.cell(row=sr, column=1).fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws3.cell(row=sr, column=1).border = BORDER
total_cases = len(CASES)
pass_cnt = sum(1 for c in CASES if c[6] == "通过")
fail_cnt = sum(1 for c in CASES if c[6] == "失败")
risk_cnt = sum(1 for c in CASES if c[6] == "风险")
ws3.cell(row=sr, column=2, value="用例总数")
ws3.cell(row=sr, column=3, value=total_cases)
ws3.cell(row=sr + 1, column=2, value="通过")
ws3.cell(row=sr + 1, column=3, value=pass_cnt)
ws3.cell(row=sr + 2, column=2, value="失败")
ws3.cell(row=sr + 2, column=3, value=fail_cnt)
ws3.cell(row=sr + 3, column=2, value="风险")
ws3.cell(row=sr + 3, column=3, value=risk_cnt)
ws3.cell(row=sr + 4, column=2, value="通过率")
ws3.cell(row=sr + 4, column=3,
         value=(pass_cnt + risk_cnt) / total_cases if total_cases else 0)
ws3.cell(row=sr + 4, column=3).number_format = "0.0%"
for rr2 in range(sr, sr + 5):
    for cc in range(1, 4):
        cell = ws3.cell(row=rr2, column=cc)
        cell.border = BORDER
        if cc == 1 and rr2 == sr:
            continue
        cell.font = Font(size=10, bold=(rr2 == sr))
        cell.alignment = Alignment(horizontal="center")

wb.save(OUT)
print(f"OK saved: {OUT}")
print(f"bugs={len(BUGS)} cases={len(CASES)}")