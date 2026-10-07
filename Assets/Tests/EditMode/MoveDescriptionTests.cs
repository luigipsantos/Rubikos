using NUnit.Framework;
using Rubikos.Core;

namespace Rubikos.Tests
{
    public class MoveDescriptionTests
    {
        private static Move OneMove(string notation) => MoveNotation.Parse(notation)[0];

        [TestCase("R", "Turn the RED face clockwise")]
        [TestCase("U'", "Turn the WHITE face counter-clockwise")]
        [TestCase("F2", "Turn the GREEN face twice (half turn)")]
        [TestCase("L", "Turn the ORANGE face clockwise")]
        [TestCase("D'", "Turn the YELLOW face counter-clockwise")]
        [TestCase("B2", "Turn the BLUE face twice (half turn)")]
        public void OuterFaces_AreNamedByTheirCenterColor(string notation, string expected)
        {
            Assert.AreEqual(expected, MoveDescription.Describe(OneMove(notation), new CubeState()));
        }

        [Test]
        public void MiddleSlice_MentionsTheSliceLetter()
        {
            string text = MoveDescription.Describe(OneMove("M"), new CubeState());
            StringAssert.Contains("middle slice (M)", text);
        }

        [Test]
        public void AfterAMiddleSliceTurn_FacesAreNamedByWhatTheyShowNow()
        {
            var cube = new CubeState();
            cube.Apply(OneMove("M"));

            // An M turn moves the top, front, bottom and back centers, but not the right and left ones.
            StringAssert.Contains("RED", MoveDescription.Describe(OneMove("R"), cube));
            StringAssert.DoesNotContain("GREEN", MoveDescription.Describe(OneMove("F"), cube));
        }
    }
}