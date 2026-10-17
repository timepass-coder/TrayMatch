
using System;
using NUnit.Framework;
using TrayMatch.Core;

namespace TrayMatch.Core.Tests
{
    public class LevelSerializerTests
    {
        [Test]
        public void RoundTrip_KeepsAllData()
        {
            var level = new Level
            {
                Id = 7,
                Width = 4,
                Height = 5,
                TraySize = 6,
                MoveLimit = 12
            };

            level.Blocks.Add(new BlockData(1, 2, Direction.Left, 3));
            level.Blocks.Add(new BlockData(3, 4, Direction.Down, 0));

            var copy = LevelSerializer.FromJson(LevelSerializer.ToJson(level));

            Assert.AreEqual(Level.CurrentFormat, copy.Format);
            Assert.AreEqual(7, copy.Id);
            Assert.AreEqual(4, copy.Width);
            Assert.AreEqual(5, copy.Height);
            Assert.AreEqual(6, copy.TraySize);
            Assert.AreEqual(12, copy.MoveLimit);
            Assert.AreEqual(2, copy.Blocks.Count);
            Assert.AreEqual(1, copy.Blocks[0].X);
            Assert.AreEqual(2, copy.Blocks[0].Y);
            Assert.AreEqual(Direction.Left, copy.Blocks[0].Dir);
            Assert.AreEqual(3, copy.Blocks[0].Color);
            Assert.AreEqual(Direction.Down, copy.Blocks[1].Dir);
        }

        [Test]
        public void Writes_CamelCaseKeys_AndDirectionsAsWords()
        {
            var level = new Level { Width = 1, Height = 1, MoveLimit = 1 };
            level.Blocks.Add(new BlockData(0, 0, Direction.Up, 0));

            string json = LevelSerializer.ToJson(level);

            StringAssert.Contains("\"moveLimit\": 1", json);
            StringAssert.Contains("\"dir\": \"up\"", json);
        }

        [Test]
        public void Reads_DocumentedFormat_AndIgnoresUnknownKeys()
        {
            const string json = @"{
                ""format"": 1,
                ""id"": 23,
                ""width"": 7,
                ""height"": 8,
                ""traySize"": 5,
                ""moveLimit"": 20,
                ""goal"": { ""type"": ""clearBoard"" },
                ""blocks"": [
                    { ""x"": 3, ""y"": 0, ""dir"": ""right"", ""color"": 1 }
                ],
                ""meta"": {
                    ""generator"": ""v1.4.0"",
                    ""solution"": [[3, 0]]
                }
            }";

            var level = LevelSerializer.FromJson(json);

            Assert.AreEqual(23, level.Id);
            Assert.AreEqual(20, level.MoveLimit);
            Assert.AreEqual(Direction.Right, level.Blocks[0].Dir);
            Assert.AreEqual(1, level.Blocks[0].Color);
        }

        [Test]
        public void MissingTraySize_DefaultsToFive()
        {
            var level = LevelSerializer.FromJson(
                @"{ ""width"": 2, ""height"": 2, ""moveLimit"": 3, ""blocks"": [] }");

            Assert.AreEqual(5, level.TraySize);
        }

        [Test]
        public void NewerFormat_IsRejected()
        {
            Assert.Throws<FormatException>(() =>
                LevelSerializer.FromJson(
                    @"{ ""format"": 99, ""width"": 1, ""height"": 1 }"));
        }

        [Test]
        public void BrokenJson_IsRejected()
        {
            Assert.Throws<FormatException>(() =>
                LevelSerializer.FromJson(@"{ ""width"": "));

            Assert.Throws<FormatException>(() =>
                LevelSerializer.FromJson("   "));
        }
    }
}
