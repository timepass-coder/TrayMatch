using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TrayMatch.Core;
using TrayMatch.Generator;

namespace TrayMatch.Core.Tests
{
    public class LevelGeneratorTests
    {
        static GeneratedLevel PrototypeLevel(int level) =>
            LevelGenerator.Generate(Recipe.Prototype(level), Recipe.PrototypeSeed(level));

        // Values from mulberry32() in prototype/web/index.html, times 2^32.
        [TestCase(0, new[] { 1144304738L, 1416247L, 958946056L, 627933444L })]
        [TestCase(20257, new[] { 1333135766L, 2856554421L, 3387440907L, 761997094L })]
        [TestCase(-5, new[] { 2078107854L, 227493659L, 4033117029L, 3420415754L })]
        public void Mulberry32_MatchesPrototype(int seed, long[] expected)
        {
            var rng = new Mulberry32(seed);
            foreach (long e in expected)
                Assert.AreEqual(e, (long)(rng.Next() * 4294967296.0));
        }

        // level_001..003.json were exported from the browser prototype.
        [TestCase(1, "level_001.json")]
        [TestCase(2, "level_002.json")]
        [TestCase(3, "level_003.json")]
        public void PrototypeLevel_MatchesExportedFile(int level, string file)
        {
            var expected = LevelSerializer.FromJson(File.ReadAllText(GoldenLevelTests.LevelPath(file)));
            var got = PrototypeLevel(level).Level;

            Assert.AreEqual(expected.Width, got.Width);
            Assert.AreEqual(expected.Height, got.Height);
            Assert.AreEqual(expected.TraySize, got.TraySize);
            Assert.AreEqual(expected.MoveLimit, got.MoveLimit);
            Assert.AreEqual(expected.Blocks.Count, got.Blocks.Count);

            for (int i = 0; i < expected.Blocks.Count; i++)
            {
                var e = expected.Blocks[i];
                var g = got.Blocks[i];
                Assert.AreEqual((e.X, e.Y, e.Dir, e.Color),
                                (g.X, g.Y, g.Dir, g.Color), $"block {i}");
            }
        }

        // FNV-1a hash over prototype levels 1-100, computed in Node from the browser's generator
        // (lines "L:WxH:moveLimit:x,y,dir,color;..."). Catches small differences the 3 files miss.
        [Test]
        public void PrototypeLevels_1To100_MatchPrototypeChecksum()
        {
            var text = new System.Text.StringBuilder();
            for (int level = 1; level <= 100; level++)
            {
                var lv = PrototypeLevel(level).Level;
                text.Append($"{level}:{lv.Width}x{lv.Height}:{lv.MoveLimit}:");
                for (int i = 0; i < lv.Blocks.Count; i++)
                {
                    var b = lv.Blocks[i];
                    text.Append(i == 0 ? "" : ";").Append($"{b.X},{b.Y},{(int)b.Dir},{b.Color}");
                }

                text.Append('\n');
            }

            uint hash = 0x811c9dc5;
            foreach (char c in text.ToString())
                hash = unchecked((hash ^ c) * 0x01000193);

            Assert.AreEqual(2247959950u, hash);
        }

        [Test]
        public void SameRecipeAndSeed_GiveTheSameLevel()
        {
            var recipe = Recipe.Prototype(12);
            var a = LevelGenerator.Generate(recipe, 4242);
            var b = LevelGenerator.Generate(recipe, 4242);

            Assert.AreEqual(LevelSerializer.ToJson(a.Level), LevelSerializer.ToJson(b.Level));
            Assert.AreEqual(a.Seed, b.Seed);
        }

        [Test]
        public void PrototypeLevels_1To300_AreValid()
        {
            for (int level = 1; level <= 300; level++)
                AssertValid(Recipe.Prototype(level), Recipe.PrototypeSeed(level), $"level {level}");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(30)]
        public void ManySeeds_GiveValidLevels(int recipeLevel)
        {
            var recipe = Recipe.Prototype(recipeLevel);
            for (int seed = 1; seed <= 100; seed++)
                AssertValid(recipe, seed * 104729, $"recipe {recipeLevel}, seed {seed * 104729}");
        }

        // Walls and ice have no generator pass yet: listing them must not change the level.
        [Test]
        public void PlaceholderElements_DontChangeTheLevel()
        {
            var plain = Recipe.Prototype(5);
            var withElements = Recipe.Prototype(5);
            withElements.Elements.Add(new ElementSettings { Type = "wall", Chance = 0.1, Max = 4 });
            withElements.Elements.Add(new ElementSettings { Type = "ice", Chance = 0.12, Max = 6 });

            Assert.AreEqual(
                LevelSerializer.ToJson(LevelGenerator.Generate(plain, 99).Level),
                LevelSerializer.ToJson(LevelGenerator.Generate(withElements, 99).Level));
        }

        [Test]
        public void BadRecipes_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => LevelGenerator.Generate(null, 1));

            var unknown = Recipe.Prototype(1);
            unknown.Elements.Add(new ElementSettings { Type = "lava" });
            Assert.Throws<ArgumentException>(() => LevelGenerator.Generate(unknown, 1));

            var noColors = Recipe.Prototype(1);
            noColors.Colors = 0;
            Assert.Throws<ArgumentException>(() => LevelGenerator.Generate(noColors, 1));
        }

        // The invariants from docs/ARCHITECTURE.md §8: solvable, block count a multiple of 3,
        // the solution fits the move limit and never overflows the tray.
        static void AssertValid(Recipe recipe, int seed, string name)
        {
            var g = LevelGenerator.Generate(recipe, seed);
            Assert.IsNotNull(g, $"{name}: no level after {LevelGenerator.MaxAttempts} attempts");

            var level = g.Level;
            Assert.AreEqual(recipe.Width, level.Width, name);
            Assert.AreEqual(recipe.Height, level.Height, name);
            Assert.That(level.Blocks.Count, Is.GreaterThan(0), name);
            Assert.AreEqual(0, level.Blocks.Count % GameState.MatchCount, $"{name}: block count");
            Assert.AreEqual(g.Solution.Count + recipe.Slack, level.MoveLimit, $"{name}: move limit");

            var colors = new HashSet<int>();
            foreach (var b in level.Blocks) colors.Add(b.Color);
            Assert.That(colors.Count, Is.LessThanOrEqualTo(recipe.Colors), $"{name}: colors");

            // A fresh GameState with the level's real move limit: losing (tray full, out of moves)
            // or a bump anywhere along the solution fails the test.
            var state = new GameState(level);
            foreach (var tap in g.Solution)
            {
                Assert.AreEqual(GameStatus.Playing, state.Status, $"{name}: game ended early");
                int before = state.BlocksOnBoard;
                state.Tap(tap[0], tap[1]);
                Assert.That(state.BlocksOnBoard, Is.LessThan(before),
                    $"{name}: tap ({tap[0]},{tap[1]}) moved nothing");
            }

            Assert.AreEqual(GameStatus.Won, state.Status, $"{name}: solution didn't win");
            Assert.AreEqual(recipe.Slack, state.MovesLeft, $"{name}: moves left");
        }
    }
}