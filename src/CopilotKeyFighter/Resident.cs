internal sealed class Resident : ApplicationContext
{
    internal const string WindowTitle = "CopilotKeyFighter.Resident.v1";
    internal const uint SettingsMessage = 0x8001, ExitMessage = 0x8002, CopilotMessage = 0x8004;
    readonly Receiver receiver;
    readonly KeyboardHook keyboard;
    readonly SettingsStore store = new(Program.StateDirectory);
    readonly NotifyIcon tray;
    readonly System.Drawing.Icon icon;
    readonly ContextMenuStrip menu = new();
    readonly ToolStripMenuItem pause;
    readonly ToolStripMenuItem execute;
    ForegroundHotkey? foreground;
    Settings settings;
    SettingsForm? form;
    CancellationTokenSource pending = new();
    bool paused, opening, sending, disposed;
    int generation;

    internal Resident(bool background)
    {
        receiver = new(this);
        keyboard = new(receiver.Handle);
        icon = Program.LoadIcon();
        menu.Items.Add("設定…", null, (_, _) => ShowSettings());
        execute = (ToolStripMenuItem)menu.Items.Add("割り当てたアプリを開く", null, (_, _) => Open());
        pause = (ToolStripMenuItem)menu.Items.Add("一時停止", null, (_, _) => TogglePause());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitThread());
        tray = new() { Text = "Copilot Key Fighter", Icon = icon, ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => ShowSettings();
        var loaded = store.Load(); settings = loaded.Settings;
        try
        {
            if (settings.StartWithWindows) Startup.Set(true);
            Configure();
        }
        catch (Exception e) { paused = true; Configure(); Notify(e); }
        if (loaded.Error != null) tray.ShowBalloonTip(5000, "設定の確認が必要です", loaded.Error, ToolTipIcon.Warning);
        if (!background || !File.Exists(store.FilePath) || loaded.Error != null)
            Native.PostMessage(receiver.Handle, SettingsMessage, 0, 0);
        TraceGesture("ready");
    }
    void CancelPending()
    {
        generation++;
        pending.Cancel(); pending.Dispose(); pending = new();
        foreground?.Cancel();
    }
    void Configure()
    {
        keyboard.Enabled = false;
        bool active = settings.Enabled && !paused;
        if (active && Startup.LegacyRunning()) throw new InvalidOperationException("旧 ChatGPTLauncher が起動中です。旧版のトレイから Exit を選び、自動起動を解除してから有効にしてください。");
        if (active) foreground ??= new(receiver.Handle);
        else { foreground?.Dispose(); foreground = null; }
        keyboard.Enabled = active;
        pause.Text = paused ? "再開" : "一時停止";
        pause.Enabled = settings.Enabled;
        execute.Enabled = settings.Action == ActionKind.Application && settings.Target != null;
        tray.Text = active ? "Copilot Key Fighter — 有効" : "Copilot Key Fighter — 停止中";
    }
    void TogglePause()
    {
        CancelPending(); paused = !paused;
        try { Configure(); } catch (Exception e) { paused = true; Configure(); Notify(e); }
    }
    void Save(Settings next)
    {
        next.Validate();
        if (next.Enabled && Startup.LegacyRunning()) throw new InvalidOperationException("旧 ChatGPTLauncher を終了してから有効にしてください。PowerToys の別の設定を変更する必要はありません。");
        var previous = settings; bool oldPaused = paused;
        CancelPending(); settings = next; paused = false;
        try { Configure(); Startup.Set(next.StartWithWindows); store.Save(next); }
        catch
        {
            settings = previous; paused = oldPaused;
            try { Startup.Set(previous.StartWithWindows); Configure(); } catch { keyboard.Enabled = false; paused = true; }
            throw;
        }
    }
    void ShowSettings()
    {
        CancelPending();
        if (form is { IsDisposed: false }) { form.Show(); form.Activate(); return; }
        form = new(settings, Save);
        form.Show(); form.Activate();
    }
    internal async void Open()
    {
        if (opening || settings.Target == null) return;
        opening = true;
        try { await AppLauncher.OpenAsync(settings.Target, settings.PreferExisting, pending.Token); }
        catch (OperationCanceledException) { }
        catch (Exception e) { Notify(e); }
        finally { opening = false; }
    }
    async void Copilot(int serial, nint originalWindow)
    {
        TraceGesture("detected");
        if (!keyboard.Enabled) return;
        // A consumed non-character chord also prevents a bare Win release from opening Start.
        // Use the same registered internal pair for all actions; never synthesize modifier keys here.
        try { foreground?.Request(); } catch (Exception e) { Notify(e); return; }
        if (settings.Action == ActionKind.Application)
        {
            if (opening) return;
            return;
        }
        if (settings.Action != ActionKind.Keyboard || sending) return;
        sending = true;
        int version = generation;
        var token = pending.Token;
        var binding = settings.Binding!;
        long deadline = Environment.TickCount64 + 2000;
        try
        {
            // Bounded, only while a gesture is pending; the idle resident has no polling timer.
            while (Environment.TickCount64 < deadline)
            {
                await Task.Delay(10, token);
                var decision = PendingGesture.Evaluate(Environment.TickCount64, deadline, version == generation, keyboard.Enabled, serial == keyboard.Interference, Native.GetForegroundWindow() == originalWindow, ReleaseGate.Blocked(keyboard.PhysicallyDown, Native.Down, binding.Key));
                if (decision == PendingDecision.Cancel)
                {
                    TraceGesture("cancelled", new { SameGeneration = version == generation, Enabled = keyboard.Enabled, SameInput = serial == keyboard.Interference, SameWindow = Native.GetForegroundWindow() == originalWindow, TimedOut = Environment.TickCount64 >= deadline, PhysicalF23 = keyboard.PhysicallyDown(0x86), Win = Native.Down(0x5B) || Native.Down(0x5C), Shift = Native.Down(0x10), Control = Native.Down(0x11), Alt = Native.Down(0x12) });
                    return;
                }
                if (decision == PendingDecision.Wait) continue;
                KeySender.Send(binding, keyboard.PhysicallyDown); TraceGesture("sent"); return;
            }
            TraceGesture("release-timeout", new { Win = Native.Down(0x5B) || Native.Down(0x5C), Shift = Native.Down(0x10), Control = Native.Down(0x11), Alt = Native.Down(0x12), F23 = Native.Down(0x86), InternalKey = Native.Down(0xFC) });
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { TraceGesture("send-error", new { Error = e.Message }); Notify(e); }
        finally { sending = false; }
    }
    void TraceGesture(string result, object? detail = null)
    {
        try
        {
            Directory.CreateDirectory(Program.StateDirectory);
            File.WriteAllText(Path.Combine(Program.StateDirectory, "last-gesture.json"), System.Text.Json.JsonSerializer.Serialize(new { Time = DateTimeOffset.Now, Result = result, Enabled = keyboard.Enabled, Action = settings.Action.ToString(), Detail = detail, Release = keyboard.ReleaseDiagnostic }, SettingsStore.Options));
        }
        catch { /* Diagnostics must never interrupt input processing. */ }
    }
    void Notify(Exception e)
    {
        Program.Log(e);
        if (!disposed) tray.ShowBalloonTip(4000, "Copilot Key Fighter", e.Message, ToolTipIcon.Warning);
    }
    protected override void ExitThreadCore()
    {
        keyboard.Enabled = false; CancelPending(); keyboard.Dispose();
        form?.Close(); tray.Visible = false; base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true; pending.Cancel(); keyboard.Dispose(); foreground?.Dispose();
            form?.Dispose(); tray.Dispose(); icon.Dispose(); menu.Dispose(); receiver.DestroyHandle(); pending.Dispose();
        }
        base.Dispose(disposing);
    }
    sealed class Receiver : NativeWindow
    {
        readonly Resident owner;
        internal Receiver(Resident owner) { this.owner = owner; CreateHandle(new CreateParams { Caption = WindowTitle, Parent = -3 }); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == SettingsMessage) owner.ShowSettings();
            else if (m.Msg == ExitMessage) owner.ExitThread();
            else if (m.Msg == CopilotMessage) owner.Copilot((int)m.WParam, m.LParam);
            else if (m.Msg == 0x312 && owner.foreground?.Accept(m.WParam) == true)
            { if (owner.keyboard.Enabled && owner.settings.Action == ActionKind.Application) owner.Open(); }
            else base.WndProc(ref m);
        }
    }
}
