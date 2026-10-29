using System.Diagnostics;
using System.Text;

namespace LangKinhHuKhong.Editer.Core;

public static class ProcessRunner
{
    public static async Task<string> RunAsync(
        string executable,
        IEnumerable<string> arguments,
        Action<string>? onStdout,
        CancellationToken cancellationToken,
        string? workingDirectory = null)
    {
        var info = new ProcessStartInfo(executable)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = info };
        if (!process.Start())
            throw new InvalidOperationException($"Không thể khởi động {executable}.");

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        });

        // Đọc song song để tránh kẹt pipe stdout/stderr khi FFmpeg xuất log dài.
        var stderr = new StringBuilder();
        var stdout = new StringBuilder();
        var stdoutTask = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                if (onStdout is not null)
                    onStdout(line);
                else if (stdout.Length < 256 * 1024)
                    stdout.AppendLine(line);
            }
        });
        var stderrTask = Task.Run(async () =>
        {
            while (await process.StandardError.ReadLineAsync() is { } line)
            {
                // Giới hạn log lỗi trong bộ nhớ.
                if (stderr.Length >= 64 * 1024)
                    stderr.Remove(0, Math.Min(stderr.Length, 8 * 1024));
                stderr.AppendLine(line);
            }
        });

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            // Đảm bảo FFmpeg đã thoát trước khi xóa thư mục làm việc.
            await process.WaitForExitAsync();
            await Task.WhenAll(stdoutTask, stderrTask);
            throw;
        }
        await Task.WhenAll(stdoutTask, stderrTask);
        cancellationToken.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{Path.GetFileName(executable)} trả về mã {process.ExitCode}:\n" + stderr);

        return stdout.ToString();
    }
}
