using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using AINovelWriter.Models;
using AINovelWriter.Services;
using AINovelWriter.ViewModels;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AINovelWriter.Views;

public sealed partial class EditorPage : Page
{
    public EditorViewModel Vm { get; }

    // 侧边栏状态
    private double _leftWidth = 270, _rightWidth = 360;
    private bool _leftCollapsed, _rightCollapsed;

    // 分隔条拖拽
    private Border? _activeSplitter;
    private bool _dragging;
    private double _dragStartX, _dragStartWidth;

    // 对话框编辑目标
    private Character? _editingCharacter;
    private WorldSetting? _editingWorld;
    private Action? _confirmAction;

    // 正文右键菜单中的 AI 命令项（用于按选区动态启用）
    private readonly List<(MenuFlyoutItem Item, AIBlockKind Kind)> _aiContextCommands = new();

    public EditorPage()
    {
        Vm = new EditorViewModel();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ChaptersList.SelectedItem = Vm.CurrentChapter;
        CharacterDialog.XamlRoot = this.Content.XamlRoot;
        WorldDialog.XamlRoot = this.Content.XamlRoot;
        ConfirmDialog.XamlRoot = this.Content.XamlRoot;
        // 填充世界观类型下拉
        WorldType.ItemsSource = new[] { "世界规则", "势力", "地点", "时间线", "特殊设定" };
        // 构建正文右键菜单（竖排 AI 命令 + 自定义修改意见）
        BuildEditorContextMenu();
    }

    // ================= 章节 =================
    private void OnChapterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChaptersList.SelectedItem is Chapter ch && ch != Vm.CurrentChapter)
            Vm.SelectChapter(ch);
    }

    private void OnAddChapter(object sender, RoutedEventArgs e) => Vm.AddChapter();

    private void OnDeleteChapter(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Chapter ch)
        {
            _confirmAction = () => Vm.DeleteChapter(ch);
            ConfirmText.Text = $"确定删除章节「{ch.Title}」吗？此操作不可撤销。";
            _ = ConfirmDialog.ShowAsync();
        }
    }

    // ================= 文本选区 =================
    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            Vm.SelectionStart = tb.SelectionStart;
            Vm.SelectionLength = tb.SelectionLength;
            Vm.CursorPosition = tb.SelectionStart + tb.SelectionLength;
            Vm.SelectedText = tb.SelectedText ?? "";
        }

        if (Vm.HasSelection)
        {
            SelectionToolbar.Visibility = Visibility.Visible;
            DispatcherQueue?.TryEnqueue(PositionSelectionToolbar);
        }
        else
        {
            SelectionToolbar.Visibility = Visibility.Collapsed;
        }
    }

    private void OnEditorScrolled(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (SelectionToolbar.Visibility == Visibility.Visible)
            DispatcherQueue?.TryEnqueue(PositionSelectionToolbar);
    }

    private void PositionSelectionToolbar()
    {
        if (Vm.CurrentChapter == null || SelectionToolbar.Visibility != Visibility.Visible) return;
        try
        {
            var rect = EditorBox.GetRectFromCharacterIndex(Vm.SelectionStart, false);
            var point = EditorBox.TransformToVisual(EditorBodyGrid).TransformPoint(new Point(rect.X, rect.Y));
            double y = point.Y - SelectionToolbar.ActualHeight - 8;
            if (y < 2) y = 2;
            SelectionToolbar.Margin = new Thickness(point.X, y, 0, 0);
        }
        catch { /* 布局未完成时忽略 */ }
    }

    private void OnToolbarCommand(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is AIBlockKind kind)
            ShowInstructionFlyout(fe, kind);
    }

    private void OnHideToolbar(object sender, RoutedEventArgs e) => SelectionToolbar.Visibility = Visibility.Collapsed;

    // ================= AI 命令（含修改意见输入） =================
    private void OnContinueAtCursor(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) ShowInstructionFlyout(fe, AIBlockKind.Continue);
    }

    private void OnQuickCommandClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is QuickCommandItem item)
        {
            QuickCmdFlyout.Hide();
            ShowInstructionFlyout(QuickCmdGrid, item.Kind);
        }
    }

    private void ShowInstructionFlyout(FrameworkElement anchor, AIBlockKind kind, bool customOpinion = false)
    {
        if (SelectionToolbar.Visibility == Visibility.Visible)
            SelectionToolbar.Visibility = Visibility.Collapsed;

        var header = customOpinion
            ? "你的修改意见（AI 将据此改写选中的这一段）："
            : $"AI 「{new AIBlock { Kind = kind }.KindLabel}」";
        var placeholder = customOpinion
            ? "例如：语气更阴沉一些 / 增加打斗细节 / 保留首句只改后半段 / 换个更口语的说法…"
            : "可附加修改意见（可选）：语气更阴沉 / 增加打斗细节 / 保留首句…";

        var tb = new TextBox
        {
            PlaceholderText = placeholder,
            Width = 340, Height = 88, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            Header = header
        };

        // 先声明 flyout，避免 lambda 在声明前使用
        Flyout flyout = null!;

        var gen = new Button { Content = "生成", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        gen.Click += (_, _) => { flyout.Hide(); _ = Vm.RunQuickCommandAsync(kind, string.IsNullOrWhiteSpace(tb.Text) ? null : tb.Text); };
        var cancel = new Button { Content = "取消" };
        cancel.Click += (_, _) => flyout.Hide();

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(cancel);
        row.Children.Add(gen);

        var panel = new StackPanel { Spacing = 10, Padding = new Thickness(14) };
        panel.Children.Add(tb);
        panel.Children.Add(row);

        flyout = new Flyout { Content = panel, XamlRoot = this.Content.XamlRoot };
        flyout.ShowAt(anchor);
    }

    // ================= 正文右键菜单（竖排 AI 命令） =================
    private void BuildEditorContextMenu()
    {
        EditorContextFlyout.Items.Clear();
        _aiContextCommands.Clear();

        // 保留原生编辑能力：撤销 / 剪切 / 复制 / 粘贴 / 全选
        var undo = new MenuFlyoutItem { Text = "撤销" };
        undo.Click += (_, _) => EditorBox.Undo();
        var cut = new MenuFlyoutItem { Text = "剪切" };
        cut.Click += (_, _) => EditorBox.CutSelectionToClipboard();
        var copy = new MenuFlyoutItem { Text = "复制" };
        copy.Click += (_, _) => EditorBox.CopySelectionToClipboard();
        var paste = new MenuFlyoutItem { Text = "粘贴" };
        paste.Click += (_, _) => EditorBox.PasteFromClipboard();
        var selectAll = new MenuFlyoutItem { Text = "全选" };
        selectAll.Click += (_, _) => EditorBox.SelectAll();

        EditorContextFlyout.Items.Add(undo);
        EditorContextFlyout.Items.Add(cut);
        EditorContextFlyout.Items.Add(copy);
        EditorContextFlyout.Items.Add(paste);
        EditorContextFlyout.Items.Add(selectAll);
        EditorContextFlyout.Items.Add(new MenuFlyoutSeparator());

        // AI 命令（竖排列表）
        foreach (var cmd in Vm.QuickCommands)
        {
            var mi = new MenuFlyoutItem
            {
                Text = "AI · " + cmd.Label,
                Icon = new FontIcon { Glyph = cmd.Glyph, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 0x7C, 0x3A, 0xED)) }
            };
            mi.Click += (_, _) => ShowInstructionFlyout(EditorBox, cmd.Kind);
            EditorContextFlyout.Items.Add(mi);
            _aiContextCommands.Add((mi, cmd.Kind));
        }
        EditorContextFlyout.Items.Add(new MenuFlyoutSeparator());

        // 用户给提示：选中一段，自定义意见让 AI 改写
        var custom = new MenuFlyoutItem { Text = "给这段提修改意见…" };
        custom.Click += (_, _) => ShowInstructionFlyout(EditorBox, AIBlockKind.Rewrite, true);
        EditorContextFlyout.Items.Add(custom);
    }

    private void OnEditorContextFlyoutOpening(object sender, object e)
    {
        bool hasSel = Vm.HasSelection;
        foreach (var (item, kind) in _aiContextCommands)
            item.IsEnabled = kind == AIBlockKind.Continue || hasSel;
    }

    // ================= 生成块操作 =================
    private void OnInsertBlock(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AIBlock b) Vm.InsertBlock(b);
    }
    private async void OnContinueBlock(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AIBlock b) await Vm.ContinueBlockAsync(b);
    }
    private async void OnRegenerateBlock(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AIBlock b) await Vm.RegenerateBlockAsync(b);
    }
    private void OnDiscardBlock(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AIBlock b) Vm.DiscardBlock(b);
    }
    private void OnRestoreHistory(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AIHistoryRecord r) Vm.RestoreHistory(r);
    }

    // ================= 侧边栏折叠 / 拖拽 =================
    private void OnToggleLeft(object sender, RoutedEventArgs e)
    {
        _leftCollapsed = !_leftCollapsed;
        if (_leftCollapsed)
        {
            LeftCol.Width = new GridLength(26);
            LeftSplitter.Visibility = Visibility.Collapsed;
            LeftPanel.Visibility = Visibility.Collapsed;
            LeftExpandBtn.Visibility = Visibility.Visible;
        }
        else
        {
            LeftCol.Width = new GridLength(_leftWidth, GridUnitType.Pixel);
            LeftSplitter.Visibility = Visibility.Visible;
            LeftPanel.Visibility = Visibility.Visible;
            LeftExpandBtn.Visibility = Visibility.Collapsed;
        }
    }

    private void OnToggleRight(object sender, RoutedEventArgs e)
    {
        _rightCollapsed = !_rightCollapsed;
        if (_rightCollapsed)
        {
            RightCol.Width = new GridLength(26);
            RightSplitter.Visibility = Visibility.Collapsed;
            RightPanel.Visibility = Visibility.Collapsed;
            RightExpandBtn.Visibility = Visibility.Visible;
        }
        else
        {
            RightCol.Width = new GridLength(_rightWidth, GridUnitType.Pixel);
            RightSplitter.Visibility = Visibility.Visible;
            RightPanel.Visibility = Visibility.Visible;
            RightExpandBtn.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSplitterPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border b) return;
        _activeSplitter = b;
        _dragging = true;
        _dragStartX = e.GetCurrentPoint(MainGrid).Position.X;
        _dragStartWidth = b == LeftSplitter ? LeftCol.Width.Value : RightCol.Width.Value;
        b.CapturePointer(e.Pointer);
    }

    private void OnSplitterMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging || _activeSplitter == null) return;
        var x = e.GetCurrentPoint(MainGrid).Position.X;
        var delta = x - _dragStartX;
        if (_activeSplitter == LeftSplitter)
        {
            var w = Math.Clamp(_dragStartWidth + delta, 200, 520);
            LeftCol.Width = new GridLength(w, GridUnitType.Pixel);
            _leftWidth = w;
        }
        else
        {
            var w = Math.Clamp(_dragStartWidth - delta, 260, 560);
            RightCol.Width = new GridLength(w, GridUnitType.Pixel);
            _rightWidth = w;
        }
    }

    private void OnSplitterReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        _activeSplitter = null;
        if (sender is Border b && e.Pointer != null)
        {
            try { b.ReleasePointerCapture(e.Pointer); } catch { }
        }
    }

    private void OnSplitterEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging && sender is Border b)
            b.Background = new SolidColorBrush(ColorHelper.FromArgb(40, 0x7C, 0x3A, 0xED));
    }
    private void OnSplitterExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging && sender is Border b)
            b.Background = new SolidColorBrush(Colors.Transparent);
    }

    // ================= 人物编辑 =================
    private void OnAddCharacter(object sender, RoutedEventArgs e)
    {
        _editingCharacter = null;
        CharName.Text = ""; CharRole.Text = ""; CharAlias.Text = ""; CharTags.Text = ""; CharNotes.Text = "";
        CharacterDialog.Title = "新增角色";
        _ = CharacterDialog.ShowAsync();
    }

    private void OnEditCharacter(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Character c)
        {
            _editingCharacter = c;
            CharName.Text = c.Name; CharRole.Text = c.Role; CharAlias.Text = c.Alias; CharTags.Text = c.PersonalityTags;
            // 优先显示自由填写的 Notes；若为空但旧字段有内容，则拼出预览，避免旧数据丢失
            CharNotes.Text = ComposeNotesPreview(c);
            CharacterDialog.Title = "编辑角色";
            _ = CharacterDialog.ShowAsync();
        }
    }

    /// <summary>优先返回 Notes；为空时把旧版细分字段拼成可读预览。</summary>
    private static string ComposeNotesPreview(Character c)
    {
        if (!string.IsNullOrWhiteSpace(c.Notes)) return c.Notes;
        var parts = new List<string>();
        void Add(string label, string val) { if (!string.IsNullOrWhiteSpace(val)) parts.Add($"{label}：{val}"); }
        Add("口头禅", c.Catchphrase);
        Add("外貌", c.Appearance);
        Add("背景故事", c.Background);
        Add("能力/技能", c.Abilities);
        Add("动机", c.Motivation);
        return string.Join("\n", parts);
    }

    private void OnDeleteCharacter(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Character c)
        {
            _confirmAction = () => Vm.Characters.Remove(c);
            ConfirmText.Text = $"确定删除角色「{c.Name}」吗？此操作不可撤销。";
            _ = ConfirmDialog.ShowAsync();
        }
    }

    private void OnCharacterDialogSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = string.IsNullOrWhiteSpace(CharName.Text) ? "未命名角色" : CharName.Text.Trim();
        if (_editingCharacter == null)
        {
            Vm.Characters.Add(new Character
            {
                Name = name, Role = CharRole.Text, Alias = CharAlias.Text, PersonalityTags = CharTags.Text,
                Notes = CharNotes.Text
            });
        }
        else
        {
            _editingCharacter.Name = name; _editingCharacter.Role = CharRole.Text; _editingCharacter.Alias = CharAlias.Text;
            _editingCharacter.PersonalityTags = CharTags.Text; _editingCharacter.Notes = CharNotes.Text;
        }
        _editingCharacter = null;
    }

    // ================= 世界观编辑 =================
    private void OnAddWorld(object sender, RoutedEventArgs e)
    {
        _editingWorld = null;
        WorldType.SelectedIndex = 0; WorldTitle.Text = ""; WorldContent.Text = ""; WorldLocked.IsChecked = true;
        WorldDialog.Title = "新增设定";
        _ = WorldDialog.ShowAsync();
    }

    private void OnEditWorld(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is WorldSetting w)
        {
            _editingWorld = w;
            WorldType.SelectedIndex = (int)w.Type; WorldTitle.Text = w.Title; WorldContent.Text = w.Content;
            WorldLocked.IsChecked = w.IsLocked;
            WorldDialog.Title = "编辑设定";
            _ = WorldDialog.ShowAsync();
        }
    }

    private void OnDeleteWorld(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is WorldSetting w)
        {
            _confirmAction = () => Vm.WorldSettings.Remove(w);
            ConfirmText.Text = $"确定删除设定「{w.Title}」吗？此操作不可撤销。";
            _ = ConfirmDialog.ShowAsync();
        }
    }

    private void OnWorldDialogSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var title = string.IsNullOrWhiteSpace(WorldTitle.Text) ? "新设定" : WorldTitle.Text.Trim();
        var type = WorldType.SelectedIndex >= 0 ? (WorldSettingType)WorldType.SelectedIndex : WorldSettingType.Rule;
        if (_editingWorld == null)
        {
            Vm.WorldSettings.Add(new WorldSetting
            {
                Type = type, Title = title, Content = WorldContent.Text,
                IsLocked = WorldLocked.IsChecked == true
            });
        }
        else
        {
            _editingWorld.Type = type; _editingWorld.Title = title; _editingWorld.Content = WorldContent.Text;
            _editingWorld.IsLocked = WorldLocked.IsChecked == true;
        }
        _editingWorld = null;
    }

    // ================= 通用确认 =================
    private void OnConfirmDialogPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _confirmAction?.Invoke();
        _confirmAction = null;
    }

    // ================= 顶部按钮 =================
    private void OnUndo(object sender, RoutedEventArgs e) => EditorBox.Undo();
    private void OnRedo(object sender, RoutedEventArgs e) => EditorBox.Redo();
    private void OnSettings(object sender, RoutedEventArgs e) => AppState.Navigate(typeof(SettingsPage));
    private void OnOpenPromptSettings(object sender, RoutedEventArgs e) => AppState.Navigate(typeof(SettingsPage));
    private void OnOutline(object sender, RoutedEventArgs e) => Vm.GoOutline();
    private void OnHome(object sender, RoutedEventArgs e) => Vm.GoHome();

    // ================= 保存 / 打开 =================
    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(AppState.CurrentProjectPath)) { OnSaveAs(sender, e); return; }
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(AppState.CurrentProjectPath);
            Vm.Save();
            AppState.ProjectService.SaveToFile(Vm.Project, file);
            Vm.StatusText = "已保存：" + AppState.CurrentProjectPath;
        }
        catch { OnSaveAs(sender, e); }
    }

    private async void OnSaveAs(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeChoices.Add("墨鸢小说项目", new[] { ".ainovel" });
        picker.SuggestedFileName = Vm.Project.Title;
        InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var file = await picker.PickSaveFileAsync();
        if (file == null) return;
        Vm.Save();
        AppState.ProjectService.SaveToFile(Vm.Project, file);
        AppState.CurrentProjectPath = file.Path;
        Vm.StatusText = "已另存为：" + file.Path;
    }

    private async void OnOpen(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".ainovel");
        InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var file = await picker.PickSingleFileAsync();
        if (file == null) return;
        var project = AppState.ProjectService.LoadFromFile(file);
        if (project == null) { Vm.StatusText = "打开失败：文件格式不正确"; return; }
        AppState.CurrentProject = project;
        AppState.CurrentProjectPath = file.Path;
        AppState.Navigate(typeof(EditorPage));
    }

    // ================= 联网搜索 =================
    private async void OnSearchWeb(object sender, RoutedEventArgs e) => await Vm.SearchWebAsync();

    private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            _ = Vm.SearchWebAsync();
    }

    private async void OnFetchPage(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is WebSearchResult result)
            await Vm.FetchSearchResultAsync(result);
    }

    // ================= 智能提取 =================
    private async void OnExtraction(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "智能设定提取",
            XamlRoot = this.Content.XamlRoot,
            PrimaryButtonText = "应用",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            MinWidth = 600
        };

        var textBox = new TextBox
        {
            PlaceholderText = "在此粘贴大量文本（世界观设定、人物介绍、剧情背景等）…\n\nAI 将自动分析并提取：\n  ① 人物名称、身份\n  ② 地点、场景\n  ③ 世界规则、势力、时间线",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 300,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var resultPanel = new StackPanel { Spacing = 6, Visibility = Visibility.Collapsed };

        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(textBox);
        stack.Children.Add(resultPanel);
        dialog.Content = stack;

        // 分析按钮
        var analyzeBtn = new Button
        {
            Content = "开始分析",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
            Margin = new Thickness(0, 0, 0, 8)
        };

        analyzeBtn.Click += (_, _) =>
        {
            var text = textBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var result = AppState.TextAnalysisService.Analyze(text);
            resultPanel.Children.Clear();
            resultPanel.Visibility = Visibility.Visible;

            // 人物
            var charHeader = new TextBlock
            {
                Text = $"人物（{result.Characters.Count} 个）",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 2)
            };
            resultPanel.Children.Add(charHeader);

            foreach (var ch in result.Characters.Take(10))
            {
                var cb = new CheckBox
                {
                    Content = $"{ch.Name}{(string.IsNullOrEmpty(ch.Role) ? "" : $"（{ch.Role}）")}",
                    IsChecked = true,
                    FontSize = 12
                };
                cb.Checked += (_, _) => ch.Selected = true;
                cb.Unchecked += (_, _) => ch.Selected = false;
                resultPanel.Children.Add(cb);
            }
            if (result.Characters.Count > 10)
            {
                resultPanel.Children.Add(new TextBlock
                {
                    Text = $"…及另外 {result.Characters.Count - 10} 个（将全部导入）",
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });
            }

            // 地点
            if (result.Locations.Count > 0)
            {
                resultPanel.Children.Add(new TextBlock
                {
                    Text = $"地点（{result.Locations.Count} 个）",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Margin = new Thickness(0, 6, 0, 2)
                });
                foreach (var loc in result.Locations)
                {
                    var cb = new CheckBox { Content = $"{loc.Name}（{loc.Category}）", IsChecked = true, FontSize = 12 };
                    cb.Checked += (_, _) => loc.Selected = true;
                    cb.Unchecked += (_, _) => loc.Selected = false;
                    resultPanel.Children.Add(cb);
                }
            }

            // 世界观
            if (result.WorldSettings.Count > 0)
            {
                resultPanel.Children.Add(new TextBlock
                {
                    Text = $"世界观设定（{result.WorldSettings.Count} 条）",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Margin = new Thickness(0, 6, 0, 2)
                });
                foreach (var ws in result.WorldSettings)
                {
                    var cb = new CheckBox { Content = $"[{ws.Type}] {ws.Title}", IsChecked = true, FontSize = 12 };
                    cb.Checked += (_, _) => ws.Selected = true;
                    cb.Unchecked += (_, _) => ws.Selected = false;
                    resultPanel.Children.Add(cb);
                }
            }

            analyzeBtn.IsEnabled = false;
            dialog.PrimaryButtonText = "导入到项目";

            Vm.StatusText = $"分析完成：{result.Characters.Count} 人物、{result.Locations.Count} 地点、{result.WorldSettings.Count} 世界观设定";
        };

        stack.Children.Insert(0, analyzeBtn);

        dialog.PrimaryButtonClick += (_, _) =>
        {
            // 用户点击"导入到项目"
            var text = textBox.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                var result = AppState.TextAnalysisService.Analyze(text);
                TextAnalysisService.ApplyToProject(Vm.Project, result);
                Vm.StatusText = $"已导入：{result.Characters.Count(c => c.Selected)} 人物、" +
                    $"{result.Locations.Count(l => l.Selected)} 地点、" +
                    $"{result.WorldSettings.Count(w => w.Selected)} 世界观设定";
                AppState.HasUnsavedChanges = true;
                AppState.AutoSaveService.MarkChanged();
            }
        };

        _ = dialog.ShowAsync();
    }

    // ================= 人物表格 =================
    private void OnCharactersTable(object sender, RoutedEventArgs e)
    {
        AppState.Navigate(typeof(CharactersPage));
    }
}
