using InsaneGenius.Utilities;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace PlexCleaner;

public class TrackInfo
{
    protected TrackInfo() { }

    internal TrackInfo(MkvToolJsonSchema.Track track)
    {
        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }

        Format = track.Codec;
        Codec = track.Properties.CodecId;
        Title = track.Properties.TrackName;
        Default = track.Properties.DefaultTrack;

        // If the "language" and "tag_language" fields are set FFprobe uses the tag language instead of the track language
        // https://github.com/MediaArea/MediaAreaXml/issues/34
        if (!string.IsNullOrEmpty(track.Properties.TagLanguage) &&
            !string.IsNullOrEmpty(track.Properties.Language) &&
            !track.Properties.Language.Equals(track.Properties.TagLanguage, StringComparison.OrdinalIgnoreCase))
        {
            HasErrors = true;
            Log.Logger.Warning("Tag and Track Language Mismatch : {TagLanguage} != {Language}", track.Properties.TagLanguage, track.Properties.Language);
        }

        // Set language
        if (string.IsNullOrEmpty(track.Properties.Language))
        {
            Language = "und";
        }
        else
        {
            // MKVMerge normally sets the language to und or 3 letter ISO 639-2 code
            // Try to lookup the language to make sure it is correct
            Iso6393 lang = PlexCleaner.Language.GetIso6393(track.Properties.Language);
            if (lang != null)
            {
                Language = lang.Part2B;
            }
            else
            {
                HasErrors = true;
                Log.Logger.Warning("Invalid Language : {Language}", track.Properties.Language);
                Language = "und";
            }
        }

        // Take care to use id and number correctly in MKVMerge and MKVPropEdit
        Id = track.Id;
        Number = track.Properties.Number;

        // Verify required info
        Debug.Assert(!string.IsNullOrEmpty(Format));
        Debug.Assert(!string.IsNullOrEmpty(Codec));
    }

    internal TrackInfo(FfMpegToolJsonSchema.Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        Format = stream.CodecName;
        Codec = stream.CodecLongName;
        Title = stream.Tags.Title;
        Default = stream.Disposition.Default;

        // TODO : FFprobe uses the tag language value instead of the track language
        // Some files show MediaInfo and MKVMerge say language is "eng", FFprobe says language is "und"
        // https://github.com/MediaArea/MediaAreaXml/issues/34

        // Set language
        if (string.IsNullOrEmpty(stream.Tags.Language))
        {
            Language = "und";
        }
        // Some sample files are "???" or "null", set to und
        else if (stream.Tags.Language.Equals("???", StringComparison.OrdinalIgnoreCase) ||
                 stream.Tags.Language.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            HasErrors = true;
            Log.Logger.Warning("Invalid Language : {Language}", stream.Tags.Language);
            Language = "und";
        }
        else
        {
            // FFprobe normally sets a 3 letter ISO 639-2 code, but some samples have 2 letter codes
            // Try to lookup the language to make sure it is correct
            Iso6393 lang = PlexCleaner.Language.GetIso6393(stream.Tags.Language);
            if (lang != null)
            {
                Language = lang.Part2B;
            }
            else
            {
                HasErrors = true;
                Log.Logger.Warning("Invalid Language : {Language}", stream.Tags.Language);
                Language = "und";
            }
        }

        // Use index for number
        Id = stream.Index;
        Number = stream.Index;

        // Verify required info
        Debug.Assert(!string.IsNullOrEmpty(Format));
        Debug.Assert(!string.IsNullOrEmpty(Codec));
    }

    internal TrackInfo(MediaInfoToolXmlSchema.Track track)
    {
        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }

        Format = track.Format;
        Codec = track.CodecId;
        Title = track.Title;
        Default = track.Default;

        // Set language
        if (string.IsNullOrEmpty(track.Language))
        {
            Language = "und";
        }
        else
        {
            // MediaInfo uses ab or abc or ab-cd tags, we need to convert to ISO 639-2
            // https://github.com/MediaArea/MediaAreaXml/issues/33
            // Try to lookup the language to make sure it is correct
            Iso6393 lang = PlexCleaner.Language.GetIso6393(track.Language);
            if (lang != null)
            {
                Language = lang.Part2B;
            }
            else
            {
                HasErrors = true;
                Log.Logger.Warning("Invalid Language : {Language}", track.Language);
                Language = "und";
            }
        }

        // FFprobe and MKVToolNix use chi not zho
        // https://github.com/mbunkus/mkvtoolnix/issues/1149
        if (Language.Equals("zho", StringComparison.OrdinalIgnoreCase))
        {
            Language = "chi";
        }

        // ID can be an integer or an integer-type, e.g. 3-CC1
        // https://github.com/MediaArea/MediaInfo/issues/201
        Id = int.Parse(track.Id.All(char.IsDigit) ? track.Id : track.Id[..track.Id.IndexOf('-', StringComparison.OrdinalIgnoreCase)], CultureInfo.InvariantCulture);

        // Use streamorder for number
        Number = track.StreamOrder;

        // Verify required info
        Debug.Assert(!string.IsNullOrEmpty(Format));
        Debug.Assert(!string.IsNullOrEmpty(Codec));
    }

    public string Format { get; set; } = "";
    public string Codec { get; set; } = "";
    public string Language { get; set; } = "";
    public int Id { get; set; }
    public int Number { get; set; }
    public enum StateType { None, Keep, Remove, ReMux, ReEncode, DeInterlace }
    public StateType State { get; set; } = StateType.None;
    public string Title { get; set; } = "";
    public bool Default { get; set; }
    public bool HasErrors { get; set; }

    public bool IsLanguageUnknown()
    {
        // Test for empty or "und" field values
        return string.IsNullOrEmpty(Language) ||
               Language.Equals("und", StringComparison.OrdinalIgnoreCase);
    }

    public virtual void WriteLine(string prefix)
    {
        Log.Logger.Information("{Prefix} : Type: {Type}, Format: {Format}, Codec: {Codec}, Language: {Language}, Id: {Id}, Number: {Number}, State: {State}, Title: {Title}, Default: {Default}, HasErrors: {HasErrors}",
            prefix,
            GetType().Name,
            Format,
            Codec,
            Language,
            Id,
            Number,
            State,
            Title,
            Default,
            HasErrors);
    }
}