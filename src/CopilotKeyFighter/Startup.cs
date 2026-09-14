using Microsoft.Win32;

internal static class Startup
{
    internal static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("CopilotKeyFighter", $"\"{Environment.ProcessPath}\" --background");
        else key.DeleteValue("CopilotKeyFighter", false);
    }
    internal static bool LegacyRunning()
    {
        try { using var mutex = Mutex.OpenExisting(@"Local\ChatGPTLauncher-Resident-v2"); return true; }
        catch (WaitHandleCannotBeOpenedException) { return false; }
    }
}
