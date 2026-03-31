namespace SECS2GEM.Simulator;

using WinFormsApp = System.Windows.Forms;

static class Program
{
    /// <summary>
    /// 应用程序的主入口点
    /// SECS/GEM Simulator - 用于测试HSMS通信和SECS-II消息
    /// </summary>
    [STAThread]
    static void Main()
    {
        WinFormsApp.Application.SetHighDpiMode(WinFormsApp.HighDpiMode.SystemAware);
        WinFormsApp.Application.EnableVisualStyles();
        WinFormsApp.Application.SetCompatibleTextRenderingDefault(false);
        WinFormsApp.Application.Run(new MainForm());
    }
}