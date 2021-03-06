﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Emergence
{
    class Symet
    {
        // Attributes

        Dictionary<int, Arm> arms;
        List<Vector2> vertices;
        List<ShapeBuilderVertice> skeletalVertices;
        List<ShapeBuilderVertice> dividerVertices;
        Vector2 position;
        double rotation;
        Vector2 velocity;
        float angularVelocity;
        float scale;
        bool alive;
        DNA dna;
        PrimitiveShape skeleton;
        PrimitiveShape segmentDividers;
        int hitPoints;
        int maxHitPoints;
        float volume;
        float totalVolume;

        PrimitiveShape bounds;

        List<int> movementSegments;
        float lastMovementSegment;
        float lastMovementArm;
        int lastMovementFire;

        int lastRegen;
        int energy;

        int worldID;

        List<SegmentShape> collidableShapes = new List<SegmentShape>();

        # region Properties

        public List<SegmentShape> CollidableShapes
        {
            get
            {
                return this.collidableShapes;
            }
            set
            {
                this.collidableShapes = value;
            }
        }
        public PrimitiveShape Skeleton
        {
            get
            {
                return this.skeleton;
            }
            set
            {
                this.skeleton = value;
            }
        }
        public float AngularVelocity
        {
            get
            {
                return this.angularVelocity;
            }
            set
            {
                this.angularVelocity = value;
            }
        }
        public int WorldID
        {
            get
            {
                return this.worldID;
            }
            set
            {
                this.worldID = value;
            }
        }
        public int Energy
        {
            get
            {
                return this.energy;
            }
            set
            {
                this.energy = value;
            }
        }
        public Vector2 Position
        {
            get
            {
                return this.position;
            }
            set
            {
                this.position = value;
                skeleton.Position = value;
                segmentDividers.Position = value;

                foreach (Arm arm in arms.Values)
                    arm.Position = value;
            }
        }

        public Vector2 Velocity
        {
            get
            {
                return this.velocity;
            }
            set
            {
                this.velocity = value;
            }
        }
        public double Rotation
        {
            get
            {
                return this.rotation;
            }
            set
            {
                this.rotation = value;
                skeleton.Rotation = Convert.ToSingle(value);
                segmentDividers.Rotation = Convert.ToSingle(value);

                foreach (Arm arm in arms.Values)
                    arm.Rotation = value;
            }
        }
        public float Scale
        {
            get
            {
                return this.scale;
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                this.scale = value;
                skeleton.Scale = value;
                segmentDividers.Scale = value;

                foreach (Arm arm in arms.Values)
                    arm.Scale = value;
            }
        }
        public bool Alive
        {
            get
            {
                return this.alive;
            }
            set
            {
                this.alive = value;
            }
        }

        #endregion

        // Functions

        public Symet()
        {
        }

        public Symet(List<Vector2> vertices, Dictionary<int, Arm> arms, DNA dna)
        {
            this.vertices = vertices;
            this.alive = false;
            this.arms = arms;
            this.dna = dna;

            this.energy = 9999;
            this.hitPoints = 100;
            this.scale = 1;
            this.rotation = Game1.Random.NextDouble() * 2 * Math.PI;
            this.velocity = new Vector2(0,0);
            this.angularVelocity = 0;

            // Start off with some BATTLE DAMAGE for show
            arms[0].SetSegmentAlive(1, false);
            arms[1].SetSegmentAlive(3, false);
            arms[2].SetSegmentAlive(2, false);
            arms[2].SetSegmentAlive(5, false);

            BuildSkeleton();

            // Get volume and set health
            volume = Symet.CalculateArea(this.vertices);
            totalVolume = arms[0].Volume * arms.Count;
            maxHitPoints = Convert.ToInt32(volume * 5);
            hitPoints = maxHitPoints;

            // Set the id of the arms
            foreach (int id in arms.Keys)
            {
                arms[id].ID = id;
            }

            // Get a list of our movement segment IDs to use for movment in Update()
            movementSegments = new List<int>();
            foreach(Chromosome chromosome in dna.Chromosomes.Values)
            {
                if (chromosome.Active && chromosome.Type == SegmentType.Movement)
                    movementSegments.Add(chromosome.ID);
            }

            // Get a random postition for the timers
            lastMovementFire = Convert.ToInt32(Game1.Random.NextDouble() * dna.MovementFrequency * 1000);
            lastRegen = Convert.ToInt32(Game1.Random.NextDouble() * 1000);

            // This will make their patterns have random starting positisions, ones that they would most likly not be able to achieve normally
            //lastMovementSegment = Convert.ToSingle(Game1.Random.NextDouble() * movementSegments.Count());
            //lastMovementArm = Convert.ToSingle(Game1.Random.NextDouble() * arms.Count());
            
            //while (lastMovementArm < .5)
            //    lastMovementArm = Convert.ToSingle(Game1.Random.NextDouble() * arms.Count() );
            //while (lastMovementSegment < .5)
            //    lastMovementSegment = Convert.ToSingle(Game1.Random.NextDouble() * movementSegments.Count());

            // This will make their starting positions appear on their pattern somewhere, make it a lot less random
            int randomNumber;
            randomNumber = Game1.Random.Next(100);
            lastMovementSegment = .5f;
            for (int i = 0; i < randomNumber; i++)
            {
                lastMovementSegment += dna.MovementFrequency;
                while (lastMovementSegment >= Convert.ToSingle(movementSegments.Count + .49999f))
                    lastMovementSegment -= Convert.ToSingle(movementSegments.Count - .00002f);
            }

            randomNumber = Game1.Random.Next(100);
            lastMovementArm = .5f;
            for (int i = 0; i < randomNumber; i++)
            {
                lastMovementArm += dna.MovementFrequency;
                while (lastMovementArm >= Convert.ToSingle(arms.Count() + .49999f))
                    lastMovementArm -= Convert.ToSingle(arms.Count() - .00002f);
            }
        }

        // Update function to grab the symet and keep some of its drawing and structural functions active
        public int GrabUpdate()
        {
            // Update each arm
            bool tempRebuildBool = false;
            foreach (Arm arm in arms.Values)
            {
                // Find out if we need to rebuild the skeleton
                if (arm.RebuildSkeleton)
                {
                    tempRebuildBool = true;
                    arm.RebuildSkeleton = false;
                }
            }

            if (tempRebuildBool)
                BuildSkeleton();

            skeleton.Update();
            segmentDividers.Update();

            velocity = Vector2.Zero;
            angularVelocity = 0;

            return 1;
        }
        public int Update(GameTime gameTime)
        {
            // Regenerate any lost hitpoints
            if (lastRegen > 1000 && energy > energy * .15)
            {
                lastRegen = 0;

                // Regen main body first
                hitPoints += 100 - hitPoints > 30 ? 30 : 100 - hitPoints;

                // Regen all the segments
                int tempEnergyDrain = 0;
                foreach (Arm arm in arms.Values)
                {
                    tempEnergyDrain += arm.Regenerate(energy / 3 > 100 ? 100 : energy / 3);
                }

                energy -= tempEnergyDrain;
            }
            lastRegen += gameTime.ElapsedGameTime.Milliseconds;

            // Do movement
            DoMovement(gameTime);

            // Update each arm
            bool tempRebuildBool = false;
            foreach (Arm arm in arms.Values)
            {
                arm.Update(gameTime);

                // Find out if we need to rebuild the skeleton
                if (arm.RebuildSkeleton)
                {
                    tempRebuildBool = true;
                    arm.RebuildSkeleton = false;
                }
            }

            if (tempRebuildBool)
                BuildSkeleton();

            skeleton.Update();
            segmentDividers.Update();

            return 1;
        }

        public int Draw(PrimitiveBatch primitiveBatch)
        {
            skeleton.Draw(primitiveBatch);
            segmentDividers.Draw(primitiveBatch);

            //This code will draw white boxes around the symet
            //List<Vector2> vertices = new List<Vector2>();
            //List<Color> colors = new List<Color>();
 
            //vertices.Add(new Vector2(skeleton.Bounds.l, skeleton.Bounds.t));
            //vertices.Add(new Vector2(skeleton.Bounds.r, skeleton.Bounds.t));
            //vertices.Add(new Vector2(skeleton.Bounds.r, skeleton.Bounds.b));
            //vertices.Add(new Vector2(skeleton.Bounds.l, skeleton.Bounds.b));
            //colors.Add(Color.White);
            //colors.Add(Color.White);
            //colors.Add(Color.White);
            //colors.Add(Color.White);
            //bounds = new PrimitiveShape(vertices.ToArray(), colors.ToArray(), DrawType.LineLoop);
            //bounds.Draw(primitiveBatch);
            return 1;
        }

        private int DoMovement(GameTime gameTime)
        {
            if (lastMovementFire > (dna.MovementFrequency ) * 1000)
            {
                lastMovementFire = 0;

                // Figure out what segment of an arm needs to fire
                int segmentTofire = 0;
                lastMovementSegment += dna.MovementFrequency;
                while (lastMovementSegment >= Convert.ToSingle(movementSegments.Count + .49999f))
                    lastMovementSegment -= Convert.ToSingle(movementSegments.Count - .00002f);

                segmentTofire = Convert.ToInt32(lastMovementSegment);

                // Figure out what arm that segment should come from
                int armToFire = 0;
                lastMovementArm += dna.MovementFrequency;
                while (lastMovementArm >= Convert.ToSingle(arms.Count() + .49999f))
                    lastMovementArm -= Convert.ToSingle(arms.Count() - .00002f);

                armToFire = Convert.ToInt32(lastMovementArm);

                // Make sure this segment is alive, if not then nothing fires this time around
                if (arms[armToFire - 1].GetSegmentAlive(segmentTofire))
                {
                    // Calculate the velocity change and angular velocity
                    Vector2 tempMovement = dna.Chromosomes[movementSegments[segmentTofire - 1]].MovementVector;
                    tempMovement = Vector2.Transform(tempMovement,
                        Matrix.CreateRotationZ(Convert.ToSingle(rotation)) *
                        Matrix.CreateRotationZ(Convert.ToSingle(((2 * Math.PI) / Convert.ToInt32(dna.BodyShape) * armToFire))));

                    double tempAngle = GetAngle(tempMovement, arms[armToFire - 1].GetSegmentCenter(dna.Chromosomes[movementSegments[segmentTofire - 1]].ID));
                    Vector2 tempVector = arms[armToFire - 1].GetSegmentCenter(dna.Chromosomes[movementSegments[segmentTofire - 1]].ID);

                    // TODO: The last parameter controls how strong the spin is. Might base it off of weight in the future
                    angularVelocity += tempMovement.Length() * Convert.ToSingle(Math.Sin(tempAngle)) / tempVector.Length() * .14f; 

                    velocity += tempMovement;

                }
            }
            lastMovementFire += gameTime.ElapsedGameTime.Milliseconds;

            // Update position and rotation
            Rotation += angularVelocity * gameTime.ElapsedGameTime.Milliseconds / 15;
            Position += velocity * gameTime.ElapsedGameTime.Milliseconds / 15;

            // Dampen the velocities
            angularVelocity *= .96f;
            velocity *= .9f;

            if (velocity.Length() < new Vector2(.1f).Length())
                velocity = Vector2.Zero;

            if (angularVelocity < .005f)
                angularVelocity = 0;

            return 1;
        }

        private double GetAngle(Vector2 verticeOne, Vector2 verticeTwo)
        {
            return Math.Atan2(verticeTwo.Y - verticeOne.Y, verticeTwo.X - verticeOne.X);
        }

        // Builds a shape that outlines the whole symet
        public int BuildSkeleton()
        {
            skeletalVertices = new List<ShapeBuilderVertice>();
            dividerVertices = new List<ShapeBuilderVertice>();

            List<Vector2> tempVertices = new List<Vector2>();
            List<Color> tempColors = new List<Color>();

            int j = 0;
            foreach (Arm arm in arms.Values)
            {
                skeletalVertices.Add(new ShapeBuilderVertice(vertices[j], Symet.GetColor(dna.BodyType)));

                if (arm.Alive)
                {
                    dividerVertices.Add(new ShapeBuilderVertice(vertices[j], Symet.GetColor(dna.BodyType)));
                    dividerVertices.Add(new ShapeBuilderVertice(vertices[(j + 1) % vertices.Count], Symet.GetColor(dna.BodyType)));
                }

                skeletalVertices.AddRange(arm.RecursiveSkeletonBuilder(1, Symet.GetColor(dna.BodyType)));
                dividerVertices.AddRange(arm.SegmentDividerBuilder());
                j++;
            }

            // Split the skeletalVertices' ShapeBuilderVertice's into seperate lists
            foreach (ShapeBuilderVertice shapeBuilderVertice in skeletalVertices)
            {
                tempVertices.Add(shapeBuilderVertice.vertice);
                tempColors.Add(shapeBuilderVertice.color);
            }

            // Remove any duplicates
            for (int i = 0; i < tempVertices.Count; i++)
            {
                if (tempVertices[i] == tempVertices[(i + 1) % tempVertices.Count])
                {
                    tempVertices.RemoveAt(i);
                    tempColors.RemoveAt(i);
                }
            }
            skeleton = new PrimitiveShape(tempVertices.ToArray(), tempColors.ToArray(), DrawType.LineLoop);

            tempVertices = new List<Vector2>();
            tempColors = new List<Color>();

            // Split the dividerVertices' ShapeBuilderVertice's into seperate lists
            foreach (ShapeBuilderVertice shapeBuilderVertice in dividerVertices)
            {
                tempVertices.Add(shapeBuilderVertice.vertice);
                tempColors.Add(shapeBuilderVertice.color);
            }
            segmentDividers = new PrimitiveShape(tempVertices.ToArray(), tempColors.ToArray(), DrawType.LineList);

            // Match them to current transformation
            skeleton.Position = position;
            skeleton.Rotation = Convert.ToSingle(rotation);
            skeleton.Scale = scale;

            segmentDividers.Position = position;
            segmentDividers.Rotation = Convert.ToSingle(rotation);
            segmentDividers.Scale = scale;

            collidableShapes = GetSegmentShapes();

            return 1;
        }

        public List<SegmentShape> GetSegmentShapes()
        {
            List<SegmentShape> shapes = new List<SegmentShape>();

            foreach (Arm arm in arms.Values)
            {
                shapes.AddRange(arm.GetCollidableShapes());
            }

            return shapes;
        }

        // Calculate the area of this segment
        public static float CalculateArea(List<Vector2> vertices)
        {
            float area = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[(i + 1) % vertices.Count];

                area -= (v2.X - v1.X) * (v2.Y + v1.Y) * .5f;
            }

            return area;
        }
        // Get the color of a segmentType
        public static Color GetColor(SegmentType type)
        {
            switch (type)
            {
                case SegmentType.None:
                    break;
                case SegmentType.Attack:
                    return Color.Red;
                case SegmentType.Defend:
                    return Color.Blue;
                case SegmentType.Photo:
                    return Color.Lime;
                case SegmentType.Movement:
                    return Color.Turquoise;
                default:
                    break;
            }

            return Color.Black;
        }
    }
}
