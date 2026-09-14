using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>Surfaces the tests in this project share.</summary>
internal static class A2UISurfaceFixtures
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    internal static A2UISurface Survey(string id = "survey_1")
    {
        var s = A2UISurface.Create(id, Basic);
        var ui = s.Components;
        return s.Root(ui.Card(ui.Column(
            ui.Text("How did we do?").Variant(TextVariant.H3),
            ui.Button("Submit").Primary().OnClick(Act.Event("submit"))))).Build();
    }
}
