using System.Text.Json;

internal static class Program
{
    internal static readonly string StateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CopilotKeyFighter");
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();
            if (args.Length == 2 && args[0] == "--list-apps")
            {
                File.WriteAllText(args[1], JsonSerializer.Serialize(AppCatalog.LoadAsync().GetAwaiter().GetResult(), SettingsStore.Options)); return 0;
            }
            if (args.Length >= 2 && args[0] == "--render-settings")
            {
                using var form = new SettingsForm(args.Contains("keyboard") ? new(Action: ActionKind.Keyboard, Binding: new()) : new(), _ => { });
                form.Show(); Application.DoEvents();
                using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, form.ClientRectangle with { Width = form.Width, Height = form.Height });
                bitmap.Save(args[1]); form.Close(); return 0;
            }
            using var mutex = new Mutex(false, @"Local\CopilotKeyFighter-v1");
            bool owned;
            try { owned = mutex.WaitOne(0); } catch (AbandonedMutexException) { owned = true; }
            if (!owned)
            {
                nint window = 0;
                for (int i = 0; i < 50 && window == 0; i++) { window = Native.FindWindowEx(-3, 0, null, Resident.WindowTitle); if (window == 0) Thread.Sleep(20); }
                if (window == 0) return 2;
                Native.GetWindowThreadProcessId(window, out uint pid); Native.AllowSetForegroundWindow(pid);
                uint message = args.Contains("--exit") ? Resident.ExitMessage : args.Contains("--background") ? 0 : Resident.SettingsMessage;
                return message == 0 || Native.PostMessage(window, message, 0, 0) ? 0 : 3;
            }
            try
            {
                if (args.Contains("--exit")) return 0;
                using var resident = new Resident(args.Contains("--background"));
                Application.Run(resident);
            }
            finally { mutex.ReleaseMutex(); }
            return 0;
        }
        catch (Exception e)
        {
            Log(e);
            if (!args.Contains("--background")) MessageBox.Show(e.Message, "Copilot Key Fighter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
    internal static void Log(Exception error)
    {
        try { Directory.CreateDirectory(StateDirectory); File.WriteAllText(Path.Combine(StateDirectory, "last-error.txt"), $"{DateTimeOffset.Now:O}\n{error}"); } catch { }
    }
    internal static System.Drawing.Icon LoadIcon()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("CopilotKeyFighter.App.ico")!;
        using var icon = new System.Drawing.Icon(stream, SystemInformation.SmallIconSize);
        return (System.Drawing.Icon)icon.Clone();
    }
}
