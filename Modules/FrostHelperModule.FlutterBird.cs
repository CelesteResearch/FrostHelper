// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using Celeste.Core.Display;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private readonly Dictionary<string, SpriteData> flutterBirdSprites = new();
        private readonly Dictionary<string, PackedColor[]> flutterBirdColors = new();

        internal PackedColor[] GetFlutterBirdColors(string colors)
        {
            if (!flutterBirdColors.TryGetValue(colors, out var parsed))
                flutterBirdColors.Add(colors, parsed = colors.Trim().Split(',').Select(Colors.Read).ToArray());
            return parsed;
        }

        internal Sprite CreateFlutterBirdSprite(string directory)
        {
            if (!flutterBirdSprites.TryGetValue(directory, out var data))
            {
                var bank = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)SourceEngine).GameGraphics)
                    .SpriteBank;
                var original = bank.SpriteData["flutterbird"];
                data = new SpriteData(SourceEngine, original.Atlas);
                foreach (var source in original.Sources) data.Add(source.XML, directory);
                flutterBirdSprites.Add(directory, data);
            }

            return data.Sprite.CreateClone();
        }

        private void UnloadFlutterBirdSprites()
        {
            flutterBirdSprites.Clear();
            flutterBirdColors.Clear();
        }
    }
}
