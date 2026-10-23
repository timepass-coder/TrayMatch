using System;
using NUnit.Framework;
using TrayMatch.Core;
using TrayMatch.Generator;

namespace TrayMatch.Core.Tests
{
    public class RecipeBookTests
    {
        [Test]
        public void HasTenLevels()
        {
            Assert.AreEqual(10, RecipeBook.Count);
        }

        [Test]
        public void AllLevels_AreValid()
        {
            for (int level = 1; level <= RecipeBook.Count; level++)
            {
                LevelGeneratorTests.AssertValid(
                    RecipeBook.RecipeFor(level),
                    RecipeBook.SeedFor(level),
                    $"level {level}");

                Assert.AreEqual(level, RecipeBook.Generate(level).Level.Id);
            }
        }

        // Levels 1, 2 and 5 are prototype levels 1, 2 and 3. Level 1 also carries the tutorial.
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(5, 3)]
        public void PrototypeLevels_AreKept(int level, int prototypeLevel)
        {
            var expected = LevelGenerator.Generate(
                Recipe.Prototype(prototypeLevel),
                Recipe.PrototypeSeed(prototypeLevel)).Level;

            expected.Id = level;

            Assert.AreEqual(
                LevelSerializer.ToJson(expected),
                LevelSerializer.ToJson(RecipeBook.Generate(level).Level));
        }

        [Test]
        public void RecipeFor_ReturnsCopy()
        {
            RecipeBook.RecipeFor(3).Colors = 1;
            Assert.AreEqual(4, RecipeBook.RecipeFor(3).Colors);
        }

        [Test]
        public void LevelOutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RecipeBook.RecipeFor(0));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => RecipeBook.SeedFor(RecipeBook.Count + 1));
        }
    }
}