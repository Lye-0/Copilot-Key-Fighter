using Microsoft.Win32;

internal static class Startup
{
    internal const string RegistryPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName="CopilotKeyFighter";
    internal static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        if (enabled) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" --background");
        else key.DeleteValue(ValueName, false);
    }
    internal static bool LegacyRunning()
    {
        try { using var mutex = Mutex.OpenExisting(@"Local\ChatGPTLauncher-Resident-v2"); return true; }
        catch (WaitHandleCannotBeOpenedException) { return false; }
    }
}
