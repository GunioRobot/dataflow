/*
* Copyright © 2008 The Dataflow Team
*
* See AUTHORS and LICENSE for details.
*/

using System;
using NUnit.Framework;

namespace Dataflow.Core {
[TestFixture()]
public class EngineTest {
    public class NumberPatch : IPatch {
        public int value;
        public Outlet<int> res;

        public NumberPatch(int value) {
            this.value = value;
        }

        public void Init(IPatchContainer container) {
            res = container.AddOutlet<int>("value");
        }

        public void Execute() {
            res.Value = value;
        }
    }

    public class HolderPatch : IPatch {
        public bool inletChanged;
        public Inlet<int> input;

        public HolderPatch() {
        }

        public void Init(IPatchContainer container) {
            input = container.AddInlet<int>("inlet");
        }

        public void Execute() {
            this.inletChanged = input.HasChanged;
        }
    }

    public class NumberEveryOddTime : IPatch {
        int value;
        bool send = false;
        Outlet<int> res;

        public NumberEveryOddTime(int value) {
            this.value = value;
        }

        public void Init(IPatchContainer container) {
            res = container.AddOutlet<int>("value");
        }

        public void Execute() {
            if (send)
                res.Value = value;
            send = !send;
        }
    }

    public class InletModes : IPatch {
        public bool executed;
        public Inlet<int> passive, active, onchange;

        public InletModes() {
        }

        public void Init(IPatchContainer container) {
            passive = container.AddPassiveInlet<int>("passive");
            active = container.AddInlet<int>("active");
            onchange = container.AddActivateOnChangeInlet<int>("onchange");
        }

        public void Execute() {
            this.executed = true;
        }
    }

    [Test()]
    public void PassiveInletDontCauseExecution() {
        NumberPatch number = new NumberPatch(99);
        InletModes patch = new InletModes();

        Engine engine = new Engine();
        engine.Add(patch);
        engine.Add(number);
        engine.Connect(number, "value", patch, "passive");

        Assert.IsFalse(patch.executed, "#1");
        engine.StepFrame();
        Assert.IsFalse(patch.executed, "#2");
    }

    [Test()]
    public void ActiveInletCauseExecution() {
        NumberPatch number = new NumberPatch(99);
        InletModes patch = new InletModes();

        Engine engine = new Engine();
        engine.Add(patch);
        engine.Add(number);
        engine.Connect(number, "value", patch, "active");

        Assert.IsFalse(patch.executed, "#1");
        engine.StepFrame();
        Assert.IsTrue(patch.executed, "#2");
    }

    [Test()]
    public void ActiveOnChangeInletCauseExecutionOnlyIfValueChanges() {
        NumberPatch number = new NumberPatch(99);
        InletModes patch = new InletModes();

        Engine engine = new Engine();
        engine.Add(patch);
        engine.Add(number);
        engine.Connect(number, "value", patch, "onchange");

        Assert.IsFalse(patch.executed, "#1");
        engine.StepFrame();
        Assert.IsTrue(patch.executed, "#2");
        patch.executed = false;

        engine.StepFrame();
        Assert.IsFalse(patch.executed, "#3");

        number.value = 11;
        engine.StepFrame();
        Assert.IsTrue(patch.executed, "#4");

    }

    [Test()]
    public void ExecutedPatchSetOutlet() {
        NumberPatch patch = new NumberPatch(99);

        Engine engine = new Engine();
        engine.Add(patch);

        Assert.IsFalse(patch.res.HasChanged, "#1");
        engine.StepFrame();
        Assert.IsTrue(patch.res.HasChanged, "#2");
        Assert.AreEqual(99, patch.res.Value, "#3");
    }

    [Test()]
    public void ValuePropagateBetweenConnectedPatches() {
        NumberPatch num = new NumberPatch(99);
        HolderPatch hold = new HolderPatch();
        Engine engine = new Engine();

        engine.Add(num);
        engine.Add(hold);

        engine.Connect(num, "value", hold, "inlet");

        Assert.IsFalse(hold.input.HasChanged, "#1");
        Assert.IsFalse(hold.inletChanged, "#2");
        engine.StepFrame();

        Assert.IsTrue(hold.inletChanged, "#3");
        Assert.AreEqual(99, hold.input.Value, "#4");
    }

    [Test()]
    public void InletIsOnlyChangedIfOutletChanged() {
        NumberEveryOddTime num = new NumberEveryOddTime(99);
        HolderPatch hold = new HolderPatch();
        Engine engine = new Engine();

        engine.Add(num);
        engine.Add(hold);

        engine.Connect(num, "value", hold, "inlet");

        Assert.IsFalse(hold.input.HasChanged, "#1");

        //first execution should not set inlet
        engine.StepFrame();
        Assert.IsFalse(hold.input.HasChanged, "#2");

        //second execution should set inlet
        engine.StepFrame();
        Assert.IsTrue(hold.inletChanged, "#3");
        Assert.AreEqual(99, hold.input.Value, "#4");

        //last execution should not set inlet
        engine.StepFrame();
        Assert.IsFalse(hold.input.HasChanged, "#5");
    }

    [Test()]
    public void OutletConnectedToMultiplePatchesPropagateAllValues() {
        NumberPatch num = new NumberPatch(99);
        HolderPatch hold0 = new HolderPatch();
        HolderPatch hold1 = new HolderPatch();
        Engine engine = new Engine();


        engine.Add(num);
        engine.Add(hold0);
        engine.Add(hold1);
        engine.Connect(num, "value", hold0, "inlet");
        engine.Connect(num, "value", hold1, "inlet");

        Assert.IsFalse(hold0.input.HasChanged, "#1");
        Assert.IsFalse(hold0.inletChanged, "#2");
        Assert.IsFalse(hold1.input.HasChanged, "#3");
        Assert.IsFalse(hold1.inletChanged, "#4");

        engine.StepFrame();

        Assert.IsTrue(hold0.inletChanged, "#5");
        Assert.AreEqual(99, hold0.input.Value, "#6");
        Assert.IsTrue(hold1.inletChanged, "#7");
        Assert.AreEqual(99, hold1.input.Value, "#8");
    }
}
}
