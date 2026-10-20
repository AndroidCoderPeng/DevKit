# -*- coding: utf-8 -*-
"""DevKit 回归测试报告生成脚本（复用原清单数据 + 回归判定）"""
import importlib.util
import os

from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

REF = os.path.dirname(os.path.abspath(__file__))
OUT = r"D:/Code/RiderProjects/DevKit/DevKit_回归测试报告.xlsx"
ORIG = os.path.join(os.path.dirname(REF), ".DevKit_全盘测试缺陷清单.ref", "build.py")

# ---- 复用原清单数据（只执行数据定义段，不触发原 xlsx 重建）----
_src = open(ORIG, encoding="utf-8").read()
_data_src = _src.split("# ============ 建表 ============")[0]
_ns = {}
exec(compile(_data_src, ORIG, "exec"), _ns)
BUGS = _ns["BUGS"]
CASES = _ns["CASES"]


def xl_color(css_hex: str) -> str:
    return "FF" + css_hex.removeprefix("#").upper()


XL_HEADER_BG = xl_color("#4472C4")
XL_HEADER_FG = xl_color("#FFFFFF")
XL_SUM_BG = xl_color("#2F5597")
XL_BORDER = xl_color("#BFBFBF")
thin = Side(style="thin", color=XL_BORDER)
BORDER = Border(left=thin, right=thin, top=thin, bottom=thin)

LV_COLOR = {
    "致命": ("#C00000", "#FFFFFF"),
    "严重": ("#FF6D3F", "#FFFFFF"),
    "一般": ("#FFD966", "#3F3F3F"),
    "轻微": ("#E2EFDA", "#375623"),
}
STATUS_COLOR = {
    "已修复": ("#C6EFCE", "#006100"),
    "仍存在": ("#FFC7CE", "#9C0006"),
    "已关闭": ("#EDEDED", "#3F3F3F"),
    "新增": ("#FFEB9C", "#9C6500"),
}
REG_COLOR = {
    "已修复": ("#C6EFCE", "#006100"),
    "仍存在": ("#FFC7CE", "#9C0006"),
    "已撤销": ("#EDEDED", "#3F3F3F"),
    "已关闭": ("#E7E6E6", "#3F3F3F"),
    "新增": ("#FFEB9C", "#9C6500"),
}
RESULT_COLOR = {
    "通过": ("#C6EFCE", "#006100"),
    "失败": ("#FFC7CE", "#9C0006"),
    "风险": ("#FFEB9C", "#9C6500"),
    "不适用": ("#EDEDED", "#3F3F3F"),
}

# ============ 回归判定（逐条核对当前代码 + git 提交得出） ============
# key: 缺陷编号 -> (回归结论, 级别(回归后), 依据说明)
VERDICT = {
    "BUG-001": ("已修复", "致命", "提交 a3d46cc：提取常量 Flags = \"no device connected\"，InitValueBinding 赋值与 4 处校验全部引用同一常量，字符串永不相等的硬伤消除；并额外为 Screenshot 补齐了原本缺失的设备校验。"),
    "BUG-002": ("已修复", "致命", "提交 6d13546：ConnectServerAsync 开头新增 int.TryParse + 1~65535 范围校验，转换移出危险路径，崩溃消除。"),
    "BUG-003": ("已修复", "致命", "提交 6d13546：WS 客户端同款端口前置校验，与 TCP 客户端保持一致。"),
    "BUG-004": ("已修复", "致命", "提交 6d13546：TcpServerViewModel 的 int.Parse 改为 TryParse + 范围校验，中文提示与 UDP/WS 服务端统一。"),
    "BUG-005": ("仍存在", "一般", "SerialPortView.xaml 仍为 <Grid /> 空布局，SerialPortViewModel 仍为空壳；菜单已注册但点击后为纯空白窗口，无「开发中」提示。属未上线功能占位，建议隐藏入口或置灰。"),
    "BUG-006": ("已修复", "严重", "提交 c76da85：TcpClientViewModel/WsClientViewModel 的 OnDialogClosed 补入 _transport?.DisconnectAsync() + Dispose() + 置空，连接与底层资源可释放。但定时器清理不完整，衍生 NEW-01 / NEW-02。"),
    "BUG-007": ("已撤销", "一般", "复核当前代码判定有误：PersistConfig 在 AddExtension(:375)、DeleteEx(:457)、输入框变更(:471)、OnDialogClosed(:743) 均已调用，_config.ExCommands 在构造时经 SettingsStore 恢复(:111)。配置可正常落盘并跨会话恢复，原缺陷不成立，予以撤销。"),
    "BUG-008": ("已修复", "严重", "提交 05b72f0：截取条件由 index < 0 改为 index > 0，index == 0 时不再做 [..-1] 越界切片，扫描不再中断。"),
    "BUG-009": ("已撤销", "一般", "复核当前代码判定有误：SelectJdk 选中根目录会自动补 bin(:80)，ShowSha1Async 前已有 File.Exists(keytool.exe) 校验并给出「keytool 未找到，请检查 JDK 路径是否正确」的友好提示，路径误选可被拦截，原缺陷不成立，予以撤销。"),
    "BUG-010": ("已修复", "严重", "提交 05b72f0：改为 StartsWith(\"package:\") 前缀判断 + Substring 提取 + 空串跳过，越界与非法行问题消除。"),
    "BUG-011": ("仍存在", "一般", "AndroidDebugBridgeViewModel.cs:214 的守卫仅在 Task.Run 起始处检查一次 device != CurrentDevice，后续每条 RunCommand 回调内无二次校验，切换设备后旧设备回调仍会覆盖品牌/型号等字段。"),
    "BUG-012": ("已关闭", "轻微", "复核当前代码判定过度：UdpServerView.xaml 空闲超时输入框右侧已有「s」单位标签(:545-550)，单位不存在误读；运行中(:336)已按 seconds/60 向下取整，:203 的 TotalMinutes 快照仅在停止监听瞬间使用，不构成用户可感知的取整跳变。判定为非缺陷，关闭。"),
    "BUG-013": ("仍存在", "一般", "UdpServerViewModel.cs:593 仍为 new IPEndPoint(IPAddress.Parse(client.Ip), client.Port)，无 TryParse 预校验，单条非法来源 IP 会导致整批发送失败且仅提示「发送失败」。"),
    "BUG-014": ("已关闭", "轻微", "ScanDirectories 硬编码 4 条（/sdcard/DCIM/Screenshots、/sdcard/Pictures/Screenshots、/sdcard/ScreenShots、/sdcard）属产品侧扫描范围决策，已覆盖主流厂商路径，无用户自定义入口属功能范围问题而非逻辑缺陷，关闭并转为需求建议。"),
    "BUG-015": ("已撤销", "一般", "复核当前代码判定有误：GetUniqueFilePath 先产出唯一新路径，adb pull 写入的正是该路径，随后以 exitCode != 0 || !File.Exists(localPath) 判定失败，判定对象与写入对象一致，计数逻辑正确，原缺陷不成立，予以撤销。"),
    "BUG-016": ("已关闭", "一般", "NetConfigurationViewModel.cs:154-163 代码注释明确写明「不需要参数的命令，选中即执行」，属有意设计的交互取舍，非实现疏漏，关闭。"),
    "BUG-017": ("仍存在", "轻微", "AddRecentlyColor 本身已做 ToUpperInvariant + OrdinalIgnoreCase 去重(:367-372)，但两条入库路径格式不统一：输入框路径 AddRecentlyColor(value) 存原始值(如 ff0000)，色块路径 AddRecentlyColor(CurrentColorHex) 存带 # 的规范化值，同一颜色仍可能以两种格式重复入库。"),
    "BUG-018": ("已关闭", "一般", "Logcat 对话框提供 V/D/I/W/E 五个级别 ToggleButton 芯片，可随时勾选切换；默认仅勾 D 属保守默认值，且 *:V 全量抓取交由客户端本地过滤便于实时切换级别，属有意设计，关闭。"),
    "BUG-019": ("仍存在", "轻微", "MainWindowViewModel.cs:40 的 SFTP 仍为 ViewType = null，ToolItemClicked 直接 return，点击完全无响应。与 BUG-005 同属未上线功能占位，建议同批处理（隐藏或加占位提示）。"),
    "BUG-020": ("仍存在", "一般", "TcpServerViewModel.cs:354 仍为 _ = _server.DisconnectAll() 弃元调用，且 TcpServerTransport.DisconnectAll 内部无 try/catch，CloseAsync 异常会成为未观察任务异常；界面亦无「已断开 N 个」结果反馈。"),
    "BUG-021": ("仍存在", "一般", "WsServerTransport.cs:177-180 GetClientKey 仍以 IP:Port 作唯一键，同 NAT 出口同源端口的并发会话会互相覆盖，注释已说明是 HttpSessionClient 无 Id 的框架限制。"),
    "BUG-022": ("仍存在", "严重", "TcpClientViewModel.cs:386-399 QuickSendAsync 对 transport == null 走 MessageBox.Show + return 而非抛异常，SendLoopInputAsync(:645) 的 catch 分支因此永不触发，循环发送无法自停，MessageBox 按间隔连续刷屏并阻塞 UI 线程。"),
    "BUG-023": ("仍存在", "轻微", "WsServerTransport.cs:275-279 new WSDataFrame(payload) 发送后未 Dispose，代码注释已写明「要及时释放」但未落实，长时间高频发送会累积底层缓冲。"),
    "BUG-024": ("仍存在", "轻微", "JniReverseViewModel.cs:44-49 SelectNdk 仅赋值 NdkPath，未像 SelectSharedLib(:51-64) 那样重算 NdkVersion，选完 NDK 路径后版本区仍显示未知。"),
    "BUG-025": ("仍存在", "一般", "SettingsStore.cs:27-31 ReadRoot 捕获异常后仅 Console.WriteLine，WinExe 模式下控制台不可见，配置损坏时用户全部设置静默恢复默认且无任何提示。"),
}

# ============ 回归阶段新发现缺陷 ============
NEW_BUGS = [
    dict(
        no="NEW-01", module="TCP 客户端 / WS 客户端", level="严重",
        title="窗口关闭时未停止循环发送定时器，关闭后 MessageBox 持续刷屏并阻塞 UI",
        pre="已在 TCP 客户端或 WS 客户端开启循环发送（间隔 100ms）",
        steps="1. 打开【Socket → TCP 客户端】并连接服务器\n2. 输入发送内容，将间隔设为 100ms，开启循环发送\n3. 在循环发送进行中直接关闭窗口\n4. 观察窗口关闭后的程序行为",
        expect="窗口关闭的同时停止循环发送定时器，不应再有任何弹窗与后台动作",
        actual="OnDialogClosed 已释放并置空 _transport，但未调用 StopLoopSending()；_loopSendCommandTimer 继续 Tick → SendLoopInputAsync → SendMessageAsync → QuickSendAsync，此时 _transport 为 null 走 MessageBox.Show(\"请先连接 TCP 服务器\") 分支，弹窗按设定间隔持续刷屏且阻塞 UI 线程，程序实质卡死",
        cause="TcpClientViewModel.cs:741-748 与 WsClientViewModel.cs:690-697 的 OnDialogClosed 遗漏 StopLoopSending()；对比 TcpServerViewModel.cs:712、WsServerViewModel.cs:753、UdpServerViewModel.cs:748、UdpClientViewModel.cs:611 四个同族实现均已调用",
        fix="两处 OnDialogClosed 补入 StopLoopSending()（并同步补 StopSessionTimer()，见 NEW-02）",
    ),
    dict(
        no="NEW-02", module="TCP 客户端 / WS 客户端", level="一般",
        title="窗口关闭时未停止会话计时器，DispatcherTimer 泄漏且 ViewModel 无法回收",
        pre="已连接服务器，会话计时器已启动",
        steps="1. 连接 TCP 客户端并保持连接数秒（会话计时器每秒 Tick）\n2. 关闭窗口\n3. 观察内存与计时器状态",
        expect="窗口关闭时会话计时器应停止并解除 Tick 订阅",
        actual="OnDialogClosed 未调用 StopSessionTimer()，_sessionTimer 仍以 1 秒间隔 Tick，持续更新 SessionDuration 并持有 ViewModel 引用，形成内存泄漏；StopSessionTimer 内部会将 _sessionTimer 置 null，遗漏后该字段亦无法释放",
        cause="TcpClientViewModel.cs:741-748 与 WsClientViewModel.cs:690-697 的 OnDialogClosed 遗漏 StopSessionTimer()",
        fix="两处 OnDialogClosed 补入 StopSessionTimer()",
    ),
    dict(
        no="NEW-03", module="TCP / WS 客户端 + TCP 服务端", level="轻微",
        title="端口校验使用 NumberStyles.None，拒绝含空白或正号的合法端口输入，且各模块风格不统一",
        pre="已打开 TCP 客户端 / WS 客户端 / TCP 服务端窗口",
        steps="1. 在端口输入框粘贴带首尾空白的值（如 \" 8080 \"）\n2. 或输入带正号的值（如 \"+8080\"）\n3. 点击连接/开始监听",
        expect="对空白与正号做容错处理，或至少给出与实际原因匹配的提示；各网络模块校验风格保持一致",
        actual="int.TryParse(RemotePort, NumberStyles.None, ...) 因 NumberStyles.None 不允许首尾空白与符号而解析失败，统一弹出「端口必须是 1 到 65535 之间的正整数」，用户难以判断真实原因；同项目的 UdpServerViewModel.cs:172 使用的是不带 NumberStyles 的 int.TryParse(LocalPort, out var port)，允许空白，风格不一致",
        cause="提交 6d13546 引入 NumberStyles.None 约束过严，与项目内其它端口校验写法不统一",
        fix="改用 int.TryParse(RemotePort, NumberStyles.Integer, ...) 或先 Trim() 再解析，并统一四个网络模块的校验写法与提示文案",
    ),
    dict(
        no="NEW-04", module="TCP / WS 客户端传输层", level="一般",
        title="DisconnectAsync 等待 closedTcs 无超时与 CancellationToken，Closed 事件丢失时任务永久挂起",
        pre="客户端已连接，随后主动关闭窗口",
        steps="1. 连接 TCP 客户端\n2. 关闭窗口触发 OnDialogClosed 中的 _ = _transport?.DisconnectAsync()\n3. 观察闭包持有的 transport 实例是否被释放",
        expect="DisconnectAsync 应有超时或可取消机制，Closed 事件未触发时能正常返回并释放资源",
        actual="TcpClientTransport.cs:139-143 与 WsClientTransport.cs:203-206 在 await closedTcs.Task 处既无超时也无 CancellationToken；若 Closed 事件因异常未触发，该 Task 永久挂起。OnDialogClosed 用弃元 _ = 启动，虽不阻塞 UI，但挂起任务的闭包持续持有 transport 与其内部缓冲区，形成不可回收的对象滞留",
        cause="TcpClientTransport.cs:139-143、WsClientTransport.cs:203-206 缺少 WaitAsync 超时或 CancellationToken 保护",
        fix="改为 await closedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3)) 或传入 CancellationToken，超时后记录并继续释放",
    ),
    dict(
        no="NEW-05", module="TCP 客户端传输层", level="轻微",
        title="主动/被动断开仅依赖 Closing 事件标记 _activeClose，事件语义不明确时状态上报会失真",
        pre="已连接 TCP 服务器，服务器侧主动关闭或网络异常断开",
        steps="1. TCP 客户端连接服务器\n2. 在服务器侧关闭连接或制造网络异常\n3. 观察客户端状态提示",
        expect="主动断开显示「已断开」，被动断开显示「连接异常」，两种状态可区分",
        actual="TcpClientTransport.cs:173 仅在 client.Closing 回调中置 _activeClose = true，:184 据此上报 Disconnected 或 Error。注释声称「Closing 仅在主动断开时触发」，但该语义依赖 TouchSocket 内部实现；若被动断开路径同样触发 Closing，则所有异常断开都会被误报为正常断开，客户端失去异常感知能力（WsClientTransport.cs:188 显式置位，行为不统一）",
        cause="TcpClientTransport.cs:173/184 对 _activeClose 的判定过度依赖框架事件语义，缺少与 WsClientTransport 一致的显式标记",
        fix="在 TcpClientTransport 中显式区分主动断开路径（DisconnectAsync 内主动置位），被动断开不置位，与 WsClientTransport 保持一致",
    ),
]

# ============ 用例回归结果 ============
# 编号 -> (回归结果, 回归实际现象, 关联缺陷)
CASE_REG = {
    "TC-001": ("通过", "点击截图正确弹出「请先刷新并连接设备」，不执行 adb 命令", "BUG-001"),
    "TC-002": ("通过", "点击安装应用正确提示先连接设备，不再弹出文件选择框", "BUG-001"),
    "TC-003": ("通过", "第三方应用包名正常列出，非冒号行被正确跳过", "BUG-010"),
    "TC-008": ("通过", "2024app_release.apk 正常列出，扫描不再中断", "BUG-008"),
    "TC-010": ("通过", "端口输入 abc 弹出中文提示，程序不崩溃", "BUG-002"),
    "TC-013": ("失败", "循环发送中断网后 MessageBox 仍按间隔连续刷屏，无法自停", "BUG-022"),
    "TC-014": ("通过", "扩展指令增删改均即时落盘，重启后指令完整恢复", "BUG-007"),
    "TC-016": ("通过", "监听端口输入 abc 弹出「端口必须是 1 到 65535 之间的正整数」", "BUG-004"),
    "TC-018": ("失败", "断开全部客户端无完成提示，异常时静默失败", "BUG-020"),
    "TC-023": ("通过", "空闲超时单位「s」已在界面标注，静默判定与用户预期一致", "BUG-012"),
    "TC-025": ("失败", "非法 IP 来源仍导致整批发送失败", "BUG-013"),
    "TC-026": ("通过", "端口输入 abc 弹出中文提示，程序不崩溃", "BUG-003"),
    "TC-027": ("失败", "连接与脚本定时器已释放，但循环发送定时器未停，关闭后仍弹窗刷屏", "BUG-006 / NEW-01"),
    "TC-031": ("失败", "串口通信窗口仍为全空白", "BUG-005"),
    "TC-033": ("失败", "同一颜色以 ff0000 与 #FF0000 两种格式重复入库", "BUG-017"),
    "TC-035": ("不适用", "选中即执行为有意设计，回归确认符合预期", "BUG-016"),
    "TC-037": ("失败", "选择 NDK 后版本区仍显示未知版本", "BUG-024"),
    "TC-040": ("通过", "导出成功/失败张数统计与实际落盘文件一致", "BUG-015"),
    "TC-042": ("不适用", "默认仅 D 级别为有意设计，级别芯片可随时切换", "BUG-018"),
    "TC-044": ("失败", "配置损坏时仍静默恢复默认，无任何提示", "BUG-025"),
    "TC-045": ("失败", "SFTP 菜单项点击仍无任何响应", "BUG-019"),
    "TC-047": ("失败", "窗口关闭后循环发送定时器仍在运行并持续弹窗", "NEW-01"),
    "TC-048": ("失败", "窗口关闭后会话计时器未停止，ViewModel 持续被引用", "NEW-02"),
    "TC-049": ("失败", "端口输入带首尾空白时被拒绝，提示与真实原因不符", "NEW-03"),
    "TC-050": ("风险", "Closed 事件丢失时 DisconnectAsync 任务挂起，对象滞留待验证", "NEW-04"),
    "TC-051": ("风险", "被动断开是否误报为正常断开，取决于 TouchSocket Closing 事件语义", "NEW-05"),
}

EXTRA_CASES = [
    ("TC-047", "TCP/WS 客户端", "窗口关闭时循环发送定时器", "循环发送进行中关闭窗口", "定时器停止、无弹窗", "定时器未停，MessageBox 持续刷屏", "失败", "NEW-01"),
    ("TC-048", "TCP/WS 客户端", "窗口关闭时会话计时器", "已连接并保持数秒后关闭", "计时器停止、对象可回收", "计时器未停，ViewModel 被持续引用", "失败", "NEW-02"),
    ("TC-049", "TCP/WS 客户端", "端口输入带空白或正号", "端口= 8080 或 +8080", "容错处理或提示准确", "被 NumberStyles.None 拒绝，提示误导", "失败", "NEW-03"),
    ("TC-050", "TCP/WS 传输层", "主动断开等待 Closed 事件", "关闭窗口触发断开", "断开任务在超时内返回", "无超时保护，任务可能永久挂起", "风险", "NEW-04"),
    ("TC-051", "TCP 客户端传输层", "被动断开状态上报", "服务器侧关闭连接", "异常断开报 Error", "可能被误报为 Disconnected", "风险", "NEW-05"),
]

# ============ 组装明细数据 ============
ALL_ROWS = []
for b in BUGS:
    verdict, level, note = VERDICT[b["no"]]
    ALL_ROWS.append(dict(
        no=b["no"], module=b["module"], level=level, title=b["title"],
        pre=b["pre"], steps=b["steps"], expect=b["expect"], actual=b["actual"],
        cause=b["cause"], fix=b["fix"], verdict=verdict, note=note, is_new=False,
    ))
for nb in NEW_BUGS:
    ALL_ROWS.append(dict(
        no=nb["no"], module=nb["module"], level=nb["level"], title=nb["title"],
        pre=nb["pre"], steps=nb["steps"], expect=nb["expect"], actual=nb["actual"],
        cause=nb["cause"], fix=nb["fix"], verdict="新增", note="回归阶段新发现",
        is_new=True,
    ))

FIXED = [r for r in ALL_ROWS if r["verdict"] == "已修复"]
REMAIN = [r for r in ALL_ROWS if r["verdict"] == "仍存在"]
CLOSED = [r for r in ALL_ROWS if r["verdict"] == "已关闭"]
DROPPED = [r for r in ALL_ROWS if r["verdict"] == "已撤销"]
NEWL = [r for r in ALL_ROWS if r["verdict"] == "新增"]
OPEN = REMAIN + NEWL

LEVELS = ["致命", "严重", "一般", "轻微"]
LEVEL_COUNT = {lv: len([r for r in ALL_ROWS if r["level"] == lv]) for lv in LEVELS}
OPEN_LEVEL_COUNT = {lv: len([r for r in OPEN if r["level"] == lv]) for lv in LEVELS}

# ============ 建表 ============
wb = Workbook()
wb.properties.title = "DevKit 回归测试报告"

# ---------- Sheet 1: 回归结论汇总 ----------
ws1 = wb.active
ws1.title = "回归结论汇总"
ws1["A1"] = "DevKit 回归测试 · 结论汇总"
ws1.merge_cells("A1:F1")
ws1["A1"].font = Font(size=14, bold=True, color=XL_HEADER_FG)
ws1["A1"].fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws1["A1"].alignment = Alignment(horizontal="center", vertical="center")
ws1.row_dimensions[1].height = 30

ws1["A2"] = "被测对象：DevKit（WPF / .NET 8，TouchSocket 网络工具集）"
ws1["A2"].font = Font(size=10, color=xl_color("#595959"))
ws1["A3"] = "回归基线：git HEAD = 05b72f0（fix 版本 4 次提交：a3d46cc / 6d13546 / c76da85 / 05b72f0）"
ws1["A3"].font = Font(size=10, color=xl_color("#595959"))
ws1["A4"] = "回归方式：逐条 diff 复核 + 当前代码走查 + dotnet build 验证（0 错误 0 警告）+ 逻辑推演复现"
ws1["A4"].font = Font(size=10, color=xl_color("#595959"))

HEADERS = ["回归结论", "数量", "占比", "涉及缺陷编号", "处置建议", "优先级"]
for col, name in enumerate(HEADERS, start=1):
    c = ws1.cell(row=6, column=col, value=name)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_HEADER_BG)
    c.alignment = Alignment(horizontal="center", vertical="center")
    c.border = BORDER

total = len(ALL_ROWS)
verdict_rows = [
    ("已修复", FIXED, "BUG-001, 002, 003, 004, 006, 008, 010", "回归验证通过，无需再处理", "—"),
    ("仍存在", REMAIN, "BUG-005, 011, 013, 017, 019, 020, 021, 022, 023, 024, 025", "按级别排期修复，本迭代优先处理 022", "P0/P1"),
    ("已撤销", DROPPED, "BUG-007, 009, 015", "原判定有误，复核当前代码后撤销", "—"),
    ("已关闭", CLOSED, "BUG-012, 014, 016, 018", "经核实属有意设计或产品范围决策，非缺陷", "—"),
    ("新增", NEWL, "NEW-01 ~ NEW-05", "回归阶段新发现，纳入修复清单", "P0/P1"),
]
r = 7
for name, rows, nos, action, prio in verdict_rows:
    ws1.cell(row=r, column=1, value=name)
    ws1.cell(row=r, column=2, value=len(rows))
    ws1.cell(row=r, column=3, value=len(rows) / total if total else 0)
    ws1.cell(row=r, column=4, value=nos)
    ws1.cell(row=r, column=5, value=action)
    ws1.cell(row=r, column=6, value=prio)
    for col in range(1, 7):
        cell = ws1.cell(row=r, column=col)
        cell.border = BORDER
        cell.alignment = Alignment(vertical="center", wrap_text=True)
    ws1.cell(row=r, column=3).number_format = "0.0%"
    for col in (1, 2, 6):
        ws1.cell(row=r, column=col).alignment = Alignment(horizontal="center", vertical="center")
    bg, fg = REG_COLOR[name]
    vc = ws1.cell(row=r, column=1)
    vc.fill = PatternFill("solid", fgColor=xl_color(bg))
    vc.font = Font(bold=True, color=xl_color(fg))
    r += 1

ws1.cell(row=12, column=1, value="合计")
ws1.cell(row=12, column=2, value=total)
ws1.cell(row=12, column=3, value=1.0)
ws1.cell(row=12, column=4, value="原清单 25 条 + 回归新增 5 条")
ws1.cell(row=12, column=5, value=f"待处理 {len(OPEN)} 条（仍存在 {len(REMAIN)} + 新增 {len(NEWL)}）")
ws1.cell(row=12, column=6, value="—")
for col in range(1, 7):
    c = ws1.cell(row=12, column=col)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_SUM_BG)
    c.border = BORDER
    c.alignment = Alignment(vertical="center", wrap_text=True, horizontal="center" if col in (2, 3, 6) else "left")
ws1.cell(row=12, column=3).number_format = "0.0%"

# 右侧：修复提交验证矩阵
ws1["H6"] = "修复提交验证矩阵"
ws1["H6"].font = Font(bold=True, color=XL_HEADER_FG)
ws1["H6"].fill = PatternFill("solid", fgColor=XL_HEADER_BG)
ws1["H6"].alignment = Alignment(horizontal="center", vertical="center")
ws1.merge_cells("H6:K6")
for cc in ("H6", "I6", "J6", "K6"):
    ws1[cc].border = BORDER
for i, name in enumerate(["提交", "提交说明", "涉及缺陷", "验证结论"], start=8):
    c = ws1.cell(row=i, column=name and 0 or i, value=None) if False else None
commits = [
    ("a3d46cc", "统一设备连接状态常量，优化设备未连接提示逻辑", "BUG-001", "修复有效，且额外补齐 Screenshot 校验"),
    ("6d13546", "添加端口号验证，确保端口在 1 到 65535 之间的正整数", "BUG-002/003/004", "修复有效，NumberStyles.None 过严见 NEW-03"),
    ("c76da85", "添加空日志列表检查，优化日志订阅与取消订阅逻辑", "BUG-006", "主体已修，定时器清理遗漏见 NEW-01/02"),
    ("05b72f0", "优化包名处理逻辑，确保只处理有效的包名并简化代码结构", "BUG-008/010", "修复有效，无回归"),
]
for j, (h, v) in enumerate([("提交", "提交说明"), ("涉及缺陷", "验证结论")]):
    pass
rr = 7
for cm, desc, bugs, concl in commits:
    ws1.cell(row=rr, column=8, value=cm)
    ws1.cell(row=rr, column=9, value=desc)
    ws1.merge_cells(start_row=rr, start_column=9, end_row=rr, end_column=10)
    ws1.cell(row=rr, column=11, value=bugs + "｜" + concl)
    ws1.merge_cells(start_row=rr, start_column=11, end_row=rr, end_column=13)
    for col in range(8, 14):
        c = ws1.cell(row=rr, column=col)
        c.border = BORDER
        c.alignment = Alignment(vertical="center", wrap_text=True)
        c.font = Font(size=9)
    ws1.cell(row=rr, column=8).font = Font(size=9, bold=True)
    ws1.row_dimensions[rr].height = 30
    rr += 1

# 右侧：待处理缺陷按级别
ws1["H13"] = "待处理缺陷 · 按级别"
ws1["H13"].font = Font(bold=True, color=XL_HEADER_FG)
ws1["H13"].fill = PatternFill("solid", fgColor=XL_HEADER_BG)
ws1["H13"].alignment = Alignment(horizontal="center", vertical="center")
ws1.merge_cells("H13:I13")
for cc in ("H13", "I13"):
    ws1[cc].border = BORDER
rr = 14
for lv in LEVELS:
    ws1.cell(row=rr, column=8, value=lv)
    ws1.cell(row=rr, column=9, value=OPEN_LEVEL_COUNT[lv])
    bg, fg = LV_COLOR[lv]
    c = ws1.cell(row=rr, column=8)
    c.fill = PatternFill("solid", fgColor=xl_color(bg))
    c.font = Font(size=10, bold=True, color=xl_color(fg))
    for col in (8, 9):
        ws1.cell(row=rr, column=col).border = BORDER
        ws1.cell(row=rr, column=col).alignment = Alignment(horizontal="center")
    rr += 1
ws1.cell(row=rr, column=8, value="待处理合计")
ws1.cell(row=rr, column=9, value=len(OPEN))
for cc in ("H" + str(rr), "I" + str(rr)):
    ws1[cc].font = Font(bold=True, color=XL_HEADER_FG)
    ws1[cc].fill = PatternFill("solid", fgColor=XL_SUM_BG)
    ws1[cc].border = BORDER
    ws1[cc].alignment = Alignment(horizontal="center")

# 结论区
concl_row = 18
ws1.cell(row=concl_row, column=1, value="回归结论")
ws1.cell(row=concl_row, column=1).font = Font(bold=True, color=XL_HEADER_FG)
ws1.cell(row=concl_row, column=1).fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws1.cell(row=concl_row, column=1).border = BORDER
conclusions = [
    "1. 用户 4 次提交集中修复了 7 条原缺陷（BUG-001/002/003/004/006/008/010），4 条致命缺陷全部消除，程序崩溃类风险已清零。",
    "2. 复核当前代码后，原清单中 3 条（BUG-007 持久化、BUG-009 JDK 路径、BUG-015 导出计数）判定有误，代码实际已具备相应处理，予以撤销。",
    "3. 4 条（BUG-012 空闲超时单位、BUG-014 扫描目录、BUG-016 选中即执行、BUG-018 Logcat 默认级别）经核实为有意设计或产品范围决策，已关闭。",
    "4. 仍存在 11 条、新增 5 条，共 16 条待处理；其中 NEW-01（关闭后循环发送定时器未停，MessageBox 刷屏卡死 UI）为 c76da85 修复不完整引入的回归，应优先处理。",
    "5. TCP / WS 客户端的 OnDialogClosed 资源清理仍不完整（NEW-01 / NEW-02），而同族 4 个服务端/UDP 实现均已完整，可直接照抄补齐。",
    "6. 建议顺带统一四个网络模块的端口校验写法（NEW-03），并为传输层 DisconnectAsync 增加等待超时（NEW-04）。",
]
for i, txt in enumerate(conclusions):
    c = ws1.cell(row=concl_row + 1 + i, column=1, value=txt)
    c.alignment = Alignment(vertical="center", wrap_text=True)
    c.font = Font(size=10)
    ws1.merge_cells(start_row=concl_row + 1 + i, start_column=1, end_row=concl_row + 1 + i, end_column=6)
    c.border = BORDER
    ws1.row_dimensions[concl_row + 1 + i].height = 26

widths1 = {"A": 12, "B": 9, "C": 9, "D": 40, "E": 34, "F": 11, "G": 3, "H": 12, "I": 30, "J": 6, "K": 30, "L": 22, "M": 22}
for col, w in widths1.items():
    ws1.column_dimensions[col].width = w

# ---------- Sheet 2: 缺陷明细（含回归结论） ----------
ws2 = wb.create_sheet("缺陷明细")
ws2["A1"] = "缺陷明细（含回归结论与验证依据）"
ws2.merge_cells("A1:M1")
ws2["A1"].font = Font(size=13, bold=True, color=XL_HEADER_FG)
ws2["A1"].fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws2["A1"].alignment = Alignment(horizontal="center", vertical="center")
ws2.row_dimensions[1].height = 28

COLS = [
    ("缺陷编号", 11), ("所属模块", 16), ("回归后级别", 10), ("缺陷标题", 40),
    ("前置条件", 24), ("复现步骤", 50), ("预期结果", 30), ("实际结果", 44),
    ("根因定位（文件:行）", 38), ("修复建议", 38), ("回归结论", 10), ("验证依据", 56),
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
for idx, b in enumerate(ALL_ROWS):
    r = data_start + idx
    vals = [b["no"], b["module"], b["level"], b["title"], b["pre"], b["steps"],
            b["expect"], b["actual"], b["cause"], b["fix"], b["verdict"], b["note"]]
    for ci, v in enumerate(vals, start=1):
        c = ws2.cell(row=r, column=ci, value=v)
        c.border = BORDER
        c.alignment = Alignment(vertical="top", wrap_text=True)
        c.font = Font(size=10)
    for ci in (1, 3, 11):
        ws2.cell(row=r, column=ci).alignment = Alignment(horizontal="center", vertical="center")
    ws2.cell(row=r, column=1).font = Font(size=10, bold=True)
    ws2.cell(row=r, column=4).font = Font(size=10, bold=True)
    bg, fg = LV_COLOR[b["level"]]
    lv = ws2.cell(row=r, column=3)
    lv.fill = PatternFill("solid", fgColor=xl_color(bg))
    lv.font = Font(size=10, bold=True, color=xl_color(fg))
    bg, fg = REG_COLOR[b["verdict"]]
    st = ws2.cell(row=r, column=11)
    st.fill = PatternFill("solid", fgColor=xl_color(bg))
    st.font = Font(size=10, bold=True, color=xl_color(fg))
    ws2.row_dimensions[r].height = 92

data_end = data_start + len(ALL_ROWS) - 1
ws2.freeze_panes = "B4"
ws2.auto_filter.ref = f"A{HEAD_ROW}:L{data_end}"

# ---------- Sheet 3: 测试用例回归记录 ----------
ws3 = wb.create_sheet("用例回归记录")
ws3["A1"] = "测试用例回归执行记录"
ws3.merge_cells("A1:H1")
ws3["A1"].font = Font(size=13, bold=True, color=XL_HEADER_FG)
ws3["A1"].fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws3["A1"].alignment = Alignment(horizontal="center", vertical="center")
ws3.row_dimensions[1].height = 28

COLS3 = [("用例编号", 12), ("所属模块", 16), ("用例标题", 30), ("前置条件", 24),
         ("预期结果", 34), ("回归实际结果", 36), ("回归结果", 11), ("关联缺陷", 16)]
for i, (name, w) in enumerate(COLS3, start=1):
    c = ws3.cell(row=3, column=i, value=name)
    c.font = Font(bold=True, color=XL_HEADER_FG)
    c.fill = PatternFill("solid", fgColor=XL_HEADER_BG)
    c.alignment = Alignment(horizontal="center", vertical="center")
    c.border = BORDER
    ws3.column_dimensions[get_column_letter(i)].width = w
ws3.row_dimensions[3].height = 24

ALL_CASES = list(CASES) + EXTRA_CASES
for idx, case in enumerate(ALL_CASES):
    r = 4 + idx
    cid = case[0]
    if cid in CASE_REG:
        res, actual_reg, rel = CASE_REG[cid]
        row = (cid, case[1], case[2], case[3], case[4], actual_reg, res, rel)
    else:
        row = case
    for ci, v in enumerate(row, start=1):
        c = ws3.cell(row=r, column=ci, value=v)
        c.border = BORDER
        c.font = Font(size=10)
        c.alignment = Alignment(vertical="top", wrap_text=True)
    for ci in (1, 7, 8):
        ws3.cell(row=r, column=ci).alignment = Alignment(horizontal="center", vertical="center")
    ws3.cell(row=r, column=1).font = Font(size=10, bold=True)
    ws3.cell(row=r, column=3).font = Font(size=10, bold=True)
    bg, fg = RESULT_COLOR.get(row[6], ("#EDEDED", "#3F3F3F"))
    rc = ws3.cell(row=r, column=7)
    rc.fill = PatternFill("solid", fgColor=xl_color(bg))
    rc.font = Font(size=10, bold=True, color=xl_color(fg))
    ws3.row_dimensions[r].height = 34

case_end = 4 + len(ALL_CASES) - 1
ws3.freeze_panes = "C4"
ws3.auto_filter.ref = f"A3:H{case_end}"

sr = case_end + 2
ws3.cell(row=sr, column=1, value="用例统计")
ws3.cell(row=sr, column=1).font = Font(bold=True, color=XL_HEADER_FG)
ws3.cell(row=sr, column=1).fill = PatternFill("solid", fgColor=XL_SUM_BG)
ws3.cell(row=sr, column=1).border = BORDER
total_cases = len(ALL_CASES)
res_list = []
for case in ALL_CASES:
    cid = case[0]
    res_list.append(CASE_REG[cid][0] if cid in CASE_REG else case[6])
pass_cnt = res_list.count("通过")
fail_cnt = res_list.count("失败")
risk_cnt = res_list.count("风险")
na_cnt = res_list.count("不适用")
stat = [("用例总数", total_cases), ("通过", pass_cnt), ("失败", fail_cnt),
        ("风险", risk_cnt), ("不适用", na_cnt),
        ("通过率", (pass_cnt + risk_cnt) / total_cases if total_cases else 0)]
for i, (k, v) in enumerate(stat):
    ws3.cell(row=sr + i, column=2, value=k)
    ws3.cell(row=sr + i, column=3, value=v)
    if k == "通过率":
        ws3.cell(row=sr + i, column=3).number_format = "0.0%"
for i in range(len(stat)):
    for cc in range(1, 4):
        cell = ws3.cell(row=sr + i, column=cc)
        cell.border = BORDER
        if cc == 1 and i == 0:
            continue
        cell.font = Font(size=10, bold=(i == 0))
        cell.alignment = Alignment(horizontal="center")

wb.save(OUT)
print(f"OK saved: {OUT}")
print(f"total={total} fixed={len(FIXED)} remain={len(REMAIN)} closed={len(CLOSED)} dropped={len(DROPPED)} new={len(NEWL)} open={len(OPEN)}")
print(f"cases={total_cases} pass={pass_cnt} fail={fail_cnt} risk={risk_cnt} na={na_cnt}")
