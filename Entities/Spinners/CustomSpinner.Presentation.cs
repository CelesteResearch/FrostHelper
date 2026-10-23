using System;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Capabilities;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Entities.Spinners
{
    public partial class CustomSpinner : ISourceSpinnerPresentation
    {
        ISourceEngineContext ISourceSpinnerPresentation.EngineContext => engine;

        bool ISourceSpinnerPresentation.Rainbow
        {
            get => Rainbow;
            set => Rainbow = value;
        }

        PackedColor ISourceSpinnerPresentation.Tint
        {
            get => Tint;
            set => Tint = value;
        }

        void ISourceSpinnerPresentation.BuildGraphicsWithSource(string directory, Action<SceneEntityState> build)
        {
            var originalSource = SpriteSource;
            SpriteSource = services.GetSprites(directory, "", false);
            try
            {
                build(this);
            }
            finally
            {
                SpriteSource = originalSource;
            }
        }

        void ISourceSpinnerPresentation.RebuildGraphics(string directory)
        {
            var originalSource = SpriteSource;
            if (directory != null) SpriteSource = services.GetSprites(directory, "", false);
            try
            {
                sprites.Clear();
                fills.Clear();
                expanded = false;
                CreateSprites();
            }
            finally
            {
                SpriteSource = originalSource;
            }
        }
    }
}
