﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using InsaneGenius.Utilities;
using Newtonsoft.Json.Linq;
using Serilog;

// https://handbrake.fr/docs/en/latest/cli/command-line-reference.html

namespace PlexCleaner;

public partial class HandBrakeTool : MediaTool
{
    public override ToolFamily GetToolFamily()
    {
        return ToolFamily.HandBrake;
    }

    public override ToolType GetToolType()
    {
        return ToolType.HandBrake;
    }

    protected override string GetToolNameWindows()
    {
        return "HandBrakeCLI.exe";
    }

    protected override string GetToolNameLinux()
    {
        return "HandBrakeCLI";
    }

    public override bool GetInstalledVersion(out MediaToolInfo mediaToolInfo)
    {
        // Initialize            
        mediaToolInfo = new MediaToolInfo(this);

        // Get version
        const string commandline = "--version";
        var exitCode = Command(commandline, out var output);
        if (exitCode != 0)
        {
            return false;
        }

        // First line as version
        // E.g. Windows : "HandBrake 1.3.3"
        // E.g. Linux : "HandBrake 1.3.3"
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Extract the short version number
        var match = InstalledVersionRegex().Match(lines[0]);
        Debug.Assert(match.Success);
        mediaToolInfo.Version = match.Groups["version"].Value;

        // Get tool filename
        mediaToolInfo.FileName = GetToolPath();

        // Get other attributes if we can read the file
        if (File.Exists(mediaToolInfo.FileName))
        {
            FileInfo fileInfo = new(mediaToolInfo.FileName);
            mediaToolInfo.ModifiedTime = fileInfo.LastWriteTimeUtc;
            mediaToolInfo.Size = fileInfo.Length;
        }

        return true;
    }

    protected override bool GetLatestVersionWindows(out MediaToolInfo mediaToolInfo)
    {
        // Initialize            
        mediaToolInfo = new MediaToolInfo(this);

        try
        {
            // Get the latest release version number from github releases
            // https://api.github.com/repos/handbrake/handbrake/releases/latest
            if (!Download.DownloadString(new Uri(@"https://api.github.com/repos/handbrake/handbrake/releases/latest"), out var json))
            {
                return false;
            }

            var releases = JObject.Parse(json);
            // "tag_name": "1.2.2",
            var versiontag = releases["tag_name"];
            Debug.Assert(versiontag != null);
            mediaToolInfo.Version = versiontag.ToString();

            // Create download URL and the output filename using the version number
            // https://github.com/HandBrake/HandBrake/releases/download/1.3.2/HandBrakeCLI-1.3.2-win-x86_64.zip
            mediaToolInfo.FileName = $"HandBrakeCLI-{mediaToolInfo.Version}-win-x86_64.zip";
            mediaToolInfo.Url = $"https://github.com/HandBrake/HandBrake/releases/download/{mediaToolInfo.Version}/{mediaToolInfo.FileName}";
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            return false;
        }
        return true;
    }

    protected override bool GetLatestVersionLinux(out MediaToolInfo mediaToolInfo)
    {
        // Initialize            
        mediaToolInfo = new MediaToolInfo(this);

        // TODO:
        return false;
    }

    public bool ConvertToMkv(string inputName, string outputName, bool includeSubtitles, bool deInterlace)
    {
        // Delete output file
        FileEx.DeleteFile(outputName);

        // TODO: How to suppress console output when running in parallel mode?
        // if (Program.Options.Parallel)

        // Build commandline
        StringBuilder commandline = new();
        CreateDefaultArgs(inputName, commandline);
        commandline.Append($"--output \"{outputName}\" ");
        commandline.Append("--format av_mkv ");

        // Video encoder options
        // E.g. --encoder x264 --quality 20 --encoder-preset medium
        commandline.Append($"--encoder {Program.Config.ConvertOptions.HandBrakeOptions.Video} ");

        // Deinterlace using decomb filter
        if (deInterlace)
        {
            commandline.Append("--comb-detect --decomb ");
        }

        // All audio with encoder
        // E.g. --all-audio --aencoder copy --audio-fallback ac3
        commandline.Append($"--all-audio --aencoder {Program.Config.ConvertOptions.HandBrakeOptions.Audio} ");

        // All or no subtitles
        commandline.Append(includeSubtitles ? "--all-subtitles " : "--subtitle none ");

        // Execute
        var exitCode = Command(commandline.ToString());
        return exitCode == 0;
    }

    private static void CreateDefaultArgs(string inputName, StringBuilder commandline)
    {
        commandline.Append($"--input \"{inputName}\" ");
        if (Program.Options.TestSnippets)
        {
            commandline.Append($"--start-at seconds:00 --stop-at seconds:{(int)Program.SnippetTimeSpan.TotalSeconds} ");
        }
    }

    const string VersionPattern = @"HandBrake\ (?<version>.*)";
    [GeneratedRegex(VersionPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    internal static partial Regex InstalledVersionRegex();
}
