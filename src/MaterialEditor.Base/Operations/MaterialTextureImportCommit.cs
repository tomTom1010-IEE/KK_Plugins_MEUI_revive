using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    /// <summary>Shared persistence/animation commit. DTO identity is retained for existing animation bindings.</summary>
    internal static class MaterialTextureImportCommit
    {
        internal static void Commit<T, TController, TAnimation>(IList<T> records, IDictionary<T, TController> animations,
            T existing, T candidate, Func<T, int?> getId, Action<T, int?> setId,
            Func<T, TAnimation> getAnimation, Action<T, TAnimation> setAnimation) where T : class
        {
            if (existing == null)
            {
                records.Add(candidate);
                return;
            }
            var previousId = getId(existing);
            var previousAnimation = getAnimation(existing);
            TController previousController;
            TController candidateController;
            var hadPrevious = animations.TryGetValue(existing, out previousController);
            var hasCandidate = animations.TryGetValue(candidate, out candidateController);
            try
            {
                setId(existing, getId(candidate));
                setAnimation(existing, getAnimation(candidate));
                animations.Remove(candidate);
                if (hasCandidate) animations[existing] = candidateController;
                else animations.Remove(existing);
            }
            catch
            {
                setId(existing, previousId);
                setAnimation(existing, previousAnimation);
                animations.Remove(candidate);
                if (hadPrevious) animations[existing] = previousController;
                else animations.Remove(existing);
                throw;
            }
        }
    }
}
