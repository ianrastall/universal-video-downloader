using System.Text.Json;
using UniversalVideoDownloader.Core.Downloads;
using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.Core.Tools;

namespace UniversalVideoDownloader.Tests;

public sealed class SpecialistTests
{
    [Fact]
    public void A_clip_can_be_queued_as_video_and_chat_and_both_jobs_survive_restart()
    {
        const string clip = "https://clips.twitch.tv/AClip";
        using var app = new TestApp(configure: settings => settings.StartQueueAutomatically = false);
        app.Downloads.IsQueueRunning = false;

        Assert.Equal(1, app.Downloads.Enqueue([clip], DownloadMode.TwitchVideo).Added);
        Assert.Equal(1, app.Downloads.Enqueue([clip], DownloadMode.TwitchChat).Added);
        Assert.Equal(1, app.Downloads.Enqueue(["https://www.twitch.tv/example/clip/AClip?tt_content=test"], DownloadMode.TwitchChat).Skipped);

        var entries = app.Queue.Load();
        Assert.Equal([DownloadMode.TwitchVideo, DownloadMode.TwitchChat], entries.Select(entry => entry.Mode));
    }

    [Fact]
    public void Twitch_jobs_can_start_without_yt_dlp()
    {
        using var app = new TestApp(installYtDlp: false);
        File.WriteAllText(ToolId.TwitchDownloader.ExecutablePath(app.ToolsDir), "");
        app.Downloads.Enqueue(["https://www.twitch.tv/videos/1234"], DownloadMode.TwitchChat);
        app.Dispatcher.RunUntil(() => app.Runner.Runs.Count == 1);
        Assert.Equal(DownloadMode.TwitchChat, app.Runner.Runs[0].Request.Mode);
    }

    [Fact]
    public void Libation_exports_supply_the_title_author_and_audible_identifier()
    {
        using var json = JsonDocument.Parse("""
            [
              { "AudibleProductId": "B000ABC123", "Title": "Test Book", "AuthorNames": "Test Author", "BookStatus": "Downloaded" },
              { "AudibleProductId": "B000ABC123", "Title": "Duplicate" },
              { "AudibleProductId": "invalid", "Title": "Incomplete" }
            ]
            """);

        var title = Assert.Single(LibationClient.ReadTitles(json.RootElement));
        Assert.Equal("Test Book", title.Title);
        Assert.Equal("Test Author", title.Authors);
        Assert.Equal("Downloaded", title.Status);
        Assert.Equal("https://www.audible.com/pd/B000ABC123", title.Link);
    }
}
