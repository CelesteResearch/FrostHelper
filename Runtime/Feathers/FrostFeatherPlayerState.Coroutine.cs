using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Presentation.ScreenEffects;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    public sealed partial class FrostFeatherPlayerState
    {
        private IEnumerator Coroutine()
        {
            var feather = CurrentFeather;
            if (feather == null) yield break;
            while (player.Sprite.CurrentAnimationID == "startStarFly") yield return null;
            while (player.Speed != Vector2.Zero) yield return null;
            yield return .1f;
            player.Sprite.Color = feather.FlyColor;
            player.Sprite.HairCount = 7;
            player.Hair.DrawPlayerSpriteOutline = true;
            FrostFeatherAccess.Displace(player);
            FrostFeatherAccess.SetTransforming(player, false);
            player.Data.StarFlyTimer = feather.FlyTime;
            if (feather.RefillDashes) player.World.RefillDash();
            if (feather.RefillStamina) player.RefillStamina();
            var direction = feather.GetAim();
            if (direction == Vector2.Zero) direction = Vector2.UnitX * player.Data.Facing;
            player.Speed = direction * 250;
            FrostFeatherAccess.SetLastDirection(player, direction);
            ((OriginalLevelSceneRoot)player.Scene).Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4,
                feather.FlyColor, Vector2Geometry.Angle(-direction));
            FrostFeatherAccess.Rumble(player, 2, 1, "StrongMedium", player.Body.Position);
            ((ISourceLevelShakeScene)player.Scene).DirectionalShake(player.StarFlyLastDirection, .3f);
            while (player.Data.StarFlyTimer > .5f)
            {
                player.Hair.Color = player.Sprite.Color = feather.FlyColor;
                yield return null;
            }

            player.StarFlyWarningSound.Play("event:/game/06_reflection/feather_state_warning");
        }
    }
}
