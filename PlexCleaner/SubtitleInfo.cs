﻿using System;
using Serilog;

namespace PlexCleaner;

public class SubtitleInfo : TrackInfo
{
    internal SubtitleInfo(MkvToolJsonSchema.Track track) : base(track)
    {
    }
    internal SubtitleInfo(FfMpegToolJsonSchema.Stream stream) : base(stream)
    {
    }

    internal SubtitleInfo(MediaInfoToolXmlSchema.Track track) : base(track)
    {
        // We need MuxingMode for VOBSUB else Plex on Nvidia Shield TV will hang on play start
        // https://forums.plex.tv/discussion/290723/long-wait-time-before-playing-some-content-player-says-directplay-server-says-transcoding
        // https://github.com/mbunkus/mkvtoolnix/issues/2131
        // https://gitlab.com/mbunkus/mkvtoolnix/-/issues/2131
        if (track.CodecId.Equals("S_VOBSUB", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(track.MuxingMode))
        {
            // TODO: Not fixed by remuxing
            Log.Logger.Warning("MediaInfoToolXmlSchema : MuxingMode not specified for S_VOBSUB Codec");
            HasErrors = true;
        }
    }
}
