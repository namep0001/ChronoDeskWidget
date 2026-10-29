using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChronoDeskWidget
{
    internal sealed class TaskItem
    {
        public string Id;
        public string Title;
        public bool Done;
    }

    internal static class LocalStore
    {
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChronoDeskWidget");
        public static readonly string ErrorPath = Path.Combine(Folder, "error.log");
        private static string TasksPath { get { return Path.Combine(Folder, "tasks.txt"); } }
        private static string NotesPath { get { return Path.Combine(Folder, "notes.txt"); } }
        private static string SettingsPath { get { return Path.Combine(Folder, "settings.ini"); } }

        static LocalStore()
        {
            EnsureFolder();
        }

        public static void EnsureFolder()
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
        }

        public static List<TaskItem> LoadTasks()
        {
            List<TaskItem> tasks = new List<TaskItem>();
            try
            {
                if (!File.Exists(TasksPath)) return tasks;
                string[] lines = File.ReadAllLines(TasksPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(new char[] { '|' }, 3);
                    if (parts.Length != 3) continue;
                    string title;
                    try { title = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])); }
                    catch { continue; }
                    tasks.Add(new TaskItem { Id = parts[0], Done = parts[1] == "1", Title = title });
                }
            }
            catch { }
            return tasks;
        }

        public static void SaveTasks(IList<TaskItem> tasks)
        {
            EnsureFolder();
            StringBuilder sb = new StringBuilder();
            foreach (TaskItem task in tasks)
            {
                string title = Convert.ToBase64String(Encoding.UTF8.GetBytes(task.Title ?? string.Empty));
                sb.Append(task.Id ?? Guid.NewGuid().ToString("N"));
                sb.Append('|').Append(task.Done ? '1' : '0').Append('|').Append(title).AppendLine();
            }
            File.WriteAllText(TasksPath, sb.ToString(), new UTF8Encoding(false));
        }

        public static string LoadNotes()
        {
            try { return File.Exists(NotesPath) ? File.ReadAllText(NotesPath, Encoding.UTF8) : string.Empty; }
            catch { return string.Empty; }
        }

        public static void SaveNotes(string notes)
        {
            EnsureFolder();
            File.WriteAllText(NotesPath, notes ?? string.Empty, new UTF8Encoding(false));
        }

        public static string LoadSetting(string key, string defaultValue)
        {
            try
            {
                if (!File.Exists(SettingsPath)) return defaultValue;
                foreach (string line in File.ReadAllLines(SettingsPath, Encoding.UTF8))
                {
                    int equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    if (string.Equals(line.Substring(0, equals).Trim(), key, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(equals + 1).Trim();
                }
            }
            catch { }
            return defaultValue;
        }

        public static void SaveSetting(string key, string value)
        {
            EnsureFolder();
            Dictionary<string, string> settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(SettingsPath))
                {
                    foreach (string line in File.ReadAllLines(SettingsPath, Encoding.UTF8))
                    {
                        int equals = line.IndexOf('=');
                        if (equals > 0) settings[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                    }
                }
            }
            catch { }
            settings[key] = value;
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in settings)
                sb.Append(pair.Key).Append('=').Append(pair.Value).AppendLine();
            File.WriteAllText(SettingsPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}