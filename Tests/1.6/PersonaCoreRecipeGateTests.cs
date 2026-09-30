using System.Xml;
using Verse;
using Xunit;

namespace PersonaWeaponsUnbound.Tests
{
    // Unit tests for the XML patch gate behind the optional persona-core
    // recipe. The gate's own logic is the only thing under test: the nested
    // operation is a recording stub, so no XPath, DirectXmlToObject, or live
    // def database is involved. PWU_Mod.Settings has an internal setter for
    // exactly this (see Source/1.6/Properties/AssemblyInfo.cs).
    public class PersonaCoreRecipeGateTests
    {
        private sealed class RecordingOperation : PatchOperation
        {
            public int Calls;
            public bool Result = true;

            protected override bool ApplyWorker(XmlDocument xml)
            {
                Calls++;
                return Result;
            }
        }

        public PersonaCoreRecipeGateTests()
        {
            // PatchOperation.Apply brackets the worker with DeepProfiler
            // Start/End when enabled (the default), which wants a Unity
            // runtime. Off, Apply is pure.
            DeepProfiler.enabled = false;
        }

        private static (PatchOperation_UnlessPersonaCoreRecipeEnabled gate, RecordingOperation inner) MakeGate()
        {
            var inner = new RecordingOperation();
            return (new PatchOperation_UnlessPersonaCoreRecipeEnabled(inner), inner);
        }

        [Fact]
        public void RecipeOff_RunsNestedOperation()
        {
            PWU_Mod.Settings = new PWU_Settings { enablePersonaCoreRecipe = false };
            (var gate, var inner) = MakeGate();

            Assert.True(gate.Apply(new XmlDocument()));
            Assert.Equal(1, inner.Calls);
            Assert.False(PatchOperation_UnlessPersonaCoreRecipeEnabled.RecipeEnabledAtLoad);
        }

        [Fact]
        public void RecipeOn_SkipsNestedOperationAndSucceeds()
        {
            PWU_Mod.Settings = new PWU_Settings { enablePersonaCoreRecipe = true };
            (var gate, var inner) = MakeGate();

            Assert.True(gate.Apply(new XmlDocument()));
            Assert.Equal(0, inner.Calls);
            Assert.True(PatchOperation_UnlessPersonaCoreRecipeEnabled.RecipeEnabledAtLoad);
        }

        [Fact]
        public void RecipeOff_ReportsNestedFailure()
        {
            PWU_Mod.Settings = new PWU_Settings { enablePersonaCoreRecipe = false };
            (var gate, var inner) = MakeGate();
            inner.Result = false;

            Assert.False(gate.Apply(new XmlDocument()));
            Assert.Equal(1, inner.Calls);
        }

        [Fact]
        public void NullSettings_ReadAsDefaultOff()
        {
            Assert.False(PatchOperation_UnlessPersonaCoreRecipeEnabled.RecipeEnabled(null));
            Assert.False(PatchOperation_UnlessPersonaCoreRecipeEnabled.RecipeEnabled(new PWU_Settings()));
            Assert.True(PatchOperation_UnlessPersonaCoreRecipeEnabled.RecipeEnabled(
                new PWU_Settings { enablePersonaCoreRecipe = true }));
        }
    }
}
