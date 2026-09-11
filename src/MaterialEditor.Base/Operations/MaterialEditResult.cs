using System;

namespace MaterialEditorAPI
{
    internal enum MaterialEditStatus { Succeeded, Failed, Cancelled, Superseded, Unverified }

    /// <summary>Internal completion contract; legacy public void APIs remain unchanged.</summary>
    internal sealed class MaterialEditResult
    {
        internal readonly MaterialEditStatus Status;
        internal readonly string Stage;
        internal readonly string Diagnostic;
        internal bool Succeeded => Status == MaterialEditStatus.Succeeded;

        internal MaterialEditResult(MaterialEditStatus status, string stage = null, string diagnostic = null)
        {
            Status = status;
            Stage = stage;
            Diagnostic = diagnostic;
        }

        internal static MaterialEditResult FromApplied(bool applied) =>
            new MaterialEditResult(applied ? MaterialEditStatus.Succeeded : MaterialEditStatus.Failed, "Apply/commit");
    }
}
