using UniversalVideoDownloader.Core.Downloads;
using UniversalVideoDownloader.Core.Settings;

namespace UniversalVideoDownloader.Tests;

public class MigrationTests
{
    [Fact]
    public void Queue_recovery_keeps_startup_usable_and_does_not_overwrite_unreadable_files()
    {
        using var app = new TestApp();
        File.WriteAllText(app.Queue.Path, "broken");
        app.Downloads.RestoreQueue();
        Assert.NotNull(app.Downloads.RestoreError);
        app.Downloads.SaveQueue();
        Assert.Empty(app.Queue.Load());
        Assert.Single(Directory.GetFiles(app.Root, "queue.json.invalid-*"));
        using var locked = File.Open(app.Queue.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        app.Downloads.RestoreQueue();
        Assert.Contains("Queue saving is disabled", app.Downloads.RestoreError);
        app.Downloads.SaveQueue(); // Must not try to replace the locked queue.
    }

    [Fact]
    public void Corrupt_queue_keeps_unique_recovery_copies()
    {
        var dir = Directory.CreateTempSubdirectory("uvd-recovery-");
        try
        {
            var path = Path.Combine(dir.FullName, "queue.json");
            File.WriteAllText(path, "[{truncated");
            var store = new QueueStore(path);
            Assert.Throws<InvalidDataException>(() => store.Load());
            Assert.Throws<InvalidDataException>(() => store.Load());
            Assert.Equal(2, Directory.GetFiles(dir.FullName, "queue.json.invalid-*").Length);
            store.Save([]);
            foreach (var backup in Directory.GetFiles(dir.FullName, "queue.json.invalid-*"))
                Assert.Equal("[{truncated", File.ReadAllText(backup));
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public void Existing_settings_gain_appearance_defaults_and_preserve_download_options()
    {
        var dir = Directory.CreateTempSubdirectory("uvd-migration-");
        try
        {
            var path = Path.Combine(dir.FullName, "settings.json");
            File.WriteAllText(path, """{"outputDirectory":"D:\\我的视频","concurrency":4,"extraArguments":"--limit-rate 1M"}""");
            var store = new SettingsStore(path);
            var settings = store.Load();
            Assert.False(settings.UseCustomAccent);
            Assert.Null(settings.Theme);
            store.Save(settings);
            var restored = store.Load();
            Assert.Equal("D:\\我的视频", restored.OutputDirectory);
            Assert.Equal(4, restored.Concurrency);
            Assert.Equal("--limit-rate 1M", restored.ExtraArguments);
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public void Native_parser_owns_independent_results_across_threads()
    {
        Parallel.For(0, 1000, index =>
        {
            var title = $"视频 🎬 {index}\0";
            Assert.Equal(new JobUpdate.Title(title), Assert.Single(YtDlpOutputParser.Parse("@@T " + title, false)));
            Assert.IsType<JobUpdate.Log>(Assert.Single(YtDlpOutputParser.Parse("", false)));
        });
    }
}
