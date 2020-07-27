using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Logging;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using Transport.Abstraction;

namespace ETxtWorker
{
    public class ETxtAgent : AgentAbstract
    {
        private readonly ILogger<ETxtAgent> _logger;
        private const string ETxtPathX86 = "C:\\Program Files (x86)\\Etxt Antiplagiat";
        private const string ETxtPath = "C:\\Program Files\\Etxt Antiplagiat";

        public ETxtAgent(ITransportSender transport, ILogger<ETxtAgent> logger) : base(transport, AgentType.Worker,
            "ETxt", MessageType.Unknown,
            MessageType.WorkerTask)
        {
            AllowConcurrency = false;
            _logger = logger;
        }

        private async Task<(bool isLaunched, UIA3Automation automation, AutomationElement window, Application app)>
            StartAndCheckForUpdateAsync()
        {
            var launchPath = Path.Combine(ETxtPathX86, "EtxtAntiplagiat.exe");
            if (!File.Exists(launchPath))
            {
                launchPath = Path.Combine(ETxtPath, "EtxtAntiplagiat.exe");
                if (!File.Exists(launchPath))
                    return (false, null, null, null);
            }

            var app = FlaUI.Core.Application.Launch(launchPath);
            using var automation = new UIA3Automation();
            app.WaitWhileBusy();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(1));
            while (window == null || window.FrameworkType != FrameworkType.Wpf)
            {
                await Task.Delay(2000);
                try
                {
                    window = FlaUI.Core.Application.Attach(app.ProcessId)
                        .GetMainWindow(automation, TimeSpan.FromSeconds(1));
                    if (window != null)
                    {
                        Console.WriteLine(window.Title);
                    }
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            var later = // window.FindFirstDescendant(x => x.ByAutomationId("cancelUpdateButton")).AsButton();
                window.FindAllChildren(wnd => wnd.ByControlType(ControlType.Window))
                    .FirstOrDefault(wnd => wnd.Name.Contains("Доступна новая версия программы"))
                    ?.FindFirstChild(upd => upd.ByAutomationId("titleBar"))
                    ?.FindAllChildren(ttl => ttl.ByControlType(ControlType.Button))
                    ?.LastOrDefault(btn => btn.AutomationId == "")
                    ?.AsButton();
            later?.Invoke();
            var newVer = window.FindAllChildren(wnd => wnd.ByControlType(ControlType.Window))
                .FirstOrDefault(wnd => wnd.Name.Contains("Доступна новая версия программы"));
            while (newVer != null)
            {
                await Task.Delay(500);
                newVer = window.FindAllChildren(wnd => wnd.ByControlType(ControlType.Window))
                    .FirstOrDefault(wnd => wnd.Name.Contains("Доступна новая версия программы"));
            }
            return (true, automation, window, app);
        }

        private async Task OpenFileAsync(AutomationElement window, string filePath)
        {
            var openFile = window
                .FindFirstChild(x => x.ByAutomationId("openFileToolStripButton")).AsButton();
            openFile.Invoke();
            AutomationElement openWindow = null;
            while (openWindow == null)
            {
                _logger.LogInformation("Waiting for openwindow");
                await Task.Delay(500);
                openWindow = window
                    .FindFirstChild(x => x.ByControlType(ControlType.Window).And(x.ByName("Открыть файл")));
            }
            _logger.LogInformation($"openwindow childs: {openWindow.FindAllChildren().Length}");

            var edit = openWindow
                .FindFirstChild(x =>
                    x.ByControlType(ControlType.ComboBox).And(x.ByAutomationId("1148") /*x.ByName("Имя файла:"*/))
                .FindFirstChild(x => x.ByControlType(ControlType.Edit))
                .AsTextBox();
            edit.Text = filePath;

            var openButton = openWindow
                .FindFirstChild(x => x.ByControlType(ControlType.Button).And(x.ByAutomationId("1") /*Открыть*/))
                .AsButton();
            openButton.Invoke();
            await Task.Delay(2000);
        }

        private async Task<ETxtResult> MakeCheckAsync(AutomationElement window, bool isFullCheck, Guid taskId,
            Guid parentId)
        {
            var checkButton = window
                .FindFirstDescendant(x =>
                    x.ByAutomationId(isFullCheck ? "rewriteCheckBigButton" : "standartCheckBigButton")).AsButton();
            checkButton.Invoke();

            // AdvegoResult result = new AdvegoResult();
            double processed = 0;
            while (Math.Abs(processed - 100) > 0.001)
            {
                var compited = window.FindFirstDescendant(x => x.ByAutomationId("totalProcessLabel"))
                    .AsTextBox();
                if (compited.Name == "Готово")
                {
                    processed = 100;
                    var detailed = new List<ETxtDetailed>();
                    var jrn = window.FindFirstChild(x => x.ByAutomationId("journalWebBrowserHost"))
                        .FindFirstDescendant(x => x.ByName("about:blank"))
                        .FindFirstChild()
                        .FindAllChildren()
                        .Select(x => x.AsTextBox())
                        .ToList();
                    bool isEnd = false;
                    double error = 0, unique = 0;
                    for (int i = 0; i < jrn.Count; i++)
                    {
                        if (!isEnd && jrn[i].Name.StartsWith("["))
                        {
                            if (jrn[i].Name.Contains("Тип проверки"))
                            {
                                isEnd = true;
                            }
                            else if (jrn[i].Name.Contains(" "))
                            {
                                var d = new ETxtDetailed
                                {
                                    SuccessQuery = false,
                                    Url = jrn[i + 1].Name
                                };
                                if (jrn[i + 2].Name.StartsWith('('))
                                {
                                    d.Comment = jrn[i + 2].Name;
                                    i++;
                                }

                                detailed.Add(d);
                                i++;
                            }
                            else
                            {
                                detailed.Add(new ETxtDetailed
                                {
                                    SuccessQuery = true,
                                    Searcher = jrn[i + 1].Name.Trim(),
                                    Matches = double.TryParse(jrn[i + 2].Name.Split(' ', '%')[1], out var cons)
                                        ? cons
                                        : 0,
                                    Url = jrn[i + 4].Name
                                });
                                i += 4;
                            }
                        }
                        else if (isEnd)
                        {
                            if (jrn[i].Name.Contains("Обнаружено ошибок"))
                            {
                                double.TryParse(jrn[i].Name.Split(' ', '%')[2], out error);
                            }

                            if (jrn[i].Name.Contains("Уникальность текста"))
                            {
                                double.TryParse(jrn[i].Name.Split(' ', '%')[2], out unique);
                            }
                        }
                    }

                    _logger.LogInformation(
                        $"Process complited: {processed}%. Unique: {unique}%. Errors: {error}%");
                    return new ETxtResult
                    {
                        Errors = error,
                        Processed = processed,
                        UniquePhrases = unique,
                        Detailed = detailed
                    };
                }
                else
                {
                    var infoEls = window.FindFirstChild(x => x.ByAutomationId("totalProcessProgressBar"))
                        .AsSlider();
                    processed = infoEls.Value;
                    _logger.LogInformation($"Processed {processed}%");
                    await Transport.SendAsync(MessageType.TaskStat.ToString(), new AgentMessage<TaskMessage>
                    {
                        Author = this,
                        MessageType = MessageType.TaskStat,
                        Data = new TaskMessage
                        {
                            ParentId = parentId,
                            Id = taskId,
                            State = TaskState.Active,
                            ProcessPercentage = processed
                        }
                    });
                }

                await Task.Delay(5000);
            }

            return null;
        }


        public override async Task ProcessMessageAsync(AgentMessage message)
        {
            var tMessage = message.To<TaskMessage>();
            var path = $"{tMessage.Data.Id.ToString()}.txt";
            if (!File.Exists(path))
            {
                await using var bw = new BinaryWriter (File.Open(path, FileMode.Create), Encoding.UTF8);
                bw.Write(tMessage.Data.Data);
            }
            var (ok, automation, window, app) = await StartAndCheckForUpdateAsync();
            await OpenFileAsync(window, Path.Combine(Directory.GetCurrentDirectory(), path));

            var result = await MakeCheckAsync(window, false, tMessage.Data.Id, tMessage.Data.ParentId);
            app.Close();
            await Transport.SendAsync(MessageType.TaskStat.ToString(), new AgentMessage<TaskMessage>
            {
                Author = this,
                MessageType = MessageType.TaskStat,
                Data = new TaskMessage
                {
                    ParentId = tMessage.Data.ParentId,
                    Id = tMessage.Data.Id,
                    State = TaskState.Finished,
                    ProcessPercentage = 100,
                    UniquePercentage = result.UniquePhrases,
                    ErrorPercentage = result.Errors,
                    Report = JsonSerializer.Serialize(result)
                }
            });
        }

        public override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            return null;
        }
    }
}