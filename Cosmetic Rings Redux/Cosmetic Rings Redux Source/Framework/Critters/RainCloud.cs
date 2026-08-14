using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Critters
{
    internal class RainCloud : Critter
    {
        private readonly bool isFlipped;
        private float scale = 1f;
        private float elapsedTime;
        private readonly int raindropSpeed = 50;

        public RainCloud(Vector2 position, int which, float rotationVelocity, float dx, float dy)
        {
            this.position = position * 64f + new Vector2(32f, -96f);
            isFlipped = Game1.random.Next(0, 2) == 1;
            baseFrame = 0;
            sprite = new AnimatedSprite(ResourceManager.RaindropsTexturePath, baseFrame, 16, 16)
            {
                loop = false
            };
            startingPosition = position;
        }

        private void doneWithRaindrop(Farmer who)
        {
        }

        public override bool update(GameTime time, GameLocation environment)
        {
            Vector2 targetPosition = Game1.player.Position + new Vector2(32f, -96f);
            position = Vector2.Lerp(position, targetPosition, 0.03f);
            scale = 0.15f * (6f + (float)Math.Sin(2 * Math.PI * elapsedTime));
            elapsedTime = (elapsedTime + (float)time.ElapsedGameTime.TotalMilliseconds / 3000f) % 1f;

            if (sprite.CurrentAnimation == null)
            {
                sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                {
                    new FarmerSprite.AnimationFrame(baseFrame + 1, raindropSpeed),
                    new FarmerSprite.AnimationFrame(baseFrame + 2, raindropSpeed),
                    new FarmerSprite.AnimationFrame(baseFrame + 3, raindropSpeed),
                    new FarmerSprite.AnimationFrame(baseFrame + 2, raindropSpeed),
                    new FarmerSprite.AnimationFrame(baseFrame + 1, raindropSpeed),
                    new FarmerSprite.AnimationFrame(baseFrame, raindropSpeed, secondaryArm: false, flip: false, doneWithRaindrop)
                });
            }

            return base.update(time, environment);
        }

        public override void draw(SpriteBatch b)
        {
        }

        public override void drawAboveFrontLayer(SpriteBatch b)
        {
            sprite.draw(
                b,
                Game1.GlobalToLocal(Game1.viewport, position + new Vector2(-16f, 12f)),
                position.Y / 10000f,
                0,
                0,
                Color.White,
                flip,
                2f
            );
            b.Draw(
                Game1.mouseCursors,
                Game1.GlobalToLocal(Game1.viewport, position),
                new Rectangle(648, 1045, 52, 33),
                Color.White,
                0f,
                new Vector2(26f, 16f),
                scale,
                isFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                1f
            );
        }
    }
}
