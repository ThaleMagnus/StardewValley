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
    internal class FrogFollower : NPC
    {
        private int ribbitTimer;
        private int jumpTimer;
        private int movementTimer;
        private readonly NetVector2 motion = new NetVector2(Vector2.Zero);
        private new readonly NetRectangle nextPosition = new NetRectangle();

        public FrogFollower(Vector2 position) : base(new AnimatedSprite(Game1.random.Next(0, 2) == 0 ? ResourceManager.FrogTexturePath : ResourceManager.FrogAlternativeTexturePath, 0, 16, 16), position * 64f, 2, "FrogFollower", null)
        {
            base.Breather = false;
            base.speed = ModEntry.Config.walkingSpeed;
            base.forceUpdateTimer = 9999;
            base.collidesWithOtherCharacters.Value = false;
            base.farmerPassesThrough = true;
            base.HideShadow = true;
            this.nextPosition.Value = this.GetBoundingBox();
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.currentLocation = location;
            base.update(time, location);
            base.forceUpdateTimer = 99999;

            Farmer f = Utility.isThereAFarmerWithinDistance(base.Tile, 10, base.currentLocation);
            if (f != null)
            {
                jumpTimer -= time.ElapsedGameTime.Milliseconds;
                movementTimer = !isMoving() && Vector2.Distance(base.Position, f.Position) > 256f ? movementTimer - time.ElapsedGameTime.Milliseconds : 1000;
                if (Vector2.Distance(base.Position, f.Position) > 640f || movementTimer <= 0)
                {
                    this.jump();
                    base.Position = f.Position;
                    this.movementTimer = 1000;
                }
                else if (Vector2.Distance(base.Position, f.Position) > 64f)
                {
                    ribbitTimer -= time.ElapsedGameTime.Milliseconds;
                    if (this.motion.Value == Vector2.Zero && Game1.random.NextDouble() < 0.5 && ribbitTimer <= 0)
                    {
                        Game1.playSound("croak");
                        ribbitTimer = 3000;
                    }
                    if (Game1.random.NextDouble() < 0.007)
                    {
                        this.jumpWithoutSound(Game1.random.Next(6, 9));
                    }
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
                    this.Sprite.Animate(time, 8, 4, 100f);
                    this.FacingDirection = 1;
                }
                else if (base.moveLeft || (Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X < 0f))
                {
                    this.Sprite.Animate(time, 12, 4, 100f);
                    this.FacingDirection = 3;
                }
                else if (base.moveUp || (Math.Abs(this.motion.Y) > Math.Abs(this.motion.X) && this.motion.Y < 0f))
                {
                    this.Sprite.Animate(time, 4, 4, 100f);
                    this.FacingDirection = 0;
                }
                else
                {
                    this.Sprite.Animate(time, 0, 4, 100f);
                    this.FacingDirection = 2;
                }
            }
            else
            {
                switch (this.FacingDirection)
                {
                    case 0:
                        this.Sprite.Animate(time, 20, 2, 500f);
                        break;
                    case 1:
                        this.Sprite.Animate(time, 16, 2, 500f);
                        break;
                    case 2:
                        this.Sprite.Animate(time, 18, 2, 500f);
                        break;
                    case 3:
                        this.Sprite.Animate(time, 22, 2, 500f);
                        break;
                }
            }
        }

        public void setMoving(Vector2 motion)
        {
            this.motion.Value = motion;
        }

        internal void resetForNewLocation(Vector2 position)
        {
            base.Position = position * 64f;
        }

        public override void jump()
        {
            if (jumpTimer <= 0)
            {
                base.jump();
                jumpTimer = Game1.random.Next(500, 3000);
            }
        }

        public override void jumpWithoutSound(float velocity = 8)
        {
            if (jumpTimer <= 0)
            {
                base.jumpWithoutSound(velocity);
                jumpTimer = Game1.random.Next(500, 3000);
            }
        }

        internal void forceJump()
        {
            base.jumpWithoutSound(5f);
            jumpTimer = Game1.random.Next(500, 3000);
        }
    }
}
