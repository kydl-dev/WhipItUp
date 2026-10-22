using System.Collections.Generic;
using NUnit.Framework;
using WhipItUp.Ui;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The Viewer tab's weapon pose: the whip coiled by default with a Straight / Coiled switch, no switch for the other families (T-015 S6b, D-057).</summary>
    [TestFixture]
    public class ViewerPoseTests
    {
        [Test]
        public void Starts_coiled_and_offers_the_straight_button()
        {
            var pose = new ViewerPose();
            Assert.That(pose.Coiled, Is.True);
            Assert.That(pose.ButtonKey, Is.EqualTo(ViewerPose.StraightKey), "a coiled whip offers 'Straight'");
            Assert.That(pose.ToString(), Is.EqualTo("coiled"));
        }

        [Test]
        public void Only_the_whip_has_the_switch()
        {
            Assert.That(ViewerPose.HasSwitch("Whip"), Is.True);
            Assert.That(ViewerPose.HasSwitch("Paddle"), Is.False);
            Assert.That(ViewerPose.HasSwitch("Flogger"), Is.False);
            Assert.That(ViewerPose.HasSwitch("whip"), Is.False, "family names are exact");
            Assert.That(ViewerPose.HasSwitch(null), Is.False);
            Assert.That(ViewerPose.HasSwitch(""), Is.False);
        }

        [Test]
        public void Only_the_whip_is_ever_shown_coiled()
        {
            var pose = new ViewerPose();
            Assert.That(pose.ShowsCoiled("Whip"), Is.True);
            Assert.That(pose.ShowsCoiled("Flogger"), Is.False, "the flogger keeps its rest pose although the switch is on 'coiled'");
            Assert.That(pose.ShowsCoiled("Paddle"), Is.False);
            Assert.That(pose.ShowsCoiled(null), Is.False);
        }

        [Test]
        public void Toggle_flips_the_pose_and_the_button_text()
        {
            var pose = new ViewerPose();
            Assert.That(pose.Toggle(), Is.True);
            Assert.That(pose.Coiled, Is.False);
            Assert.That(pose.ShowsCoiled("Whip"), Is.False, "straight view after the click");
            Assert.That(pose.ButtonKey, Is.EqualTo(ViewerPose.CoiledKey), "a straight whip offers 'Coiled'");
            Assert.That(pose.ToString(), Is.EqualTo("straight"));

            Assert.That(pose.Toggle(), Is.True);
            Assert.That(pose.Coiled, Is.True);
            Assert.That(pose.ShowsCoiled("Whip"), Is.True);
            Assert.That(pose.ButtonKey, Is.EqualTo(ViewerPose.StraightKey));
        }

        [Test]
        public void The_chosen_pose_stays_for_the_whip_when_other_families_are_looked_at()
        {
            var pose = new ViewerPose();
            pose.Toggle();                                   // whip set to straight
            Assert.That(pose.ShowsCoiled("Paddle"), Is.False);
            Assert.That(pose.ShowsCoiled("Whip"), Is.False, "back on the whip it is still straight");
        }

        [Test]
        public void Toggle_logs_both_poses()
        {
            var lines = new List<string>();
            var pose = new ViewerPose(lines.Add);
            pose.Toggle();
            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Does.Contain("coiled -> straight"));
        }

        [Test]
        public void Button_texts_exist_in_the_English_table()
        {
            Assert.That(UiText.En.ContainsKey(ViewerPose.StraightKey), Is.True);
            Assert.That(UiText.En.ContainsKey(ViewerPose.CoiledKey), Is.True);
            Assert.That(UiText.En[ViewerPose.StraightKey], Is.EqualTo("Straight"));
            Assert.That(UiText.En[ViewerPose.CoiledKey], Is.EqualTo("Coiled"));
        }
    }
}
