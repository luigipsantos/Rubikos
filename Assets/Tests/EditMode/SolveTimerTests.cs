using NUnit.Framework;
using Rubikos.Core;

namespace Rubikos.Tests
{
    public class SolveTimerTests
    {
        [Test]
        public void NewTimer_IsIdle()
        {
            var timer = new SolveTimer();
            Assert.AreEqual(SolveTimerState.Idle, timer.State);
            Assert.AreEqual(0.0, timer.ElapsedSeconds(100.0));
        }

        [Test]
        public void FirstMoveAfterArming_StartsTheClock()
        {
            var timer = new SolveTimer();
            timer.Arm();
            Assert.AreEqual(SolveTimerState.Armed, timer.State);

            timer.NotifyMove(10.0);

            Assert.AreEqual(SolveTimerState.Running, timer.State);
            Assert.AreEqual(2.5, timer.ElapsedSeconds(12.5), 1e-9);
        }

        [Test]
        public void MovesBeforeArming_AreIgnored()
        {
            var timer = new SolveTimer();
            timer.NotifyMove(10.0);
            Assert.AreEqual(SolveTimerState.Idle, timer.State);
        }

        [Test]
        public void LaterMoves_DoNotRestartTheClock()
        {
            var timer = new SolveTimer();
            timer.Arm();
            timer.NotifyMove(10.0);

            timer.NotifyMove(20.0);

            Assert.AreEqual(5.0, timer.ElapsedSeconds(15.0), 1e-9);
        }

        [Test]
        public void Stop_FreezesTheTime()
        {
            var timer = new SolveTimer();
            timer.Arm();
            timer.NotifyMove(10.0);

            timer.Stop(14.0);

            Assert.AreEqual(SolveTimerState.Finished, timer.State);
            Assert.AreEqual(4.0, timer.ElapsedSeconds(999.0), 1e-9);
        }

        [Test]
        public void Stop_WhenNotRunning_DoesNothing()
        {
            var timer = new SolveTimer();
            timer.Arm();

            timer.Stop(5.0);

            Assert.AreEqual(SolveTimerState.Armed, timer.State);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var timer = new SolveTimer();
            timer.Arm();
            timer.NotifyMove(1.0);
            timer.Stop(3.0);

            timer.Reset();

            Assert.AreEqual(SolveTimerState.Idle, timer.State);
            Assert.AreEqual(0.0, timer.ElapsedSeconds(10.0));
        }

        [Test]
        public void Arm_AfterAFinishedSolve_StartsFresh()
        {
            var timer = new SolveTimer();
            timer.Arm();
            timer.NotifyMove(1.0);
            timer.Stop(3.0);

            timer.Arm();
            timer.NotifyMove(100.0);

            Assert.AreEqual(1.0, timer.ElapsedSeconds(101.0), 1e-9);
        }

        [TestCase(0.0, "0:00.00")]
        [TestCase(5.5, "0:05.50")]
        [TestCase(65.25, "1:05.25")]
        [TestCase(600.0, "10:00.00")]
        [TestCase(59.999, "0:59.99")]
        [TestCase(0.29, "0:00.29")]
        [TestCase(-3.0, "0:00.00")]
        public void Format_ShowsMinutesSecondsAndHundredths(double seconds, string expected)
        {
            Assert.AreEqual(expected, SolveTimer.Format(seconds));
        }
    }
}