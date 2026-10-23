using System;
using System.IO;
using NUnit.Framework;
using TrayMatch.Core;
using TrayMatch.Generator;

namespace TrayMatch.Core.Tests
{
    public class LevelPackTests
    {
        [Test]
        public void Pack_RoundTrips()
        {
            var pack = PackBuilder.Build();
            string json = LevelSerializer.ToJson(pack);

            Assert.AreEqual(
                json,
                LevelSerializer.ToJson(LevelSerializer.PackFromJson(json)));
        }

        [Test]
        public void Build_HasEveryRecipeBookLevel_WithMeta()
        {
            var pack = PackBuilder.Build();

            Assert.AreEqual(RecipeBook.Count, pack.Levels.Count);

            for (int i = 0; i < pack.Levels.Count; i++)
            {
                var level = pack.Levels[i];

                Assert.AreEqual(i + 1, level.Id);
                Assert.AreEqual(LevelGenerator.Version, level.Meta.Generator);
                Assert.AreEqual(RecipeBook.RoleFor(i + 1), level.Meta.Role);
                Assert.AreEqual(RecipeBook.SeedFor(i + 1), level.Meta.Seed);
                Assert.AreEqual(
                    level.MoveLimit - RecipeBook.RecipeFor(i + 1).Slack,
                    level.Meta.Solution.Count);
            }
        }

        [Test]
        public void LevelWithoutMeta_WritesNoMetaKey()
        {
            var level = new Level { Width = 1, Height = 1, MoveLimit = 1 };

            StringAssert.DoesNotContain("meta", LevelSerializer.ToJson(level));
        }

        [Test]
        public void ChangedLevels_FindsOnlyLevelsThatPlayDifferently()
        {
            var oldPack = PackBuilder.Build();

            var same = PackBuilder.Build();
            same.Levels[0].Meta.Role = "renamed"; // meta only; doesn't count
            Assert.IsEmpty(PackBuilder.ChangedLevels(oldPack, same));

            var edited = PackBuilder.Build();
            edited.Levels[2].Blocks[0].Color++;
            edited.Levels[6].MoveLimit++;
            edited.Levels.RemoveAt(9);

            CollectionAssert.AreEqual(
                new[] { 3, 7, 10 },
                PackBuilder.ChangedLevels(oldPack, edited));
        }

        [Test]
        public void PackFromJson_RejectsBadInput()
        {
            Assert.Throws<FormatException>(
                () => LevelSerializer.PackFromJson(""));

            Assert.Throws<FormatException>(
                () => LevelSerializer.PackFromJson("{ not json"));

            Assert.Throws<FormatException>(
                () => LevelSerializer.PackFromJson(
                    "{\"format\": 9, \"levels\": []}"));

            Assert.Throws<FormatException>(
                () => LevelSerializer.PackFromJson(
                    "{\"format\": 1, \"levels\": null}"));
        }

        // levels.json is what the game ships. If this fails after a RecipeBook change: TrayMatch → Bake Levels.
        [Test]
        public void BakedPack_MatchesRecipeBook()
        {
            var baked = LevelSerializer.PackFromJson(
                File.ReadAllText(GoldenLevelTests.LevelPath("levels.json")));

            var current = PackBuilder.Build();

            Assert.AreEqual(
                current.Levels.Count,
                baked.Levels.Count,
                "level count: bake again");

            CollectionAssert.IsEmpty(
                PackBuilder.ChangedLevels(baked, current),
                "levels differ: bake again");
        }

        [Test]
        public void BakedPack_SolutionsWin()
        {
            var baked = LevelSerializer.PackFromJson(
                File.ReadAllText(GoldenLevelTests.LevelPath("levels.json")));

            foreach (var level in baked.Levels)
            {
                var state = new GameState(level);

                foreach (var tap in level.Meta.Solution)
                    state.Tap(tap[0], tap[1]);

                Assert.AreEqual(GameStatus.Won, state.Status, $"level {level.Id}");
            }
        }
    }
}