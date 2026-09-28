using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AINovelWriter.Models;
using AINovelWriter.Services;
using AINovelWriter.ViewModels;

namespace AINovelWriter.Views;

/// <summary>
/// 人物表格视图页面 —— 以表格形式展示所有人物信息，支持内联编辑、新增、删除、详细编辑
/// </summary>
public sealed partial class CharactersPage : Page
{
    public ObservableCollection<Character> Characters { get; }

    private readonly HashSet<string> _selectedIds = new();
    private NovelProject? _project;

    public CharactersPage()
    {
        _project = AppState.CurrentProject;
        Characters = _project?.Characters ?? [];

        InitializeComponent();

        if (_project != null)
        {
            TitleText.Text = "人物表格 · " + _project.Title;
            SubtitleText.Text = $"共 {Characters.Count} 个人物";
            StatusText.Text = $"共 {Characters.Count} 个人物 · 可在表格中直接编辑";
        }

        // 监听字符变化以更新状态
        Characters.CollectionChanged += (_, _) =>
        {
            SubtitleText.Text = $"共 {Characters.Count} 个人物";
            StatusText.Text = $"共 {Characters.Count} 个人物 · 未保存的编辑将自动标记";
            AppState.HasUnsavedChanges = true;
            AppState.AutoSaveService.MarkChanged();
        };
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        AppState.GoBack();
    }

    private void OnAddCharacter(object sender, RoutedEventArgs e)
    {
        var newChar = new Character
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = "新人物"
        };
        Characters.Add(newChar);
        _project ??= AppState.CurrentProject;
        if (_project != null && !_project.Characters.Contains(newChar))
            _project.Characters.Add(newChar);

        StatusText.Text = "已新增人物「新人物」，双击编辑名称";
    }

    private void OnEditCharacterDetailed(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Character c)
        {
            var dialog = new ContentDialog
            {
                Title = "详细编辑 · " + c.Name,
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var panel = new StackPanel { Spacing = 10, Width = 520 };

            var nameBox = new TextBox { Header = "姓名 / 称呼（必填）", Text = c.Name };
            var roleBox = new TextBox { Header = "身份 / 定位", Text = c.Role };
            var aliasBox = new TextBox { Header = "别名 / 外号", Text = c.Alias };
            var tagsBox = new TextBox { Header = "性格标签（逗号分隔）", Text = c.PersonalityTags, PlaceholderText = "坚韧,内敛,执着" };
            var notesBox = new TextBox
            {
                Header = "角色设定（自由填写）",
                Text = c.Notes,
                Height = 200, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                PlaceholderText = "可写：口头禅 / 外貌 / 背景故事 / 能力技能 / 动机，或任何自定义设定。"
            };

            panel.Children.Add(nameBox);
            panel.Children.Add(roleBox);
            panel.Children.Add(aliasBox);
            panel.Children.Add(tagsBox);
            panel.Children.Add(notesBox);
            dialog.Content = panel;

            dialog.PrimaryButtonClick += (_, _) =>
            {
                c.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? "未命名角色" : nameBox.Text.Trim();
                c.Role = roleBox.Text;
                c.Alias = aliasBox.Text;
                c.PersonalityTags = tagsBox.Text;
                c.Notes = notesBox.Text;
                AppState.HasUnsavedChanges = true;
                AppState.AutoSaveService.MarkChanged();
                StatusText.Text = $"已更新人物「{c.Name}」";
            };

            _ = dialog.ShowAsync();
        }
    }

    private void OnDeleteCharacterRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Character c)
        {
            var dialog = new ContentDialog
            {
                Title = "确认删除",
                Content = $"确定删除人物「{c.Name}」吗？此操作不可撤销。",
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            dialog.PrimaryButtonClick += (_, _) =>
            {
                Characters.Remove(c);
                _project?.Characters.Remove(c);
                _selectedIds.Remove(c.Id);
                AppState.HasUnsavedChanges = true;
                AppState.AutoSaveService.MarkChanged();
                StatusText.Text = $"已删除人物「{c.Name}」";
            };

            _ = dialog.ShowAsync();
        }
    }

    private void OnDeleteSelected(object sender, RoutedEventArgs e)
    {
        var toRemove = Characters.Where(c => _selectedIds.Contains(c.Id)).ToList();
        if (toRemove.Count == 0) return;

        var dialog = new ContentDialog
        {
            Title = "确认批量删除",
            Content = $"确定删除选中的 {toRemove.Count} 个人物吗？此操作不可撤销。",
            XamlRoot = this.Content.XamlRoot,
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close
        };

        dialog.PrimaryButtonClick += (_, _) =>
        {
            foreach (var c in toRemove)
            {
                Characters.Remove(c);
                _project?.Characters.Remove(c);
                _selectedIds.Remove(c.Id);
            }
            DeleteSelectedButton.IsEnabled = false;
            AppState.HasUnsavedChanges = true;
            AppState.AutoSaveService.MarkChanged();
            StatusText.Text = $"已删除 {toRemove.Count} 个人物";
        };

        _ = dialog.ShowAsync();
    }

    private void OnSelectAll(object sender, RoutedEventArgs e)
    {
        _selectedIds.Clear();
        foreach (var c in Characters) _selectedIds.Add(c.Id);
        DeleteSelectedButton.IsEnabled = _selectedIds.Count > 0;
    }

    private void OnUnselectAll(object sender, RoutedEventArgs e)
    {
        _selectedIds.Clear();
        DeleteSelectedButton.IsEnabled = false;
    }

    // 绑定复选框时无法直接使用 Tag，在代码中处理选中
    private void OnCharacterCheckChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string id)
        {
            if (cb.IsChecked == true) _selectedIds.Add(id);
            else _selectedIds.Remove(id);
            DeleteSelectedButton.IsEnabled = _selectedIds.Count > 0;
        }
    }
}
