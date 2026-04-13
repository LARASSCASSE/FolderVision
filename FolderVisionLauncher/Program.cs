using System.Diagnostics;
using System.IO;

var launcherDir = AppContext.BaseDirectory;
var exePath = Path.Combine(launcherDir, "publish-wpf", "FolderVision.Wpf.exe");

if (!File.Exists(exePath))
{
    System.Windows.MessageBox.Show(
        "Could not find FolderVision.Wpf.exe in publish-wpf subfolder.\nExpected: " + exePath,
        "FolderVision Launcher Error");
    return 1;
}

Process.Start(new ProcessStartInfo(exePath)
{
    WorkingDirectory = Path.GetDirectoryName(exePath)!,
    UseShellExecute = true,
});
return 0;
