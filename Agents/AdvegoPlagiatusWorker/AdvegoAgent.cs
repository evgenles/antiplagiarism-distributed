using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Logging;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using TextCopy;
using Transport.Abstraction;

namespace AdvegoPlagiatusWorker
{
    public class AdvegoAgent : AgentAbstract
    {
        private readonly ILogger<AdvegoAgent> _logger;
        public string AdvegoPath = "C:\\Program Files\\Advego Plagiatus 3";

        public AdvegoAgent(ITransportSender transport, ILogger<AdvegoAgent> logger) :
            base(transport, AgentType.Worker, "Advego", MessageType.Unknown, MessageType.WorkerTask)
        {
            _logger = logger;
        }

        private async Task<bool> StartAndCheckForUpdate()
        {
            var launchPath = Path.Combine(AdvegoPath, "launch.exe");
            if (!File.Exists(launchPath)) return false;
            var app = FlaUI.Core.Application.Launch(launchPath);
            using var automation = new UIA3Automation();
            app.WaitWhileBusy();
            //   var windows = app.GetAllTopLevelWindows(automation);
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(1));
            var x = window?.FindAllChildren();
            while (x == null || x.Length == 0)
            {
                await Task.Delay(5000);
                try
                {
                    window = FlaUI.Core.Application.Attach(app.ProcessId)
                        .GetMainWindow(automation, TimeSpan.FromSeconds(1));
                    if (window != null)
                    {
                        Console.WriteLine(window.Title);
                        x = window.FindAllChildren();
                    }
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            var later = window?.FindFirstDescendant(btn => btn.ByName("Напомнить позже Enter")).AsButton();
            later?.Invoke();
            await Task.Delay(10000);
            return true;
        }

        private (UIA3Automation automation, Window window) AttachToExecutable()
        {
            var executable = Path.Combine(AdvegoPath, "Plagiatus.exe");
            var app2 = FlaUI.Core.Application.Attach(executable, 0);
            var automation = new UIA3Automation();
            var y = app2.GetAllTopLevelWindows(automation);
            if (y.Length == 0)
            {
                app2 = FlaUI.Core.Application.Attach(executable, 1);
                y = app2.GetAllTopLevelWindows(automation);
            }

            var window = app2.GetMainWindow(automation);
            Console.WriteLine(window.Title);
            return (automation, window);
        }

        private async Task OpenFile(AutomationElement window, string filePath)
        {
            var loadFromFile = window.FindFirstDescendant(x => x.ByName("Загрузить текст из документа"))
                .Parent.FindFirstDescendant(x => x.ByControlType(FlaUI.Core.Definitions.ControlType.Button)).AsButton();
            loadFromFile.Invoke();
            await Task.Delay(2000);
            var edit = window.FindAllDescendants(x => x.ByControlType(FlaUI.Core.Definitions.ControlType.Edit))
                .First(x => x.Name.Contains("Имя файла")).AsTextBox();
            edit.Text = filePath;

            var openButton = edit.Parent
                .FindAllDescendants(x => x.ByControlType(FlaUI.Core.Definitions.ControlType.Button))
                .First(x => x.Name.Contains("Открыть")).AsButton();
            openButton.Invoke();
            await Task.Delay(2000);
        }

        private async Task<AdvegoResult> MakeCheck(AutomationElement window, bool isFullCheck)
        {
            var checkButton = window
                .FindFirstDescendant(x => x.ByName(isFullCheck ? "Полная проверка" : "Быстрая проверка")).AsButton();
            checkButton.Invoke();

            AdvegoResult result = new AdvegoResult();

            while (Math.Abs(result.Processed - 100) > 0.001)
            {
                var compited = window.FindFirstDescendant(x => x.ByName("Проверка завершена:"))?.Parent
                    ?.FindAllDescendants();
                if (compited != null)
                {
                    result.Processed = 100;
                    for (int i = compited.Length - 1; i >= 0; i--)
                    {
                        switch (compited[i].Name)
                        {
                            case "Уникальность по словам":
                                result.UniqueWords = double.Parse(compited[i - 1].Name.Trim(' ', '%'));
                                break;
                            case "Уникальность по фразам":
                                result.UniquePhrases = double.Parse(compited[i - 1].Name.Trim(' ', '%'));
                                break;
                            case "Проверено документов:":
                                result.DocumentChecked = double.Parse(compited[i + 1].Name.Trim());
                                break;
                            case "Всего найдено: ":
                                result.SimilarDocument = double.Parse(compited[i + 1].Name.Trim());
                                break;
                            case "Ошибок:":
                                result.Errors = double.Parse(compited[i + 1].Name.Trim());
                                break;
                        }
                    }

                    _logger.LogInformation(
                        $"Process complited: {result.Processed}%. Unique words: {result.UniqueWords}%. UniuePhrases: {result.UniquePhrases}%");
                   _logger.LogInformation(
                        $"Documents checked: {result.DocumentChecked}. Found {result.SimilarDocument}. Can`t open: {result.Errors}");
                    return result;
                }
                else
                {
                    var infoEls = window.FindFirstDescendant(x => x.ByName("Идёт проверка:"))?.Parent
                        ?.FindAllChildren();
                    if (infoEls != null)
                    {
                        for (int i = 0; i < infoEls.Length; i++)
                        {
                            if (infoEls[i].Name == "Идёт проверка:")
                                result.Processed = double.Parse(infoEls[i + 1].Name.Trim(' ', '%'));
                            else if (infoEls[i].Name == "по фразам")
                                result.UniquePhrases = double.Parse(infoEls[i + 1].Name.Trim(' ', '%'));
                            else if (infoEls[i].Name == "по словам")
                                result.UniqueWords = double.Parse(infoEls[i + 1].Name.Trim(' ', '%'));
                        }

                        _logger.LogInformation(
                            $"Processed: {result.Processed}%. Unique words: {result.UniqueWords}%. UniuePhrases: {result.UniquePhrases}%");
                    }

                    await Task.Delay(5000);
                }
            }

            return null;
        }

        private async Task<List<MatchAdvego>> GetDetailedResult(AutomationElement window, UIA3Automation automation)
        {
            try
            {
                window.FindFirstDescendant(x => x.ByName("Посмотреть результаты проверки Enter")).AsButton().Invoke();
                var bar = window.FindFirstDescendant(x => x.ByControlType(FlaUI.Core.Definitions.ControlType.ToolBar));
                var resultMenu =
                    bar.FindAllDescendants(x => x.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)).Last()
                        .AsMenuItem();
                window.Focus();
                window.FocusNative();
                window.SetForeground();
                resultMenu.Focus();
                resultMenu.SetForeground();
                await Task.Delay(1000);
                resultMenu.Click();
                automation.GetDesktop().FindFirstDescendant(x => x.ByName("Копировать результат проверки Ctrl+Shift+3"))
                    .AsMenuItem().Invoke();
                var text = await ClipboardService.GetTextAsync();
                if (text != null)
                {
                    var lines = text.Split(Environment.NewLine);
                    bool docs = false;
                    List<MatchAdvego> detailed = new List<MatchAdvego>();
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("##########")) docs = true;
                        else if (docs)
                        {
                            var splitted = line.Split('|', '-');
                            detailed.Add(new MatchAdvego
                            {
                                Matches = double.Parse(splitted[0]),
                                Rerite = double.Parse(splitted[1].Trim()),
                                Url = splitted[2].Trim()
                            });
                        }
                    }

                    _logger.LogInformation("{DetailedAdvego}", detailed);

                    return detailed;
                }
            }
            catch (Exception e)
            {
               _logger.LogError(e, "Can`t get detailed info");
            }

            return null;
        }

        public override Task ProcessMessageAsync(AgentMessage message)
        {
            throw new NotImplementedException();
        }

        public override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            return null;
        }
    }
}