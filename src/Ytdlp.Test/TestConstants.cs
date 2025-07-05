namespace Ytdlp.Test;

internal static class TestConstants
{
    public static string GetAvailableFormats()
    {
        return @"[info] Available formats for Gk0WHyRUcgM:
ID      EXT   RESOLUTION FPS CH |  FILESIZE   TBR PROTO | VCODEC          VBR ACODEC      ABR ASR MORE INFO
------------------------------------------------------------------------------------------------------------------------------
sb3     mhtml 48x27        0    |                 mhtml | images                                  storyboard
sb2     mhtml 80x45        1    |                 mhtml | images                                  storyboard
sb1     mhtml 160x90       1    |                 mhtml | images                                  storyboard
sb0     mhtml 320x180      1    |                 mhtml | images                                  storyboard
233     mp4   audio only        |                 m3u8  | audio only          unknown             [ml] Untested, Default, low
234     mp4   audio only        |                 m3u8  | audio only          unknown             [ml] Untested, Default, high
249-drc webm  audio only      2 |   1.64MiB   54k https | audio only          opus        54k 48k [ml] low, DRC, webm_dash
250-drc webm  audio only      2 |   2.06MiB   68k https | audio only          opus        68k 48k [ml] low, DRC, webm_dash
249     webm  audio only      2 |   1.63MiB   54k https | audio only          opus        54k 48k [ml] low, webm_dash
250     webm  audio only      2 |   2.06MiB   68k https | audio only          opus        68k 48k [ml] low, webm_dash
140-drc m4a   audio only      2 |   3.91MiB  130k https | audio only          mp4a.40.2  130k 44k [ml] medium, DRC, m4a_dash
251-drc webm  audio only      2 |   3.50MiB  116k https | audio only          opus       116k 48k [ml] medium, DRC, webm_dash
140     m4a   audio only      2 |   3.91MiB  129k https | audio only          mp4a.40.2  129k 44k [ml] medium, m4a_dash
251     webm  audio only      2 |   3.49MiB  116k https | audio only          opus       116k 48k [ml] medium, webm_dash
602     mp4   256x144     13    | ~ 2.67MiB   89k m3u8  | vp09.00.10.08   89k video only          Untested
269     mp4   256x144     25    | ~ 5.17MiB  171k m3u8  | avc1.4D400C    171k video only          Untested
160     mp4   256x144     25    |   2.17MiB   72k https | avc1.4d400c     72k video only          144p, mp4_dash
603     mp4   256x144     25    | ~ 4.72MiB  157k m3u8  | vp09.00.11.08  157k video only          Untested
278     webm  256x144     25    |   2.28MiB   76k https | vp9             76k video only          144p, webm_dash
229     mp4   426x240     25    | ~ 8.71MiB  289k m3u8  | avc1.4D4015    289k video only          Untested
133     mp4   426x240     25    |   4.50MiB  149k https | avc1.4d4015    149k video only          240p, mp4_dash
604     mp4   426x240     25    | ~ 7.27MiB  241k m3u8  | vp09.00.20.08  241k video only          Untested
242     webm  426x240     25    |   3.73MiB  124k https | vp9            124k video only          240p, webm_dash
230     mp4   640x360     25    | ~16.61MiB  551k m3u8  | avc1.4D401E    551k video only          Untested
134     mp4   640x360     25    |   8.06MiB  267k https | avc1.4d401e    267k video only          360p, mp4_dash
18      mp4   640x360     25  2 |   9.50MiB  315k https | avc1.42001E         mp4a.40.2       22k [ml] 360p
605     mp4   640x360     25    | ~13.02MiB  432k m3u8  | vp09.00.21.08  432k video only          Untested
243     webm  640x360     25    |   6.02MiB  199k https | vp9            199k video only          360p, webm_dash
231     mp4   854x480     25    | ~30.08MiB  997k m3u8  | avc1.4D401E    997k video only          Untested
135     mp4   854x480     25    |  16.47MiB  546k https | avc1.4d401e    546k video only          480p, mp4_dash
606     mp4   854x480     25    | ~18.73MiB  621k m3u8  | vp09.00.30.08  621k video only          Untested
244     webm  854x480     25    |   9.55MiB  317k https | vp9            317k video only          480p, webm_dash
232     mp4   1280x720    25    | ~49.59MiB 1644k m3u8  | avc1.64001F   1644k video only          Untested
136     mp4   1280x720    25    |  27.89MiB  924k https | avc1.64001f    924k video only          720p, mp4_dash
609     mp4   1280x720    25    | ~27.93MiB  926k m3u8  | vp09.00.31.08  926k video only          Untested
247     webm  1280x720    25    |  16.95MiB  562k https | vp9            562k video only          720p, webm_dash
270     mp4   1920x1080   25    | ~87.56MiB 2903k m3u8  | avc1.640028   2903k video only          Untested
137     mp4   1920x1080   25    |  52.55MiB 1742k https | avc1.640028   1742k video only          1080p, mp4_dash
614     mp4   1920x1080   25    | ~47.46MiB 1574k m3u8  | vp09.00.40.08 1574k video only          Untested
248     webm  1920x1080   25    |  29.53MiB  979k https | vp9            979k video only          1080p, webm_dash
";
    }    
}
