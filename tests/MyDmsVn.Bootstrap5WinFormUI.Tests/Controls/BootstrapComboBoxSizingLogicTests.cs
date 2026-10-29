using System;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapComboBoxSizingLogicTests
{
    [TestCase(32, 6, 25, 32, 26, 26)]
    [TestCase(32, 6, 30, 36, 30, 30)]
    [TestCase(40, 8, 24, 40, 32, 32)]
    public void ResolveSeparatesOuterSelectionAndListItemHeights(
        int targetOuterHeight,
        int nativeChromeHeight,
        int minimumFieldHeight,
        int expectedOuterHeight,
        int expectedSelectionHeight,
        int expectedItemHeight)
    {
        var sizing = BootstrapComboBoxSizingLogic.Resolve(
            targetOuterHeight,
            nativeChromeHeight,
            minimumFieldHeight);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sizing.OuterHeight, Is.EqualTo(expectedOuterHeight));
            Assert.That(sizing.SelectionHeight, Is.EqualTo(expectedSelectionHeight));
            Assert.That(sizing.ItemHeight, Is.EqualTo(expectedItemHeight));
        }));
    }

    [Test]
    public void ResolveRejectsInvalidPhysicalMetrics()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapComboBoxSizingLogic.Resolve(0, 6, 20)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapComboBoxSizingLogic.Resolve(32, -1, 20)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapComboBoxSizingLogic.Resolve(32, 6, 0)));
    }
}
