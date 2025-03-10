﻿using System.Text.Json.Serialization;

namespace Solitons.Postgres.PgUp.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PgUpScriptDiscoveryMode
{
    /// <summary>
    /// This mode specifies that only the files or patterns explicitly listed in runOrder should be matched and executed.
    /// No additional files from the working directory or subfolders are included.
    /// </summary>
    None = 0,

    /// <summary>
    /// In this mode, the tool will automatically discover and load all files in the specified working directory (shallow search, no subfolders). Files not explicitly matched by runOrder will be executed in alphabetical order after the matched ones.
    /// </summary>
    Shallow = 1,

    /// <summary>
    /// This mode instructs the tool to discover and load all files within the working directory, including subfolders (deep search).
    /// As with shallow discovery, files not explicitly matched by runOrder are executed alphabetically after the matched ones.
    /// </summary>
    Recursive = 2
}