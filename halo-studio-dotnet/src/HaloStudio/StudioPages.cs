namespace HaloStudio;

public sealed partial class StudioForm
{
    void BuildDisplay()
    {
        var p = pages[0];
        var monitors = Card(p, "监控主题", "选择后显示电脑状态；也可在下方切回时钟。", fullWidth: true);
        monitors.Controls.Add(gallery); monitors.Controls.Add(monitorExpand);
        monitorExpand.Click += (_, _) => { monitorExpanded = !monitorExpanded; UpdateThemeGroups(); };
        var clocks = Card(p, "时钟主题", "只显示时钟，不切换系统监控；开启自动歌词后，播放音乐时仍会显示歌词。", fullWidth: true);
        clocks.Controls.Add(clockGallery); clocks.Controls.Add(clockExpand);
        clockExpand.Click += (_, _) => { clockExpanded = !clockExpanded; UpdateThemeGroups(); };
        var colors = Card(p, "主题配色", "色卡与预览依据实屏观感近似显示。整屏共色，每个主题独立保存。");
        colors.Controls.Add(colorStatus); colors.Controls.Add(colorChips); LoadColorChips();
        colors.Controls.Add(Row(Button("设备 RGB…", PickThemeColor), Button("恢复主题原色", () => SetThemeColor(null))));
        colors.Controls.Add(Hint("主题与校准快捷色按色相排列，白色置后。需要原厂色值时，展开下方 TempoHub 配色。"));
        colors.Controls.Add(Row(Button("打开配色文件夹", () => OpenFolder(Path.GetDirectoryName(ScreenPalette.FilePath)!)), Button("重新加载色表", ReloadColors)));
        Setting(colors, "白色偏蓝校正", "仅用于主题／校准白色；官方配色不受影响，255 为关闭", Number("白色蓝色分量", 0, 255, () => current.ScreenWhiteBlue, v => Set(current with { ScreenWhiteBlue = (int)v })));
        colors.Controls.Add(officialExpand);
        officialContent.AutoSize = true; officialContent.AutoSizeMode = AutoSizeMode.GrowAndShrink; officialContent.BackColor = Palette.Card;
        officialContent.Controls.Add(Hint("30 个原厂预设，直接发送原始 RGB，不应用白色校正。颜色按主题保存，实屏可能有色差。"));
        officialContent.Controls.Add(officialChips);
        officialContent.Controls.Add(Button("选择官方 RGB…", () => PickThemeColor(true)));
        officialContent.SizeChanged += (_, _) =>
        {
            int w = officialContent.ClientSize.Width;
            officialChips.MaximumSize = new(w, 0); officialChips.Width = w;
            foreach (var label in officialContent.Controls.OfType<Label>()) label.MaximumSize = new(w, 0);
        };
        colors.Controls.Add(officialContent); officialContent.Visible = false;
        officialExpand.Click += (_, _) => SetOfficialExpanded(!officialExpanded);

        var daily = Card(p, "显示内容", "决定平时显示什么，以及音乐播放时是否切换歌词。");
        Setting(daily, "接管音响屏幕", "关闭后恢复原厂时钟", Toggle("接管音响屏幕", () => current.Enabled, v => Set(current with { Enabled = v })));
        Setting(daily, "空闲时显示", "监控主题支持两种内容；纯时钟主题始终显示时钟", Combo("空闲时显示", [("clock", "时钟"), ("monitor", "系统监控")], () => current.MonitorEnabled ? "monitor" : "clock", v => Set(current with { MonitorEnabled = v == "monitor" })));
        Setting(daily, "音乐播放时", "支持网易云音乐和 Windows 版 Apple Music", Combo("音乐播放时", [("auto", "自动显示歌词"), ("idle", "保持时钟或监控")], () => current.Mode, v => Set(current with { Mode = v })));
        daily.Controls.Add(Hint("歌词结束后回到时钟或监控；驾驶遥测收到游戏数据时优先显示游戏仪表。"));

        var motion = Card(p, "画面样式与动效", "调整静态外观和换页效果，不改变显示内容。");
        Setting(motion, "使用主题排版", "关闭后使用简洁布局，保留当前内容与颜色", Toggle("使用主题排版", () => current.Themes, v => Set(current with { Themes = v })));
        Setting(motion, "时钟显示秒数", "仅影响支持秒数的时钟布局", Toggle("时钟显示秒数", () => current.Seconds, v => Set(current with { Seconds = v })));
        Setting(motion, "图案动态效果", "让主题装饰缓慢变化；关闭后保留静态图案", Toggle("图案动态效果", () => current.Animations, v => Set(current with { Animations = v })));
        Setting(motion, "换页过渡动画", "用于歌词换句、监控轮换和主题切换", Toggle("换页过渡动画", () => current.Transitions, v => Set(current with { Transitions = v })));
        Setting(motion, "内容换页效果", "开启过渡后生效；切换主题时自动使用展开或溶解", Combo("内容换页效果", [("up", "向上推入"), ("wipe", "横向揭幕"), ("center", "中心展开"), ("dissolve", "像素溶解")], () => current.PixelTransition, v => Set(current with { PixelTransition = v })));

    }
    void BuildMonitor()
    {
        var p = pages[1]; var cpu = Card(p, "CPU 温度", "独立按需采集，优先复用已加载的硬件驱动。");
        cpu.Controls.Add(thermal);
        Setting(cpu, "独立温度采集", "首次默认关闭；首次开启需确认一次管理员授权，之后开机和手动启动均复用授权。", Toggle("独立温度采集", () => current.CpuTemperatureEnabled, v => { if (v) { engine.EnableTemperature(); current = engine.Config; Sync(); } else Set(current.WithTemperatureChoice(false)); }));
        cpu.Controls.Add(Row(Button("授权并重试", () => { engine.EnableTemperature(); current = engine.Config; Sync(); }, true)));
        cpu.Controls.Add(Hint("主程序保持普通权限。后台任务只按需启动固定温度采集器，退出即停止；程序位置变化或授权失效时需重新设置。"));
        var layout = Card(p, "显示编排");
        Setting(layout, "监控布局", "紧凑信息或单项图形仪表", Combo("监控布局", [("dense", "紧凑两行 · 八项状态"), ("compact", "双指标卡片"), ("value", "数值与图形仪表"), ("bar", "分段负载仪表"), ("graph", "历史曲线")], () => current.MonitorView, v => Set(current with { MonitorView = v })));
        Setting(layout, "双指标横向轮换", "横向切换时生效；整对模式优先同类配对", Combo("双指标横向轮换", [("pair", "每次一对 · 同类优先"), ("single", "每次一个 · 逐个接续")], () => current.PairAdvance, v => Set(current with { PairAdvance = v })));
        Setting(layout, "轮换指标", "按选择顺序轮流显示", Toggle("轮换指标", () => current.MonitorRotate, v => Set(current with { MonitorRotate = v })));
        Setting(layout, "轮换间隔", "单位：秒", Number("轮换间隔", 2, 120, () => current.RotationSeconds, v => Set(current with { RotationSeconds = (int)v })));
        layout.Controls.Add(Hint("网速按活动物理网卡统计，避免 VPN 重复计数；单位为字节/秒，屏幕 K/M 对应 KiB/s、MiB/s（1024 进制），不是 Mbps。"));
        var selected = Card(p, "显示指标", "勾选后立即生效；紧凑两行布局会用所选指标替换主题槽位。同类优先，余项就近配对。", maxHeight: 480); selected.Controls.Add(metrics);
        metrics.SelectionChanged += () => { if (!loading) Set(current with { MonitorMetrics = metrics.SelectedKeys, MonitorMetricsCustom = true }); };
        selected.Controls.Add(Button("应用指标选择", () => Set(current with { MonitorMetrics = metrics.SelectedKeys, MonitorMetricsCustom = true })));
        var fans = Card(p, "风扇额定转速", "输入额定最大 RPM，用于计算相对转速百分比。"); fanKey.PlaceholderText = "传感器键，例如 gpu0_fan0"; fanMax.PlaceholderText = "最大 RPM";
        Setting(fans, "风扇传感器", "使用显示指标中的传感器键", new InputSurface(fanKey, 256));
        Setting(fans, "额定最大转速", "单位：RPM", new InputSurface(fanMax, 160));
        void SaveFanIfValid() { string key = fanKey.Text.Trim(); if (key.Length > 0 && double.TryParse(fanMax.Text, out double max) && double.IsFinite(max) && max > 0 && current.FanLimits.GetValueOrDefault(key) != max) Set(current with { FanLimits = new(current.FanLimits) { [key] = max } }); }
        fanKey.Validated += (_, _) => SaveFanIfValid(); fanMax.Validated += (_, _) => SaveFanIfValid();
        fans.Controls.Add(Button("保存额定转速", () => { if (double.TryParse(fanMax.Text, out double max) && double.IsFinite(max) && max > 0 && fanKey.Text.Trim().Length > 0) Set(current with { FanLimits = new(current.FanLimits) { [fanKey.Text.Trim()] = max } }); else MessageBox.Show(this, "请输入传感器键和大于 0 的额定转速。", "风扇设置"); }));
        var live = Card(p, "实时硬件状态", "当前数值与采集来源", fullWidth: true); live.Controls.Add(sensors);
    }
    void BuildMusic()
    {
        var p = pages[2]; var now = Card(p, "正在聆听", fullWidth: true); now.Controls.Add(musicTitle); now.Controls.Add(lyric); now.Controls.Add(musicDetail);
        var lrc = Card(p, "歌词同步", "支持网易云音乐与 Windows 版 Apple Music。自动识别正在播放的播放器，无需切换来源。");
        lrc.Controls.Add(Hint("Apple Music 通过 Windows 系统媒体控制提供歌曲和进度，再联网匹配歌词；不读取 Apple Music 自带歌词。未匹配到时可导入 LRC。"));
        Setting(lrc, "联网匹配歌词", "通过网易云／LRCLIB 匹配歌词；也可使用缓存或本地 LRC", Toggle("联网匹配歌词", () => current.Online, v => Set(current with { Online = v })));
        Setting(lrc, "刷新延迟补偿", "默认提前 0.8 秒，可与手动偏移叠加", Number("刷新延迟补偿", 0, 3, () => (decimal)current.LyricLeadSeconds, v => Set(current with { LyricLeadSeconds = (double)v }), 1));
        Setting(lrc, "时间轴偏移", "秒；正值再提前，负值延后。Enter 或点击空白处应用", Number("时间轴偏移", -30, 30, () => (decimal)current.Offset, v => Set(current with { Offset = (double)v }), 1)); lrc.Controls.Add(Button("导入 LRC 文件", ImportLrc));
        lrc.Controls.Add(Hint("有逐字／逐词时间戳时，按主题宽度分成短句并准时换页；否则仅显示开头… → …结尾，预留传输耗时让结尾至少占半程。未知时间轴时尽早显示结尾。"));
        var light = Card(p, "音乐氛围灯");
        Setting(light, "氛围灯", "控制音响两侧灯光", Toggle("氛围灯", () => current.AmbientEnabled, v => Set(current with { AmbientEnabled = v })));
        Setting(light, "跟随音乐强弱", "目前采样网易云音频峰值，音量固定也能律动；Apple Music 暂不支持此项", Toggle("跟随音乐强弱", () => current.Rhythm, v => Set(current with { Rhythm = v })));
        Setting(light, "律动方式", "主题配色同步作为律动主色", Combo("律动方式", [("pulse", "连续能量起伏"), ("color", "主题双色变化"), ("beat", "节拍脉冲")], () => current.RhythmStyle, v => Set(current with { RhythmStyle = v })));
        Setting(light, "动态对比", "提高数值会增强明暗差异", Number("动态对比", .4m, 6, () => (decimal)current.RhythmGain, v => Set(current with { RhythmGain = (double)v }), 1));
        var ambient = Card(p, "基础灯效");
        Setting(ambient, "效果", "音乐律动关闭时使用", Combo("基础灯效", [("static", "常亮"), ("breathing", "呼吸"), ("color_tide", "彩潮"), ("ripple", "涟漪"), ("flow", "流光"), ("dynamic", "动态")], () => current.Effect, v => Set(current with { Effect = v })));
        Setting(ambient, "亮度", "两侧氛围灯亮度", Combo("灯光亮度", [("low", "低"), ("medium", "中"), ("high", "高")], () => current.Brightness, v => Set(current with { Brightness = v })));
        Setting(ambient, "速度", "基础灯效的变化节奏", Number("灯效速度", 1, 10, () => current.Speed, v => Set(current with { Speed = (int)v })));
        ambient.Controls.Add(Button("选择基础灯色…", () => { using var d = new ColorDialog { FullOpen = true, Color = ColorTranslator.FromHtml(current.AmbientColor) }; if (d.ShowDialog(this) == DialogResult.OK) Set(current with { AmbientColor = $"#{d.Color.R:X2}{d.Color.G:X2}{d.Color.B:X2}" }); }));
    }
    void BuildDemo()
    {
        var p = pages[3]; var hero = Card(p, "星际来信", "一段 59 秒的像素短片"); hero.Controls.Add(Label("让代码落下，让光留下。", 18, true));
        hero.Controls.Add(Hint("32 列绿色字符错速下落，显露 HALO STUDIO。走过六套主题后，以四句原创歌词展现整屏推入与揭幕。"));
        hero.Controls.Add(Row(Button("▶  开始演示", StartDemo, true), Button("停止并回到日常", engine.StopShowcase))); hero.Controls.Add(demoStatus); hero.Controls.Add(demoProgress);
        var playlist = Card(p, "演出单");
        Setting(playlist, "01   代码之雨", "MCU 本地字符流星与标题揭幕", Label("10 秒", 9, false, Palette.Muted));
        Setting(playlist, "02   六种设计语言", "每个主题选取一个代表场景", Label("30 秒", 9, false, Palette.Muted));
        Setting(playlist, "03   原创歌词", "逐句推入、横向揭幕与结束画面", Label("19 秒", 9, false, Palette.Muted));
        playlist.Controls.Add(Hint("演示使用模拟监控数值；结束后继续当前歌词或默认显示。屏幕预览显示目标帧。")); playlist.Controls.Add(Button("打开演示编排文件夹", () => OpenFolder(Path.Combine(AppContext.BaseDirectory, "demos"))));
    }
    void BuildDevice()
    {
        var p = pages[4]; var run = Card(p, "应用设置"); var boot = new ToggleSwitch { AccessibleName = "开机后台启动", Checked = Autostart.Enabled }; boot.CheckedChanged += (_, _) => Safe(() => Autostart.Set(boot.Checked));
        Setting(run, "开机后台启动", "登录 Windows 后自动运行 HaloStudio", boot);
        Setting(run, "资源配置", "隐藏窗口时暂停界面绘制", Combo("资源配置", [("eco", "节能 · 音频采样 20 Hz"), ("balanced", "均衡 · 音频采样 30 Hz")], () => current.PowerProfile, v => Set(current with { PowerProfile = v })));
        var device = Card(p, "音响连接", "已验证的 MCU RAM 直绘、压缩与差分传输。"); device.Controls.Add(Row(Button("导入 MCU 验证记录", ImportDevice), Button("打开运行目录", () => OpenFolder(runtime))));
        var files = Card(p, "创作与资源", "主题和演示在运行时读取，新增主题无需重新编译。"); files.Controls.Add(Row(Button("重新加载主题", ReloadThemes), Button("主题文件夹", () => OpenFolder(Designs.DirectoryPath)), Button("演示文件夹", () => OpenFolder(Path.Combine(AppContext.BaseDirectory, "demos")))));
        var about = Card(p, "HaloStudio", "Halo PixelBar 屏幕与灯光控制"); about.Controls.Add(Hint("关闭窗口会隐藏到托盘。退出或切换旧版时先释放显示权，恢复原厂时钟。")); about.Controls.Add(Button("退出并恢复原厂显示", async () => await QuitAsync()));
        var legacy = new LinkLabel { Text = "切换到旧版本", AutoSize = true, Font = Palette.Font(8), LinkColor = Palette.Muted, ActiveLinkColor = Palette.Accent, Margin = new(0, 16, 0, 4) };
        legacy.LinkClicked += async (_, _) => await SwitchLegacyAsync(); about.Controls.Add(legacy);
    }

    static FlowLayoutPanel Stack() => new() { FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new(0) };
    static Label Label(string text, float size, bool bold = false, Color? color = null) => new() { Text = text, AutoSize = true, Font = Palette.Font(size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color ?? Palette.Text, Margin = new(0, 0, 0, 6) };
    static Label Hint(string text) { var l = Label(text, 9, false, Palette.Muted); l.MaximumSize = new(700, 0); l.Margin = new(0, 4, 0, 12); return l; }
    static void ResizePage(PagePanel page) => page.PerformLayout();
    static FlowLayoutPanel Card(PagePanel page, string title, string? detail = null, bool fullWidth = false, int maxHeight = 0)
    {
        var card = new StudioCard { Width = 850, Height = 100, FullWidth = fullWidth, AccessibleName = title }; var stack = Stack(); stack.BackColor = Palette.Card;
        stack.Controls.Add(Label(title, 11, true)); if (detail != null) stack.Controls.Add(Hint(detail)); card.Controls.Add(stack); page.Controls.Add(card);
        bool arranging = false;
        void Arrange()
        {
            if (arranging) return; arranging = true;
            try {
                int w = Math.Max(200, card.ClientSize.Width - card.Padding.Horizontal); stack.Location = new(card.Padding.Left, card.Padding.Top); stack.Width = w;
                foreach (Control row in stack.Controls) { if (row is SettingRow or SensorTable or MetricPicker or ThemeGallery) row.Width = w; else if (row is FlowLayoutPanel flow) { flow.MaximumSize = new(w, 0); flow.Width = w; } else if (row is Label) row.MaximumSize = new(w, 0); else if (row is ProgressBar) row.Width = w; }
                stack.PerformLayout();
                if (maxHeight > 0 && stack.Controls.OfType<MetricPicker>().FirstOrDefault() is { } picker)
                {
                    int fixedHeight = stack.GetPreferredSize(new Size(w, 0)).Height - picker.Height + card.Padding.Vertical;
                    picker.SetViewportLimit(Math.Max(Palette.Px(card, 56), Palette.Px(card, maxHeight) - fixedHeight));
                    stack.PerformLayout();
                }
                stack.Height = stack.GetPreferredSize(new Size(w, 0)).Height; card.Height = stack.Height + card.Padding.Vertical;
            } finally { arranging = false; }
        }
        card.Tag = (Action)Arrange; card.SizeChanged += (_, _) => Arrange(); stack.ControlAdded += (_, e) => { if(e.Control != null) e.Control.SizeChanged += (_, _) => Arrange(); Arrange(); }; Arrange();
        return stack;
    }
    static void Setting(FlowLayoutPanel card, string title, string detail, Control control)
    {
        card.Controls.Add(new SettingRow(title, detail, control));
    }
    static FlowLayoutPanel Row(params Control[] controls) { var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new(0, 7, 0, 2) }; row.Controls.AddRange(controls); return row; }
    static ModernButton Button(string text, Action action, bool primary = false) { var b = new ModernButton { Text = text, Width = Math.Max(120, text.Length * 16 + 32), Height = 42, Primary = primary, ForeColor = Palette.Text, Margin = new(0, 5, 10, 5) }; b.Click += (_, _) => action(); return b; }
    ToggleSwitch Toggle(string title, Func<bool> get, Action<bool> set) { var c = new ToggleSwitch { AccessibleName = title }; bindings.Add(() => c.Checked = get()); c.CheckedChanged += (_, _) => { if (!loading) set(c.Checked); }; return c; }
    Control Combo(string title, (string Key, string Name)[] options, Func<string> get, Action<string> set) { var c = new StudioCombo { AccessibleName = title }; foreach (var p in options) c.Options.Add(new(p.Key, p.Name)); bindings.Add(() => c.Value = get()); c.ValueChanged += (_, _) => { if (!loading) set(c.Value); }; return c; }

    Control Number(string title, decimal min, decimal max, Func<decimal> get, Action<decimal> set, int places = 0) { var c = new CommitNumber { Minimum = min, Maximum = max, DecimalPlaces = places, Increment = places > 0 ? .1m : 1, BackColor = Palette.Input, ForeColor = Palette.Text, BorderStyle = BorderStyle.None, Width = 130, AccessibleName = title }; bindings.Add(() => { if (!c.ContainsFocus) c.Value = Math.Clamp(get(), min, max); }); c.ValueChanged += (_, _) => { if (!loading) set(c.Value); }; return new InputSurface(c, 160); }
    static TextBox Input(int width) => new() { Width = width, BackColor = Palette.Input, ForeColor = Palette.Text, BorderStyle = BorderStyle.FixedSingle, Margin = new(0, 5, 12, 5) };
}
