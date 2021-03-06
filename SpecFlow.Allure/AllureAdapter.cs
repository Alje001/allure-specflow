﻿using AllureCSharpCommons;
using AllureCSharpCommons.AllureModel;
using AllureCSharpCommons.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using TechTalk.SpecFlow;

namespace SpecFlow.Allure
{
    internal class AllureAdapter
    {
        static readonly Lazy<AllureAdapter> lazy = new Lazy<AllureAdapter>(() => new AllureAdapter());
        static readonly object lockObj = new object();
        AllureCSharpCommons.Allure lifecycle = AllureCSharpCommons.Allure.Lifecycle;

        public static AllureAdapter Instance => lazy.Value;

        AllureAdapter()
        {
            lock (lockObj)
            {
                string path = Config.ResultsPath;
                if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    path += Path.DirectorySeparatorChar;
                }
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                if (!directoryInfo.Exists)
                    Directory.CreateDirectory(directoryInfo.FullName);

                AllureConfig.AllowEmptySuites = true;
                AllureConfig.ResultsPath = path;
            }
        }

        public void StartSuite(FeatureInfo featureInfo)
        {
            if (featureInfo != null)
            {
                var uid = GetTestSuiteIdForCurrentThread(featureInfo);
                var suiteStarted = new TestSuiteStartedEvent(uid, uid);
                suiteStarted.Title = featureInfo.Title;
                suiteStarted.Description = new description { type = descriptiontype.text, Value = featureInfo.Description };
                suiteStarted.Labels = GetFeatureLabels(featureInfo);
                lifecycle.Fire(suiteStarted);
            }
        }

        public void StartTestCase(FeatureInfo featureInfo, ScenarioInfo scenarioInfo)
        {
            var featureUid = GetTestSuiteIdForCurrentThread(featureInfo);

            TestCaseStartedEvent testCaseStarted = new TestCaseStartedEvent(featureUid, scenarioInfo.GetHashCode().ToString(), DateTime.Now)
            {
                Title = scenarioInfo.Title,
                Labels = GetScenarioLabels(featureInfo, scenarioInfo)
            };
            lifecycle.Fire(testCaseStarted);

        }

        public void StartStep(string title)
        {
            if (title != null)
            {
                var stepEvent = new StepStartedEvent(title.GetHashCode().ToString());
                stepEvent.Title = title;
                lifecycle.Fire(stepEvent);
            }
        }

        public void FailStep()
        {
            lifecycle.Fire(new StepFailureEvent());
            FinishStep();
        }

        public void FinishStep()
        {
            lifecycle.Fire(new StepFinishedEvent());
        }

        public void CancelStep()
        {
            lifecycle.Fire(new StepCanceledEvent());
            FinishStep();
        }

        public void PendingStep()
        {
            lifecycle.Fire(new StepPendingEvent());
            FinishStep();

        }
        public void CancelTestCase(Exception exception)
        {
            TestCaseCanceledEvent testCaseCanceled = new TestCaseCanceledEvent()
            {
                Throwable = exception,
                StackTrace = exception.StackTrace
            };
            lifecycle.Fire(testCaseCanceled);
            lifecycle.Fire(new TestCaseFinishedEvent());
        }
        public void FinishTestCase(ScenarioContext scenarioContext, Exception exception = null)
        {
            AddAttachments(scenarioContext);

            var error = exception ?? scenarioContext.TestError;
            if (error != null)
            {
                TestCaseFailureEvent failure = new TestCaseFailureEvent()
                {
                    Throwable = error,
                    StackTrace = error.StackTrace
                };
                lifecycle.Fire(failure);
            }
            lifecycle.Fire(new TestCaseFinishedEvent());
        }

        public void PendingTestCase(Exception ex)
        {
            lifecycle.Fire(new TestCasePendingEvent() { Throwable = ex });
            lifecycle.Fire(new TestCaseFinishedEvent());
        }
        public void FailTestSuite(FeatureInfo featureInfo, ScenarioInfo scenarioInfo, Exception exception)
        {
            StartTestCase(featureInfo, scenarioInfo);
            CancelTestCase(exception);
            FinishSuite(featureInfo);
        }

        public void FinishSuite(FeatureInfo featureInfo)
        {
            lifecycle.Fire(new TestSuiteFinishedEvent(GetTestSuiteIdForCurrentThread(featureInfo)));
        }

        private label[] GetFeatureLabels(FeatureInfo featureInfo)
        {
            var labels = featureInfo.Tags.Select(x => new label("feature", x)).ToList();

            if (featureInfo != null && !string.IsNullOrWhiteSpace(featureInfo.Title))
                labels.Add(new label("story", featureInfo.Title));

            return labels.ToArray();
        }

        private label[] GetScenarioLabels(FeatureInfo featureInfo, ScenarioInfo scenarioInfo)
        {
            var labels = scenarioInfo.Tags.Select(x => new label("feature", x)).ToList();
            labels.AddRange(featureInfo.Tags.Select(x => new label("feature", x)).ToList());
            labels.Add(new label("thread", Thread.CurrentThread.ManagedThreadId.ToString()));
            return labels.ToArray();
        }

        private string GetTestSuiteIdForCurrentThread(FeatureInfo featureInfo)
        {
            return Math.Abs(featureInfo.Title.GetHashCode()).ToString();
        }

        private void AddAttachments(ScenarioContext scenarioContext)
        {
            foreach (var key in scenarioContext.Keys.Where(x => x.StartsWith(Config.AttachmentKey)))
            {
                var contextValue = scenarioContext[key] as Tuple<string, string>;
                var title = contextValue.Item1;
                var filePath = contextValue.Item2;
                MakeAttachmentEvent makeAttach = new MakeAttachmentEvent(filePath, title);
                lifecycle.Fire(makeAttach);
            }
        }

    }
}
