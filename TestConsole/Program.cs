using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                string str =
                    @"{""Processed"":100,""UniquePhrases"":97,""Errors"":0,
""Detailed"":[
{""Searcher"":""null"",
""Matches"":""0"",
""Url"":""https://abit.pskgu.ru/file/download/umrs/588A465DBA587389065553BB96C9347B"",
""SuccessQuery"":false,
""Comment"":""( The request timed out )""
},
{""Searcher"":""Ya"",""Matches"":2,""Url"":""https://coronavirus-monitor.ru/"",""SuccessQuery"":true,""Comment"":null},
{""Searcher"":null,""Matches"":0,""Url"":""http://newukraineinstitute.org/media/news/950/file/\\u041F\\u0440\\u0435\\u0437\\u0438\\u0434\\u0435\\u043D\\u0442\\u0441\\u043A\\u0430\\u044F \\u043A\\u0430\\u043C\\u043F\\u0430\\u043D\\u0438\\u044F-\\u0441\\u0442\\u0430\\u0440\\u0442_2019.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":""Ya"",""Matches"":3,""Url"":""https://ru.wikipedia.org/wiki/\\u0413\\u0440\\u0430\\u0434\\u0438\\u0435\\u043D\\u0442\\u043D\\u044B\\u0439_\\u0441\\u043F\\u0443\\u0441\\u043A"",""SuccessQuery"":true,""Comment"":null},{""Searcher"":null,""Matches"":0,""Url"":""http://olympiads.mccme.ru/mfo/experiment/experiment.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""http://nbuv.gov.ua/j-pdf/Vejpte_2015_6(2)__8.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":""Yah"",""Matches"":1,""Url"":""https://en.wikipedia.org/wiki/Euler\\u0027s_formula"",""SuccessQuery"":true,""Comment"":null},{""Searcher"":null,""Matches"":0,""Url"":""http://miigaik.ru/vtiaoai/tutorials/19.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":""Ya"",""Matches"":2,""Url"":""https://habr.com/ru/post/459922/"",""SuccessQuery"":true,""Comment"":null},{""Searcher"":null,""Matches"":0,""Url"":""http://www.machinelearning.ru/wiki/images/archive/3/34/20140423085331!Rodomanov-fast-gradient-methods.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""http://scipp.ucsc.edu/~haber/ph116A/clog_11.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":""Go"",""Matches"":1,""Url"":""https://ru.bmstu.wiki/\\u041C\\u0435\\u0434\\u0438\\u0430\\u043D\\u043D\\u0430\\u044F_\\u0444\\u0438\\u043B\\u044C\\u0442\\u0440\\u0430\\u0446\\u0438\\u044F"",""SuccessQuery"":true,""Comment"":null},{""Searcher"":null,""Matches"":0,""Url"":""https://math.spbu.ru/user/gran/students/Danilova.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":""Yah"",""Matches"":2,""Url"":""https://studizba.com/lectures/129-inzhenerija/1850-informacionnye-ustrojstva-i-sistemy/36195-6-sistemy-tehnicheskogo-zrenija.html"",""SuccessQuery"":true,""Comment"":null},{""Searcher"":null,""Matches"":0,""Url"":""https://www.youtube.com/watch?v=WnO-TwfxLk4"",""SuccessQuery"":false,""Comment"":null},{""Searcher"":null,""Matches"":0,""Url"":""http://window.edu.ru/resource/374/69374/files/tfkp_fmo.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""https://mipt.ru/education/chair/mathematics/study/uchebniki/\\u041B_\\u0422\\u0424\\u041A\\u041F_\\u0413\\u043E\\u0440\\u044F\\u0439\\u043D\\u043E\\u0432_\\u041F\\u043E\\u043B\\u043E\\u0432\\u0438\\u043D\\u043A\\u0438\\u043D(2).pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""http://www.irbis-nbuv.gov.ua/cgi-bin/irbis_nbuv/cgiirbis_64.exe?C21COM=2\\u0026I21DBN=UJRN\\u0026P21DBN=UJRN\\u0026IMAGE_FILE_DOWNLOAD=1\\u0026Image_file_name=PDF/vikt_2014_64_21.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""https://www.graphicon.ru/html/2007/proceedings/Papers/Paper_20.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""https://www.iae.nsk.su/images/stories/5_Autometria/5_Archives/2018/2/06_surin.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""},{""Searcher"":null,""Matches"":0,""Url"":""https://mk.cs.msu.ru/images/9/9a/Dmus1-selezn.pdf"",""SuccessQuery"":false,""Comment"":""( The request timed out )""}]}";

                var res = JsonSerializer.Deserialize<Dictionary<string, object>>(str, new JsonSerializerOptions());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}