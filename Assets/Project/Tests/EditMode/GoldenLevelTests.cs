
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TrayMatch.Core;

namespace TrayMatch.Core.Tests
{
    // The level files exported from the web prototype carry a winning tap sequence in
    // meta.solution. Replaying it through GameState proves the C# rules match the prototype.
    public class GoldenLevelTests
    {
        static string LevelPath(string file)
        {
            // Unity runs tests from the project folder; the Mac test runner from its output folder.
            foreach (var root in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
            {
                string p = Path.Combine(root, "Assets", "Project", "Levels", file);
                if (File.Exists(p)) return p;
            }

            Assert.Fail($"Level file not found: Assets/Project/Levels/{file}");
            return null;
        }

        [TestCase("level_001.json")]
        [TestCase("level_002.json")]
        [TestCase("level_003.json")]
        public void Solution_WinsTheLevel(string file)
        {
            string json = File.ReadAllText(LevelPath(file));
            var state = new GameState(LevelSerializer.FromJson(json));
            var solution = (JArray)JObject.Parse(json)["meta"]["solution"];

            foreach (var tap in solution)
            {
                Assert.AreEqual(
                    GameStatus.Playing,
                    state.Status,
                    "game ended before the solution finished");

                var events = state.Tap((int)tap[0], (int)tap[1]);

                Assert.IsTrue(
                    events.OfType<LeftEvent>().Any(),
                    $"tap at ({tap[0]},{tap[1]}) did not send any block out");
            }

            Assert.AreEqual(GameStatus.Won, state.Status);
            Assert.GreaterOrEqual(state.MovesLeft, 0);
        }
    }
}
