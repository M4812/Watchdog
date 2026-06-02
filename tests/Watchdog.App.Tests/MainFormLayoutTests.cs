using System.Drawing;
using System.Windows.Forms;
using Watchdog.App;

namespace Watchdog.App.Tests;

[TestClass]
public sealed class MainFormLayoutTests
{
    [STATestMethod]
    public void InitialLayout_KeepsMonitoringActionButtonsInsideSettingsSurface()
    {
        using var form = new MainForm
        {
            Size = new Size(900, 790),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000)
        };

        form.Show();
        Application.DoEvents();

        foreach (var buttonText in new[] { "开始监控", "停止监控", "立即检查" })
        {
            var button = FindControl<Button>(form, buttonText);
            Assert.IsNotNull(button, $"找不到按钮：{buttonText}");
            Assert.IsNotNull(button.Parent, $"{buttonText} 没有父容器。");
            Assert.IsTrue(button.Visible, $"{buttonText} 不可见。");
            Assert.IsTrue(
                button.Bottom <= button.Parent.ClientSize.Height,
                $"{buttonText} 超出设置区域底部：Bottom={button.Bottom}, ParentHeight={button.Parent.ClientSize.Height}");
        }
    }

    private static T? FindControl<T>(Control root, string text)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T typedChild && child.Text == text)
            {
                return typedChild;
            }

            var nested = FindControl<T>(child, text);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
