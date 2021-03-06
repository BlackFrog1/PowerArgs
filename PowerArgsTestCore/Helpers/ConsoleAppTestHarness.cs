﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Runtime.CompilerServices;

namespace ArgsTests.CLI.Physics
{
    class ConsoleAppTestHarness
    {
        public static void Run(TestContext context, Action<ConsoleApp> testCode, [CallerMemberName]string testName = null, int w = 80, int h = 30)
        {
            ConsoleApp app = new ConsoleApp(w, h);
            app.Recorder = TestRecorder.CreateTestRecorder(testName, context);
            app.QueueAction(() => { testCode(app); });
            app.Start().Wait();
        }

        public static void Run(TestContext context, Action<ConsoleApp, SpacetimePanel> testCode, [CallerMemberName]string testName = null, int w = 80, int h = 30)
        {
            Promise spaceTimePromise = null;
            Run(context, (app) =>
            {
                var panel = app.LayoutRoot.Add(new SpacetimePanel(w, h));
                panel.SpaceTime.QueueAction("Test",() =>
                {
                    testCode(app, panel);
                });
                spaceTimePromise = panel.SpaceTime.Start();
                spaceTimePromise.Finally((p) => app.Stop());

            }, testName, w, h);

            Assert.IsNotNull(spaceTimePromise);
            spaceTimePromise.Wait();
        }
    }
}
