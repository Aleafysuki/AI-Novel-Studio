using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using AINovelWriter.Models;

namespace AINovelWriter.Converters;

/// <summary>
/// 布尔值转可见性（True -> Visible, False -> Collapsed）
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}

/// <summary>
/// 布尔值反转转可见性（True -> Collapsed, False -> Visible）
/// </summary>
public class BoolToInvertVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Collapsed;
    }
}

/// <summary>
/// 布尔值反转
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && !b;
    }
}

/// <summary>
/// 章节状态转颜色
/// </summary>
public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChapterStatus status)
        {
            return status switch
            {
                ChapterStatus.Draft => new SolidColorBrush(Microsoft.UI.Colors.Gray),
                ChapterStatus.Writing => new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                ChapterStatus.Review => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                ChapterStatus.Completed => new SolidColorBrush(Microsoft.UI.Colors.Green),
                ChapterStatus.Published => new SolidColorBrush(Microsoft.UI.Colors.Purple),
                _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字数 > 0 时显示
/// </summary>
public class WordCountVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is long count && count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// AI 参与度 → 颜色（越高越"AI 主导"，颜色越深）
/// </summary>
public class ParticipationColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double level = value switch { double d => d, int i => i, _ => 0 };
        return level switch
        {
            <= 5 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x6B, 0x72, 0x80)),   // 灰
            <= 35 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x10, 0xA3, 0x7A)),  // 绿
            <= 65 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x2E, 0x7D, 0xF6)),  // 蓝
            <= 90 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x7C, 0x3A, 0xED)),  // 紫
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xE0, 0x2E, 0x6B))       // 红
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 非空字符串时显示
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => !string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 对象非 null 时显示
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 数值 > 0 时显示（int/count）
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int n = value switch { int i => i, long l => (int)l, _ => 0 };
        bool invert = parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase);
        bool show = n > 0;
        if (invert) show = !show;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// AI 生成占比 → 描述色（AI 占比越高越紫）
/// </summary>
public class AiRatioColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int ratio = value switch { int i => i, double d => (int)d, _ => 0 };
        return ratio switch
        {
            >= 70 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x7C, 0x3A, 0xED)),
            >= 40 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x2E, 0x7D, 0xF6)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x10, 0xA3, 0x7A))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 大纲确认状态 → 按钮文字（已确认 / 确认）
/// </summary>
public class ConfirmTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? "✓ 已确认" : "确认";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 大纲确认状态 → 按钮背景（已确认显示强调色淡底）
/// </summary>
public class ConfirmBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(38, 0x7C, 0x3A, 0xED))
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
