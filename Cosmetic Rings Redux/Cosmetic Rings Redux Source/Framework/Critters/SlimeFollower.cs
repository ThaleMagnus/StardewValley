using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Critters
{
    internal class SlimeFollower : NPC
    {

        private int wagTimer;
        private int movementTimer;
        private readonly NetVector2 motion = new NetVector2(Vector2.Zero);
        private new readonly NetRectangle nextPosition = new NetRectangle();
        public readonly NetColor color = new NetColor();
        public readonly NetBool prismatic = new NetBool();
        public readonly NetInt specialNumber = new NetInt();
        private readonly NetVector2 facePosition = new NetVector2();
        private bool hasSpecialFlag;

        public SlimeFollower(Vector2 position) : base(new AnimatedSprite("Characters\\Monsters\\Green Slime", 0, 16, 16), position * 64f, 2, "SlimeFollower", null)
        {
            base.Breather = false;
            base.speed = ModEntry.Config.walkingSpeed;
            base.forceUpdateTimer = 9999;
            base.collidesWithOtherCharacters.Value = false;
            base.farmerPassesThrough = true;
            base.HideShadow = true;
            this.nextPosition.Value = this.GetBoundingBox();

            int green = Game1.random.Next(200, 256);

            switch (Game1.random.NextDouble())
            {
                case var chance when chance <= 0.1:
                    // Prismatic slime
                    this.prismatic.Value = true;
                    break;
                case var chance when chance <= 0.75:
                    // Pick random color
                    this.color.Value = Utility.GetPrismaticColor(348 + this.specialNumber.Value, 5f);
                    break;
                default:
                    // Default green slime
                    this.color.Value = new Color(green / Game1.random.Next(2, 10), Game1.random.Next(180, 256), (Game1.random.NextDouble() < 0.1) ? 255 : (255 - green));
                    break;
            }

            this.Gender = (Gender)Game1.random.Next(0, 2);
            this.specialNumber.Value = Game1.random.Next(100);

            this.Sprite.SpriteHeight = 24;
            this.Sprite.UpdateSourceRect();

            if (Game1.random.NextDouble() <= 0.25)
            {
                this.hasSpecialFlag = true;
            }
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.currentLocation = location;
            base.update(time, location);
            base.forceUpdateTimer = 99999;

            if (this.wagTimer > 0)
            {
                this.wagTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
            }
            else if (Game1.random.NextDouble() < 0.01 && this.wagTimer <= 0)
            {
                this.wagTimer = 992;
            }
            this.yOffset = Math.Max(this.yOffset - (int)Math.Abs(base.xVelocity + base.yVelocity) / 2, -64);
            if (this.yOffset < 0)
            {
                this.yOffset = Math.Min(0, this.yOffset + 4 + (int)((this.yOffset <= -64) ? ((float)(-this.yOffset) / 8f) : ((float)(-this.yOffset) / 16f)));
            }

            Farmer f = Utility.isThereAFarmerWithinDistance(base.Tile, 10, base.currentLocation);
            if (f != null)
            {
                movementTimer = !isMoving() && Vector2.Distance(base.Position, f.Position) > 256f ? movementTimer - time.ElapsedGameTime.Milliseconds : 1000;
                if (Vector2.Distance(base.Position, f.Position) > 640f || movementTimer <= 0)
                {
                    base.Position = f.Position;
                    this.movementTimer = 1000;
                }
                else if (Vector2.Distance(base.Position, f.Position) > 64f)
                {
                    this.setMoving(Utility.getVelocityTowardPlayer(new Point((int)base.Position.X, (int)base.Position.Y), base.speed, f));
                }
                else
                {
                    this.motion.Value = Vector2.Zero;
                }
            }

            if (!base.IsInvisible && base.controller == null)
            {
                this.nextPosition.Value = this.GetBoundingBox();
                this.nextPosition.X += (int)this.motion.X;
                if (!location.isCollidingPosition(this.nextPosition.Value, Game1.viewport, this))
                {
                    base.Position = new Vector2(base.Position.X + (int)this.motion.X, base.Position.Y);
                }
                this.nextPosition.X -= (int)this.motion.X;
                this.nextPosition.Y += (int)this.motion.Y;
                if (!location.isCollidingPosition(this.nextPosition.Value, Game1.viewport, this))
                {
                    base.Position = new Vector2(base.Position.X, base.Position.Y + (int)this.motion.Y);
                }
            }
            if (base.controller != null || this.motion.Value != Vector2.Zero)
            {
                if (base.moveRight || (Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X > 0f))
                {
                    this.FacingDirection = 1;
                }
                else if (base.moveLeft || (Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X < 0f))
                {
                    this.FacingDirection = 3;
                }
                else if (base.moveUp || (Math.Abs(this.motion.Y) > Math.Abs(this.motion.X) && this.motion.Y < 0f))
                {
                    this.FacingDirection = 0;
                }
                else
                {
                    this.FacingDirection = 2;
                }

                this.Sprite.Animate(time, 8, 4, 50f);
            }
            else
            {
                switch (this.FacingDirection)
                {
                    case 0:
                        this.Sprite.Animate(time, 8, 4, 200f);
                        break;
                    case 1:
                        this.Sprite.Animate(time, 8, 4, 200f);
                        break;
                    case 2:
                        this.Sprite.Animate(time, 8, 4, 200f);
                        break;
                    case 3:
                        this.Sprite.Animate(time, 8, 4, 200f);
                        break;
                }
            }

            switch (this.FacingDirection)
            {
                case 2:
                    if (this.facePosition.X > 0f)
                    {
                        this.facePosition.X -= 2f;
                    }
                    else if (this.facePosition.X < 0f)
                    {
                        this.facePosition.X += 2f;
                    }
                    if (this.facePosition.Y < 0f)
                    {
                        this.facePosition.Y += 2f;
                    }
                    break;
                case 1:
                    if (this.facePosition.X < 8f)
                    {
                        this.facePosition.X += 2f;
                    }
                    if (this.facePosition.Y < 0f)
                    {
                        this.facePosition.Y += 2f;
                    }
                    break;
                case 3:
                    if (this.facePosition.X > -8f)
                    {
                        this.facePosition.X -= 2f;
                    }
                    if (this.facePosition.Y < 0f)
                    {
                        this.facePosition.Y += 2f;
                    }
                    break;
                case 0:
                    if (this.facePosition.X > 0f)
                    {
                        this.facePosition.X -= 2f;
                    }
                    else if (this.facePosition.X < 0f)
                    {
                        this.facePosition.X += 2f;
                    }
                    if (this.facePosition.Y > -8f)
                    {
                        this.facePosition.Y -= 2f;
                    }
                    break;
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(this.Sprite.Texture, base.getLocalPosition(Game1.viewport) + new Vector2(32f, this.GetBoundingBox().Height / 2 + this.yOffset), this.Sprite.SourceRect, this.prismatic.Value ? Utility.GetPrismaticColor(348 + this.specialNumber.Value, 5f) : this.color.Value, 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, base.Scale - 0.4f * ((float)(int)0 / 120000f)), SpriteEffects.None, Math.Max(0f, base.drawOnTop ? 0.991f : ((float)(base.StandingPixel.Y + 0 * 2) / 10000f)));
            b.Draw(Game1.shadowTexture, base.getLocalPosition(Game1.viewport) + new Vector2(32f, (float)(this.GetBoundingBox().Height / 2 * 7) / 4f + (float)this.yOffset + 8f * base.Scale - (float)(((int)0 > 0) ? 8 : 0)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f + base.Scale - (float)(int)0 / 120000f - ((this.Sprite.currentFrame % 4 % 3 != 0 || 0 != 0) ? 1f : 0f) + (float)this.yOffset / 30f, SpriteEffects.None, (float)(base.StandingPixel.Y - 1 + 0 * 2) / 10000f);

            if ((int)this.Gender == 0)
            {
                int xDongleSource = ((this.isMoving() || this.wagTimer > 0) ? (16 * Math.Min(7, Math.Abs(((this.wagTimer > 0) ? (992 - this.wagTimer) : (Game1.currentGameTime.TotalGameTime.Milliseconds % 992)) - 496) / 62) % 64) : 48);
                int yDongleSource = ((this.isMoving() || this.wagTimer > 0) ? (24 * Math.Min(1, Math.Max(1, Math.Abs(((this.wagTimer > 0) ? (992 - this.wagTimer) : (Game1.currentGameTime.TotalGameTime.Milliseconds % 992)) - 496) / 62) / 4)) : 24);

                if (this.hasSpecialFlag)
                {
                    yDongleSource += 48;
                }

                b.Draw(this.Sprite.Texture, base.getLocalPosition(Game1.viewport) + new Vector2(32f, this.GetBoundingBox().Height - 16 + (4 * (-2 + Math.Abs(this.Sprite.currentFrame % 4 - 2))) + this.yOffset) * base.Scale, new Rectangle(xDongleSource, 168 + yDongleSource, 16, 24), this.color.Value, 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, base.Scale - 0.4f * ((float)(int)0 / 120000f)), base.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, base.drawOnTop ? 0.991f : ((float)base.StandingPixel.Y / 10000f + 0.0001f)));
            }

            b.Draw(this.Sprite.Texture, base.getLocalPosition(Game1.viewport) + (new Vector2(32f, this.GetBoundingBox().Height / 2 + ((-1 <= 0) ? (4 * (-2 + Math.Abs(this.Sprite.currentFrame % 4 - 2))) : (4 - 4 * (this.Sprite.currentFrame % 4 % 3))) + this.yOffset) + this.facePosition.Value) * Math.Max(0.2f, base.Scale - 0.4f * ((float)(int)0 / 120000f)), new Rectangle(32, 120, 16, 24), Color.White * ((this.FacingDirection == 0) ? 0.5f : 1f), 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, base.Scale - 0.4f * ((float)(int)0 / 120000f)), SpriteEffects.None, Math.Max(0f, base.drawOnTop ? 0.991f : ((float)(base.StandingPixel.Y + 0 * 2) / 10000f + 0.0001f)));
        }

        public void setMoving(Vector2 motion)
        {
            this.motion.Value = motion;
        }

        internal void resetForNewLocation(Vector2 position)
        {
            base.Position = position * 64f;
        }
    }
}
