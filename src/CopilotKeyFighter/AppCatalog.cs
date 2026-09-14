using System.Runtime.InteropServices;

internal static class AppCatalog
{
    internal static Task<List<AppTarget>> LoadAsync()
    {
        var completion = new TaskCompletionSource<List<AppTarget>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { completion.SetResult(Load()); } catch (Exception e) { completion.SetException(e); }
        }) { IsBackground = true, Name = "Installed applications" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
    static List<AppTarget> Load()
    {
        object? shell = null, folder = null, items = null;
        var apps = new List<AppTarget>();
        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!);
            folder = ((dynamic)shell!).NameSpace("shell:AppsFolder");
            if (folder == null) throw new InvalidOperationException("アプリ一覧を取得できませんでした。");
            items = ((dynamic)folder).Items();
            for (int i = 0; i < (int)((dynamic)items).Count; i++)
            {
                object? item = null;
                try
                {
                    item = ((dynamic)items).Item(i);
                    string name = Convert.ToString(((dynamic)item).Name) ?? "";
                    string id = Property(item, "System.AppUserModel.ID");
                    string executable = Property(item, "System.Link.TargetParsingPath");
                    if (id.Length == 0) id = Convert.ToString(((dynamic)item).Path) ?? "";
                    if (name.Length == 0 || id.Length == 0) continue;
                    // Keep Shell IDs distinct from packaged AUMIDs and executable paths.
                    if (id.Contains('!')) apps.Add(new(TargetKind.Packaged, name, id));
                    else if (File.Exists(executable) && executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        apps.Add(new(TargetKind.Executable, name, executable, executable));
                    else apps.Add(new(TargetKind.Shell, name, id, executable));
                }
                catch (COMException) { /* A single stale registration must not hide the remaining apps. */ }
                finally { Release(item); }
            }
            return apps.DistinctBy(x => (x.Kind, x.Id)).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        finally { Release(items); Release(folder); Release(shell); }
    }
    static string Property(dynamic item, string name) { try { return Convert.ToString(item.ExtendedProperty(name)) ?? ""; } catch (COMException) { return ""; } }
    internal static void LaunchShell(string id)
    {
        object? shell = null, folder = null, item = null;
        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!);
            folder = ((dynamic)shell!).NameSpace("shell:AppsFolder");
            item = ((dynamic)folder!).ParseName(id);
            if (item == null) throw new InvalidOperationException("アプリが登録されていません。設定から選び直してください。");
            ((dynamic)item).InvokeVerb();
        }
        finally { Release(item); Release(folder); Release(shell); }
    }
    static void Release(object? value) { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
}
