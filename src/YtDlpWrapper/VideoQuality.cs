using System.ComponentModel;

namespace YtDlpWrapper;

public enum VideoQuality
{
    All,           // All available quality
    MergeAll,      // Merge all available formats
    Best,          // Best available quality
    BestVideo,     // Best video-only quality (no audio)
    Worst,         // Worst available quality    
    WorstVideo,    // Worst video-only quality (no audio)   
}
