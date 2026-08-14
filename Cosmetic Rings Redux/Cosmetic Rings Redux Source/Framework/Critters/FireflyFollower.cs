using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Critters
{
    internal sealed class FireflyFollower : Critter
    {
        private readonly string lightId;
        private readonly LightSource light;
        private float elapsedTime;
        private Vector2 motion;
        private float motionMultiplier;
        private float spawnOffsetY;
        private float spawnOffsetX;

        public FireflyFollower(Vector2 position)
        {
            spawnOffsetX = 20f + Game1.random.Next(0, 2) * 64f;
            spawnOffsetY = 30f + Game1.random.Next(0, 5);

            baseFrame = -1;
            this.position = position * 64f;
            startingPosition = this.position;
            motion = new Vector2(Game1.random.Next(-10, 11) * 0.1f, Game1.random.Next(-10, 11) * 0.1f);
            motionMultiplier = 1f;
            lightId = $"ThaleTheGreat.CosmeticRingsRedux.Firefly.{Guid.NewGuid():N}";
            light = new LightSource(
                lightId,
                4,
                this.position,
                Game1.random.Next(1, 6) * 0.1f,
                Color.Purple * 0.8f,
                LightSource.LightContext.None,
                0L,
                null
            );
        }

        internal void AttachLight(GameLocation location)
        {
            if (location == null)
                return;

            location.removeLightSource(lightId);
            location.sharedLights[lightId] = light;
        }

        internal void DetachLight(GameLocation location)
        {
            location?.removeLightSource(lightId);
        }

        internal void ResetForNewLocation(Vector2 tilePosition, GameLocation location)
        {
            position = tilePosition * 64f;
            startingPosition = position;
            light.position.Value = position;
            AttachLight(location);
        }

        public override bool update(GameTime time, GameLocation environment)
        {
            light.radius.Value = 0.1f * (5f + (float)Math.Sin(2 * Math.PI * elapsedTime));
            elapsedTime = (elapsedTime + (float)time.ElapsedGameTime.TotalMilliseconds / 3000f) % 1f;

            if (Game1.player.isMoving())
            {
                spawnOffsetX = Game1.player.Position.X + spawnOffsetX < position.X ? 84f : 20f;
                motionMultiplier = 1f;
            }

            Vector2 targetPosition = Game1.player.Position + new Vector2(spawnOffsetX, spawnOffsetY);
            position = Vector2.Lerp(position, targetPosition, Vector2.Distance(targetPosition, position) >= 64f && Game1.player.isMoving() ? 0.05f : 0.02f);
            position += motion * motionMultiplier;

            motion.X = Math.Clamp(motion.X + Game1.random.Next(-1, 2) * 0.1f, -1f, 1f);
            motion.Y = Math.Clamp(motion.Y + Game1.random.Next(-1, 2) * 0.1f, -1f, 1f);
            light.position.Value = position;

            return false;
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(
                Game1.staminaRect,
                Game1.GlobalToLocal(position),
                Game1.staminaRect.Bounds,
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                position.Y / 10000f
            );
        }

        public override void drawAboveFrontLayer(SpriteBatch b)
        {
        }
    }
}
