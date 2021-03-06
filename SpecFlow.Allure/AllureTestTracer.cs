﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.BindingSkeletons;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlow.Allure
{
    public class AllureTestTracer : TestTracer, ITestTracer
    {
        public AllureTestTracer(ITraceListener traceListener, IStepFormatter stepFormatter, IStepDefinitionSkeletonProvider stepDefinitionSkeletonProvider, RuntimeConfiguration runtimeConfiguration) : base(traceListener, stepFormatter, stepDefinitionSkeletonProvider, runtimeConfiguration)
        {
        }

        void ITestTracer.TraceStep(StepInstance stepInstance, bool showAdditionalArguments)
        {
            base.TraceStep(stepInstance, showAdditionalArguments);

            var title = $"{stepInstance.Keyword} {stepInstance.Text}";
            AllureAdapter.Instance.StartStep(title);
        }

        void ITestTracer.TraceStepDone(BindingMatch match, object[] arguments, TimeSpan duration)
        {
            base.TraceStepDone(match, arguments, duration);
            AllureAdapter.Instance.FinishStep();
        }

        void ITestTracer.TraceError(Exception ex)
        {
            base.TraceError(ex);
            AllureAdapter.Instance.FailStep();

        }
        void ITestTracer.TraceStepSkipped()
        {
            base.TraceStepSkipped();
            AllureAdapter.Instance.CancelStep();
        }

        void ITestTracer.TraceStepPending(BindingMatch match, object[] arguments)
        {
            base.TraceStepPending(match, arguments);
            AllureAdapter.Instance.PendingStep();

        }

        void ITestTracer.TraceNoMatchingStepDefinition(StepInstance stepInstance, TechTalk.SpecFlow.ProgrammingLanguage targetLanguage, System.Globalization.CultureInfo bindingCulture, List<BindingMatch> matchesWithoutScopeCheck)
        {
            base.TraceNoMatchingStepDefinition(stepInstance, targetLanguage, bindingCulture, matchesWithoutScopeCheck);
            AllureAdapter.Instance.FailStep();
        }
    }
}
