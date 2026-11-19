using System;
using System.Threading;
using System.Windows;

namespace HoldToPeek;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        using var instance = new Mutex(true, @"Local\HoldToPeek.Application", out bool isFirst);
        if (!isFirst)
        {
            try
            {
                using var signal = EventWaitHandle.OpenExisting(@"Local\HoldToPeek.ShowSettings");
                signal.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                MessageBox.Show("瞄一眼正在启动，请稍后从托盘打开。", "瞄一眼");
            }
            return 0;
        }

        try
        {
            using var signal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\HoldToPeek.ShowSettings");
            var app = new PeekApplication(signal);
            return app.Run();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"启动失败：{exception.Message}", "瞄一眼", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
        finally
        {
            instance.ReleaseMutex();
        }
    }
}
