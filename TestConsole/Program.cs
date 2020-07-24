﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        private const string ETxtPath = "C:\\Program Files (x86)\\Etxt Antiplagiat";

        private async Task<(bool isLaunched, UIA3Automation automation, AutomationElement window, Application app)>
            StartAndCheckForUpdateAsync()
        {
            var launchPath = Path.Combine(ETxtPath, "EtxtAntiplagiat.exe");
            if (!File.Exists(launchPath)) return (false, null, null, null);
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
            return (true, automation, window, app);
        }

        private async Task OpenFileAsync(AutomationElement window, string filePath)
        {
            var fileMenu = window
                .FindFirstDescendant(x => x.ByAutomationId("menuBar"))
                .FindFirstChild(x => x.ByAutomationId("fileToolStripMenuItem"))
                .AsMenuItem();
            fileMenu.Expand();
            var dec = fileMenu.FindFirstChild(x => x.ByAutomationId("openFileToolStripMenuItem")).AsMenuItem();
            dec.Invoke();

            await Task.Delay(500);
            var openWindow = window
                .FindFirstChild(x => x.ByControlType(ControlType.Window) /*.And(x.ByName("Открыть файл"))*/);
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

        private async Task MakeCheckAsync(AutomationElement window, bool isFullCheck, Guid taskId,
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
                    var detailed = new List<AntiplagiariusDetailed>();
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
                                var d = new AntiplagiariusDetailed
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
                                detailed.Add(new AntiplagiariusDetailed
                                {
                                    SuccessQuery = true,
                                    Searcher = jrn[i + 1].Name.Trim(),
                                    Сoincidences = double.TryParse(jrn[i + 2].Name.Split(' ', '%')[1], out var cons)
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
                }
                //     _logger.LogInformation(
                //         $"Process complited: {result.Processed}%. Unique words: {result.UniqueWords}%. UniuePhrases: {result.UniquePhrases}%");
                //     _logger.LogInformation(
                //         $"Documents checked: {result.DocumentChecked}. Found {result.SimilarDocument}. Can`t open: {result.Errors}");
                //     return result;
                // }
                else
                {
                    var infoEls = window.FindFirstChild(x => x.ByAutomationId("totalProcessProgressBar"))
                        .AsSlider();
                    processed = infoEls.Value;

                    //     _logger.LogInformation(
                    //         $"Processed: {result.Processed}%. Unique words: {result.UniqueWords}%. UniuePhrases: {result.UniquePhrases}%");
                    //     await Transport.SendAsync(MessageType.TaskStat.ToString(), new AgentMessage<TaskMessage>
                    //     {
                    //         Author = this,
                    //         MessageType = MessageType.TaskStat,
                    //         Data = new TaskMessage
                    //         {
                    //             ParentId = parentId,
                    //             Id = taskId,
                    //             State = TaskState.Active,
                    //             ProcessPercentage = result.Processed
                    //         }
                    //     });
                    // }
                    Console.WriteLine($"Processed {processed}%");
                    await Task.Delay(5000);
                }
            }

            return;
        }

        static async Task Main(string[] args)
        {
            try
            {
                var result = new Program();
                var (ok, automation, window, app) = await result.StartAndCheckForUpdateAsync();
                await result.OpenFileAsync(window,
                    "D:\\antiplagiarius-distributed\\AntiplagiariusDistributed\\TestConsole\\test.txt");

                await result.MakeCheckAsync(window, false, Guid.Empty, Guid.Empty);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}