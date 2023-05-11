﻿using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace PlexCleaner;

public static partial class KeepAwake
{
    public static void PreventSleep()
    {
        // Windows only
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsSystemRequired);
        }
    }

    public static void AllowSleep()
    {
        // Windows only
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetThreadExecutionState(ExecutionState.EsContinuous);
        }
    }

    public static void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        PreventSleep();
    }


    [LibraryImport("kernel32.dll")]
    private static partial ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    [Flags]
    private enum ExecutionState : uint
    {
        // ReSharper disable once UnusedMember.Local
        EsAwayModeRequired = 0x00000040,
        EsContinuous = 0x80000000,
        // ReSharper disable once UnusedMember.Local
        EsDisplayRequired = 0x00000002,
        EsSystemRequired = 0x00000001
    }
}
