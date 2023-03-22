﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PlexCleaner;

public class SelectMediaInfo
{
    public SelectMediaInfo(MediaTool.ToolType parser)
    {
        Selected = new MediaInfo(parser);
        NotSelected = new MediaInfo(parser);
    }

    public SelectMediaInfo(MediaInfo mediaInfo, Func<TrackInfo, bool> selectFunc)
    {
        Selected = new MediaInfo(mediaInfo.Parser);
        NotSelected = new MediaInfo(mediaInfo.Parser);
        Add(mediaInfo, selectFunc);
    }

    public SelectMediaInfo(MediaInfo mediaInfo, bool select)
    {
        Selected = new MediaInfo(mediaInfo.Parser);
        NotSelected = new MediaInfo(mediaInfo.Parser);
        Add(mediaInfo, select);
    }

    public MediaInfo Selected;
    public MediaInfo NotSelected;

    private MediaInfo Select(bool select)
    {
        return select ? Selected : NotSelected;
    }

    public void Add(MediaInfo mediaInfo, Func<TrackInfo, bool> selectFunc)
    {
        Debug.Assert(mediaInfo.Parser == Selected.Parser);
        Debug.Assert(mediaInfo.Parser == NotSelected.Parser);
        Add(mediaInfo.Video, selectFunc);
        Add(mediaInfo.Audio, selectFunc);
        Add(mediaInfo.Subtitle, selectFunc);
    }

    public void Add(MediaInfo mediaInfo, bool select)
    {
        Debug.Assert(mediaInfo.Parser == Selected.Parser);
        Debug.Assert(mediaInfo.Parser == NotSelected.Parser);
        Select(select).Video.AddRange(mediaInfo.Video);
        Select(select).Audio.AddRange(mediaInfo.Audio);
        Select(select).Subtitle.AddRange(mediaInfo.Subtitle);
    }

    public void Add(IEnumerable<TrackInfo> trackList, Func<TrackInfo, bool> selectFunc)
    {
        foreach (var trackInfo in trackList)
        {
            Add(trackInfo, selectFunc(trackInfo));
        }
    }

    public void Add(IEnumerable<TrackInfo> trackList, bool select)
    {
        foreach (var trackInfo in trackList)
        {
            Add(trackInfo, select);
        }
    }

    public void Add(TrackInfo trackInfo, Func<TrackInfo, bool> selectFunc)
    {
        Add(trackInfo, selectFunc(trackInfo));
    }

    public void Add(TrackInfo trackInfo, bool select)
    {
        switch (trackInfo)
        {
            case VideoInfo info:
                Select(select).Video.Add(info);
                break;
            case AudioInfo info:
                Select(select).Audio.Add(info);
                break;
            case SubtitleInfo info:
                Select(select).Subtitle.Add(info);
                break;
            default:
                throw new ArgumentException(null, nameof(trackInfo));
        }
    }

    public void Move(MediaInfo mediaInfo, bool select)
    {
        Debug.Assert(mediaInfo.Parser == Selected.Parser);
        Debug.Assert(mediaInfo.Parser == NotSelected.Parser);
        Move(mediaInfo.Video, select);
        Move(mediaInfo.Audio, select);
        Move(mediaInfo.Subtitle, select);
    }

    public void Move(IEnumerable<TrackInfo> trackList, Func<TrackInfo, bool> selectFunc)
    {
        foreach (var trackInfo in trackList)
        {
            Move(trackInfo, selectFunc(trackInfo));
        }
    }

    public void Move(IEnumerable<TrackInfo> trackList, bool select)
    {
        foreach (var trackInfo in trackList)
        {
            Move(trackInfo, select);
        }
    }

    public void Move(TrackInfo trackInfo, Func<TrackInfo, bool> selectFunc)
    {
        Move(trackInfo, selectFunc(trackInfo));
    }

    public void Move(TrackInfo trackInfo, bool select)
    {
        switch (trackInfo)
        {
            case VideoInfo info:
                Selected.Video.Remove(info);
                NotSelected.Video.Remove(info);
                Select(select).Video.Add(info);
                break;
            case AudioInfo info:
                Selected.Audio.Remove(info);
                NotSelected.Audio.Remove(info);
                Select(select).Audio.Add(info);
                break;
            case SubtitleInfo info:
                Selected.Subtitle.Remove(info);
                NotSelected.Subtitle.Remove(info);
                Select(select).Subtitle.Add(info);
                break;
            default:
                throw new ArgumentException(null, nameof(trackInfo));
        }
    }

    public void SetState(TrackInfo.StateType selectState, TrackInfo.StateType notSelectState)
    {
        Selected.Video.ForEach(item => item.State = selectState);
        Selected.Audio.ForEach(item => item.State = selectState);
        Selected.Subtitle.ForEach(item => item.State = selectState);

        NotSelected.Video.ForEach(item => item.State = notSelectState);
        NotSelected.Audio.ForEach(item => item.State = notSelectState);
        NotSelected.Subtitle.ForEach(item => item.State = notSelectState);
    }

    public void WriteLine(string selected, string notSelected)
    {
        Selected.WriteLine(selected);
        NotSelected.WriteLine(notSelected);
    }
}
