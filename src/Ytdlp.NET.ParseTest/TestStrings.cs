namespace Ytdlp.NET.ParseTest;

internal static class TestStrings
{
    public static string Test1()
    {
        return 
            $"[info] Available formats for UPbqukYBy_E:\r\n" +
            $"ID  EXT   RESOLUTION FPS CH │   FILESIZE   TBR PROTO │ VCODEC          VBR ACODEC      ABR ASR MORE INFO\r\n" +
            $"─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────\r\n" +
            $"sb3 mhtml 48x27        0    │                  mhtml │ images                                  storyboard\r\n" +
            $"sb2 mhtml 80x45        0    │                  mhtml │ images                                  storyboard\r\n" +
            $"sb1 mhtml 160x90       0    │                  mhtml │ images                                  storyboard\r\n" +
            $"sb0 mhtml 320x180      0    │                  mhtml │ images                                  storyboard\r\n" +
            $"139 m4a   audio only      2 │    7.03MiB   49k https │ audio only          mp4a.40.5   49k 22k [ml] low, m4a_dash\r\n" +
            $"249 webm  audio only      2 │    7.30MiB   51k https │ audio only          opus        51k 48k [ml] low, webm_dash\r\n" +
            $"140 m4a   audio only      2 │   18.66MiB  129k https │ audio only          mp4a.40.2  129k 44k [ml] medium, m4a_dash\r\n" +
            $"251 webm  audio only      2 │   15.45MiB  107k https │ audio only          opus       107k 48k [ml] medium, webm_dash\r\n" +
            $"160 mp4   256x144     30    │    8.40MiB   58k https │ avc1.4d400c     58k video only          144p, mp4_dash\r\n" +
            $"278 webm  256x144     30    │   12.04MiB   84k https │ vp9             84k video only          144p, webm_dash\r\n" +
            $"394 mp4   256x144     30    │    6.59MiB   46k https │ av01.0.00M.08   46k video only          144p, mp4_dash\r\n" +
            $"133 mp4   426x240     30    │   18.60MiB  129k https │ avc1.4d4015    129k video only          240p, mp4_dash\r\n" +
            $"242 webm  426x240     30    │   19.98MiB  139k https │ vp9            139k video only          240p, webm_dash\r\n" +
            $"395 mp4   426x240     30    │   13.02MiB   90k https │ av01.0.00M.08   90k video only          240p, mp4_dash\r\n" +
            $"134 mp4   640x360     30    │   35.97MiB  250k https │ avc1.4d401e    250k video only          360p, mp4_dash\r\n" +
            $"18  mp4   640x360     30  2 │   49.71MiB  345k https │ avc1.42001E         mp4a.40.2       44k [ml] 360p\r\n" +
            $"243 webm  640x360     30    │   44.05MiB  306k https │ vp9            306k video only          360p, webm_dash\r\n" +
            $"396 mp4   640x360     30    │   27.47MiB  191k https │ av01.0.01M.08  191k video only          360p, mp4_dash\r\n" +
            $"135 mp4   854x480     30    │   70.86MiB  492k https │ avc1.4d401f    492k video only          480p, mp4_dash\r\n" +
            $"244 webm  854x480     30    │   73.40MiB  509k https │ vp9            509k video only          480p, webm_dash\r\n" +
            $"397 mp4   854x480     30    │   45.50MiB  316k https │ av01.0.04M.08  316k video only          480p, mp4_dash\r\n" +
            $"136 mp4   1280x720    30    │  138.47MiB  961k https │ avc1.64001f    961k video only          720p, mp4_dash\r\n" +
            $"247 webm  1280x720    30    │  152.78MiB 1060k https │ vp9           1060k video only          720p, webm_dash\r\n" +
            $"398 mp4   1280x720    30    │   88.68MiB  615k https │ av01.0.05M.08  615k video only          720p, mp4_dash\r\n" +
            $"137 mp4   1920x1080   30    │  271.92MiB 1887k https │ avc1.640028   1887k video only          1080p, mp4_dash\r\n" +
            $"248 webm  1920x1080   30    │  268.95MiB 1866k https │ vp9           1866k video only          1080p, webm_dash\r\n" +
            $"399 mp4   1920x1080   30    │  177.70MiB 1233k https │ av01.0.08M.08 1233k video only          1080p, mp4_dash";
    }

    public static string Test2()
    {
        return 
            $"ID  EXT   RESOLUTION FPS CH │   FILESIZE   TBR PROTO │ VCODEC        VBR ACODEC      ABR ASR MORE INFO\r\n" +
            $"───────────────────────────────────────────────────────────────────────────────────────────────────────────────────\r\n" +
            $"sb3 mhtml 48x27        0    │                  mhtml │ images                                storyboard\r\n" +
            $"sb2 mhtml 80x45        0    │                  mhtml │ images                                storyboard\r\n" +
            $"sb1 mhtml 160x90       0    │                  mhtml │ images                                storyboard\r\n" +
            $"sb0 mhtml 320x180      0    │                  mhtml │ images                                storyboard\r\n" +
            $"139 m4a   audio only      2 │    3.33MiB   49k https │ audio only        mp4a.40.5   49k 22k [ml] low, m4a_dash\r\n" +
            $"249 webm  audio only      2 │    3.47MiB   51k https │ audio only        opus        51k 48k [ml] low, webm_dash\r\n" +
            $"140 m4a   audio only      2 │    8.83MiB  129k https │ audio only        mp4a.40.2  129k 44k [ml] medium, m4a_dash\r\n" +
            $"251 webm  audio only      2 │    8.71MiB  128k https │ audio only        opus       128k 48k [ml] medium, webm_dash\r\n" +
            $"160 mp4   256x144     30    │    6.33MiB   93k https │ avc1.4d400c   93k video only          144p, mp4_dash\r\n" +
            $"278 webm  256x144     30    │    5.84MiB   86k https │ vp9           86k video only          144p, webm_dash\r\n" +
            $"133 mp4   426x240     30    │   12.74MiB  187k https │ avc1.4d4015  187k video only          240p, mp4_dash\r\n" +
            $"242 webm  426x240     30    │   11.11MiB  163k https │ vp9          163k video only          240p, webm_dash\r\n" +
            $"134 mp4   640x360     30    │   24.41MiB  358k https │ avc1.4d401e  358k video only          360p, mp4_dash\r\n" +
            $"18  mp4   640x360     30  2 │   30.95MiB  454k https │ avc1.42001E       mp4a.40.2       44k [ml] 360p\r\n" +
            $"243 webm  640x360     30    │   21.32MiB  313k https │ vp9          313k video only          360p, webm_dash\r\n" +
            $"135 mp4   854x480     30    │   40.63MiB  596k https │ avc1.4d401f  596k video only          480p, mp4_dash\r\n" +
            $"244 webm  854x480     30    │   36.22MiB  531k https │ vp9          531k video only          480p, webm_dash\r\n" +
            $"136 mp4   1280x720    30    │   80.08MiB 1174k https │ avc1.64001f 1174k video only          720p, mp4_dash\r\n" +
            $"247 webm  1280x720    30    │   72.21MiB 1059k https │ vp9         1059k video only          720p, webm_dash\r\n" +
            $"137 mp4   1920x1080   30    │  145.93MiB 2140k https │ avc1.640028 2140k video only          1080p, mp4_dash\r\n" +
            $"248 webm  1920x1080   30    │  132.09MiB 1937k https │ vp9         1937k video only          1080p, webm_dash";
    }
}
