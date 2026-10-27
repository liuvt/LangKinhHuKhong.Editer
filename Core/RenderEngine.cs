using System.Globalization;
using System.Text;

namespace LangKinhHuKhong.Editer.Core;

/// <summary>
/// v1.2: ALL MP3 tracks => ONE continuous audio track; ALL videos => ONE playlist;
/// repeat the video playlist only as needed, then render EXACTLY ONE MP4.
/// No MP3 is played in parallel and no separate MP4 is created per MP3.
/// </summary>
public sealed class RenderEngine(FfmpegTools tools)
{
    private readonly MediaProbe _probe = new(tools);

    public async Task<string> RenderAsync(
        RenderOptions options,
        IProgress<RenderProgress> progress,
        CancellationToken cancellationToken)
    {
        Validate(options);
        Directory.CreateDirectory(options.OutputDirectory);

        var workdir = Path.Combine(Path.GetTempPath(), "LangKinhHuKhong.Editer", Guid.NewGuid().ToString("N"));
        var clipsFolder = Path.Combine(workdir, "clips");
        var audioFolder = Path.Combine(workdir, "audio");
        Directory.CreateDirectory(clipsFolder);
        Directory.CreateDirectory(audioFolder);

        try
        {
            // Step 1: normalize every MP3 to a common, lossless intermediate format.
            // This makes concatenation safe with different sample rates, bitrates,
            // channel layouts, metadata and VBR MP3 files. MP3 order is preserved.
            var audioClips = new List<string>();
            double estimatedAudioDuration = 0;
            for (var i = 0; i < options.Mp3Files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = options.Mp3Files[i];
                var seconds = await _probe.GetDurationSecondsAsync(input, cancellationToken);
                estimatedAudioDuration += seconds;
                var output = Path.Combine(audioFolder, $"{i + 1:D5}.flac");
                var index = i;
                var title = $"Chuẩn hóa MP3 {i + 1}/{options.Mp3Files.Count}: {Path.GetFileName(input)}";
                progress.Report(new RenderProgress(title, 15.0 * index / options.Mp3Files.Count));
                var args = new List<string>
                {
                    "-nostdin", "-y", "-hide_banner", "-loglevel", "error",
                    "-stats_period", "0.5", "-progress", "pipe:1",
                    "-i", input, "-map", "0:a:0", "-vn", "-sn", "-dn",
                    "-af", "aresample=48000:async=1:first_pts=0,aformat=sample_fmts=s16:channel_layouts=stereo",
                    "-c:a", "flac", "-compression_level", "5", output
                };
                await ProcessRunner.RunAsync(tools.Ffmpeg, args,
                    line => ReportFfmpegProgress(line, seconds, p =>
                        progress.Report(new RenderProgress(title,
                            15.0 * (index + p) / options.Mp3Files.Count))), cancellationToken);
                audioClips.Add(output);
            }

            // Step 2: encode the concatenated audio just once. The final AUDIO
            // duration (rather than the sum of approximate MP3 metadata) is the
            // authoritative length used when rendering the one output video.
            var audioList = Path.Combine(workdir, "audio.ffconcat");
            await WriteConcatListAsync(audioList, "audio", audioClips, cancellationToken);
            var joinedAudio = Path.Combine(workdir, "joined_audio.m4a");
            const string joinTitle = "Ghép tất cả MP3 thành một audio liên tục";
            progress.Report(new RenderProgress(joinTitle, 15));
            var joinArgs = new List<string>
            {
                "-nostdin", "-y", "-hide_banner", "-loglevel", "error",
                "-stats_period", "0.5", "-progress", "pipe:1",
                "-safe", "0", "-f", "concat", "-i", audioList,
                "-map", "0:a:0", "-vn", "-sn", "-dn",
                "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2",
                "-movflags", "+faststart", joinedAudio
            };
            await ProcessRunner.RunAsync(tools.Ffmpeg, joinArgs,
                line => ReportFfmpegProgress(line, estimatedAudioDuration, p =>
                    progress.Report(new RenderProgress(joinTitle, 15.0 + 5.0 * p))),
                cancellationToken);
            var audioDuration = await _probe.GetDurationSecondsAsync(joinedAudio, cancellationToken);

            // Step 3: normalize each video using ITS OWN playback speed and the
            // selected export resolution/FPS; retain original sequence.
            var videoClips = new List<string>();
            double playlistDuration = 0;
            for (var i = 0; i < options.Videos.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var video = options.Videos[i];
                var sourceDuration = await _probe.GetDurationSecondsAsync(video.FilePath, cancellationToken);
                var expectedDuration = sourceDuration / video.PlaybackSpeed;
                var clipFile = Path.Combine(clipsFolder, $"{i + 1:D5}.mp4");
                var index = i;
                var title = $"Chuẩn hóa video {i + 1}/{options.Videos.Count} ({PlaybackSpeeds.Format(video.PlaybackSpeed)}): {Path.GetFileName(video.FilePath)}";
                progress.Report(new RenderProgress(title, 20.0 + 35.0 * i / options.Videos.Count));
                var hasAudio = !options.MuteVideoAudio &&
                    await _probe.HasAudioStreamAsync(video.FilePath, cancellationToken);
                var args = BuildNormalizeArguments(options, video, video.FilePath,
                    clipFile, expectedDuration, hasAudio);
                await ProcessRunner.RunAsync(tools.Ffmpeg, args,
                    line => ReportFfmpegProgress(line, expectedDuration, p =>
                        progress.Report(new RenderProgress(title,
                            20.0 + 35.0 * (index + p) / options.Videos.Count))),
                    cancellationToken);
                playlistDuration += await _probe.GetDurationSecondsAsync(clipFile, cancellationToken);
                videoClips.Add(clipFile);
            }

            // Step 4: loop the ENTIRE ordered video playlist when necessary.
            // Output always ends at the joined audio duration, never per input MP3.
            var videoList = Path.Combine(workdir, "playlist.ffconcat");
            await WriteConcatListAsync(videoList, "clips", videoClips, cancellationToken);
            var cycles = Math.Max(1, (int)Math.Ceiling(audioDuration / playlistDuration));
            var finalTitle = $"Xuất 1 MP4 | Audio {TimeSpan.FromSeconds(audioDuration).ToString("c", CultureInfo.InvariantCulture)} | " +
                             $"Playlist video {TimeSpan.FromSeconds(playlistDuration).ToString("c", CultureInfo.InvariantCulture)} | " +
                             $"{cycles} lượt phát (lượt cuối có thể bị cắt)";
            progress.Report(new RenderProgress(finalTitle, 55));
            var outputFile = GetUniqueOutputFile(options.OutputDirectory, "LangKinhHuKhong_Full", options.ResolutionName);
            var temporaryOutput = Path.Combine(workdir, "final_render.mp4");
            var finalArgs = BuildOutputArguments(options, videoList, joinedAudio, temporaryOutput, audioDuration);
            await ProcessRunner.RunAsync(tools.Ffmpeg, finalArgs,
                line => ReportFfmpegProgress(line, audioDuration, p =>
                    progress.Report(new RenderProgress(finalTitle, 55.0 + 45.0 * p))),
                cancellationToken);

            // A failed/canceled job must not leave a partial MP4 in output folder.
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryOutput, outputFile);
            progress.Report(new RenderProgress("Hoàn thành 1 video MP4 duy nhất.", 100));
            return outputFile;
        }
        finally
        {
            try { Directory.Delete(workdir, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task WriteConcatListAsync(string file, string relativeDirectory,
        IReadOnlyList<string> clips, CancellationToken ct)
    {
        var content = new StringBuilder("ffconcat version 1.0\n");
        foreach (var clip in clips)
            content.Append("file '").Append(relativeDirectory).Append('/')
                .Append(Path.GetFileName(clip)).Append("'\n");
        await File.WriteAllTextAsync(file, content.ToString(), new UTF8Encoding(false), ct);
    }

    private static void Validate(RenderOptions settings)
    {
        if (settings.Videos.Count == 0) throw new ArgumentException("Chưa chọn video nguồn.");
        if (settings.Mp3Files.Count == 0) throw new ArgumentException("Chưa chọn MP3.");
        if (settings.Overlays.Count > 2) throw new ArgumentException("Tối đa 2 ảnh overlay.");
        if (settings.Overlays.Select(x => x.Corner).Distinct().Count() != settings.Overlays.Count)
            throw new ArgumentException("Hai ảnh phải đặt ở hai góc khác nhau.");
        if (string.IsNullOrWhiteSpace(settings.OutputDirectory))
            throw new ArgumentException("Chưa chọn thư mục đầu ra.");
        if (settings.FrameRate is < 1 or > 120) throw new ArgumentException("FPS không hợp lệ.");
        if (settings.Width is < 64 or > 7680 || settings.Height is < 64 or > 4320)
            throw new ArgumentException("Độ phân giải không hợp lệ.");
        foreach (var clip in settings.Videos)
            if (!PlaybackSpeeds.IsSupported(clip.PlaybackSpeed))
                throw new ArgumentException($"Tốc độ video không hợp lệ: {clip.FilePath} ({clip.PlaybackSpeed}).");
        foreach (var input in settings.Videos.Select(x => x.FilePath).Concat(settings.Mp3Files)
                     .Concat(settings.Overlays.Select(x => x.ImagePath)))
            if (!File.Exists(input)) throw new FileNotFoundException("Không tìm thấy tệp: " + input, input);
    }

    private static string BuildNormalizeFilter(RenderOptions settings, double playbackSpeed) =>
        $"setpts=(PTS-STARTPTS)/{playbackSpeed.ToString("0.##", CultureInfo.InvariantCulture)}," +
        $"fps={settings.FrameRate}," +
        $"scale={settings.Width}:{settings.Height}:force_original_aspect_ratio=decrease:flags=lanczos," +
        $"pad={settings.Width}:{settings.Height}:(ow-iw)/2:(oh-ih)/2," +
        "setsar=1,format=yuv420p";

    private static List<string> BuildNormalizeArguments(
        RenderOptions settings, VideoClipOption video, string sourceFile, string clipFile,
        double expectedDuration, bool hasSourceAudio)
    {
        var duration = expectedDuration.ToString("F6", CultureInfo.InvariantCulture);
        var args = new List<string>
        {
            "-nostdin", "-y", "-hide_banner", "-loglevel", "error",
            "-stats_period", "0.5", "-progress", "pipe:1", "-i", sourceFile
        };
        if (!settings.MuteVideoAudio && !hasSourceAudio)
            args.AddRange(["-f", "lavfi", "-i", "anullsrc=channel_layout=stereo:sample_rate=48000"]);

        var videoFilter = BuildNormalizeFilter(settings, video.PlaybackSpeed) +
            ",tpad=stop_mode=clone:stop_duration=0.10," +
            $"trim=duration={duration},setpts=PTS-STARTPTS";
        if (settings.MuteVideoAudio)
            args.AddRange(["-map", "0:v:0", "-an", "-sn", "-dn", "-vf", videoFilter]);
        else
        {
            var audioStart = hasSourceAudio
                ? $"[0:a:0]{PlaybackSpeeds.AudioTempo(video.PlaybackSpeed)},aresample=48000:async=1:first_pts=0,"
                : "[1:a]aresample=48000:async=1:first_pts=0,";
            var graph = $"[0:v]{videoFilter}[vnorm];" + audioStart +
                        $"aformat=channel_layouts=stereo,apad,atrim=duration={duration},asetpts=PTS-STARTPTS[anorm]";
            args.AddRange(["-filter_complex", graph, "-map", "[vnorm]", "-map", "[anorm]"]);
        }
        args.AddRange([
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "21",
            "-pix_fmt", "yuv420p", "-r", settings.FrameRate.ToString(CultureInfo.InvariantCulture),
            "-video_track_timescale", "30000"
        ]);
        if (!settings.MuteVideoAudio)
            args.AddRange(["-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-ac", "2"]);
        args.AddRange(["-t", duration, "-movflags", "+faststart", clipFile]);
        return args;
    }

    private static List<string> BuildOutputArguments(
        RenderOptions settings, string playlistPath, string joinedAudio, string outputFile,
        double audioDuration)
    {
        var args = new List<string>
        {
            "-nostdin", "-y", "-hide_banner", "-loglevel", "error",
            "-stats_period", "0.5", "-progress", "pipe:1",
            "-stream_loop", "-1", // always repeat the entire playlist if audio is longer
            "-safe", "0", "-f", "concat", "-i", playlistPath,
            "-i", joinedAudio
        };
        foreach (var overlay in settings.Overlays)
            args.AddRange(["-loop", "1", "-framerate", settings.FrameRate.ToString(CultureInfo.InvariantCulture), "-i", overlay.ImagePath]);

        var filter = BuildOverlayFilter(settings);
        if (!settings.MuteVideoAudio)
        {
            filter += ";[0:a:0]aresample=async=1:first_pts=0,volume=0.20[gameaudio]" +
                      ";[1:a:0]aresample=async=1:first_pts=0,apad[voice]" +
                      ";[voice][gameaudio]amix=inputs=2:duration=longest:" +
                      "dropout_transition=0:normalize=0,aresample=async=1[aout]";
        }
        args.AddRange(["-filter_complex", filter, "-map", "[vout]", "-map",
            settings.MuteVideoAudio ? "1:a:0" : "[aout]"]);

        if (settings.Encoder == VideoEncoder.Nvenc)
            args.AddRange(["-c:v", "h264_nvenc", "-preset", "p4", "-rc", "vbr", "-cq", "21", "-b:v", "0"]);
        else
            args.AddRange(["-c:v", "libx264", "-preset", "veryfast", "-crf", "20"]);

        // Audio already joined/encoded once; directly copy it when source is muted,
        // preventing unnecessary generational loss and MP3 transition artifacts.
        if (settings.MuteVideoAudio)
            args.AddRange(["-c:a", "copy"]);
        else
            args.AddRange(["-c:a", "aac", "-b:a", "192k", "-ar", "48000"]);

        args.AddRange([
            "-pix_fmt", "yuv420p", "-r", settings.FrameRate.ToString(CultureInfo.InvariantCulture),
            "-t", audioDuration.ToString("F6", CultureInfo.InvariantCulture),
            "-map_metadata", "-1", "-map_chapters", "-1",
            "-movflags", "+faststart", outputFile
        ]);
        return args;
    }

    private static string BuildOverlayFilter(RenderOptions settings)
    {
        var filters = new List<string> { "[0:v]setpts=PTS-STARTPTS[v0]" };
        var maxWidth = Math.Max(16, settings.Width * settings.OverlayWidthPercent / 100);
        var maxHeight = Math.Max(16, settings.Height * settings.OverlayHeightPercent / 100);
        var margin = settings.Width * settings.MarginPercent / 100;
        for (var i = 0; i < settings.Overlays.Count; i++)
        {
            var x = settings.Overlays[i].Corner is Corner.TopLeft or Corner.BottomLeft
                ? margin.ToString(CultureInfo.InvariantCulture)
                : $"W-w-{margin}";
            var y = settings.Overlays[i].Corner is Corner.TopLeft or Corner.TopRight
                ? margin.ToString(CultureInfo.InvariantCulture)
                : $"H-h-{margin}";
            filters.Add($"[{i + 2}:v]scale={maxWidth}:{maxHeight}:" +
                        "force_original_aspect_ratio=decrease:flags=lanczos," +
                        $"format=rgba[logo{i}]");
            filters.Add($"[v{i}][logo{i}]overlay=x={x}:y={y}:" +
                        $"eof_action=repeat:format=auto[v{i + 1}]");
        }
        filters.Add($"[v{settings.Overlays.Count}]format=yuv420p[vout]");
        return string.Join(';', filters);
    }

    private static void ReportFfmpegProgress(string line, double totalSeconds, Action<double> callback)
    {
        if (!line.StartsWith("out_time=", StringComparison.Ordinal)) return;
        if (TimeSpan.TryParse(line[9..], CultureInfo.InvariantCulture, out var time) && totalSeconds > 0)
            callback(Math.Clamp(time.TotalSeconds / totalSeconds, 0, 1));
    }

    private static string GetUniqueOutputFile(string folder, string name, string resolution)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        if (string.IsNullOrEmpty(clean)) clean = "video";
        var prefix = $"{clean}_{resolution}";
        var candidate = Path.Combine(folder, prefix + ".mp4");
        for (var i = 2; File.Exists(candidate); i++)
            candidate = Path.Combine(folder, $"{prefix}_{i}.mp4");
        return candidate;
    }
}
