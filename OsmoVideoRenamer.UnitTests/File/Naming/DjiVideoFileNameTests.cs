using OsmoVideoRenamer.File.Naming;

namespace OsmoVideoRenamer.UnitTests.File.Naming
{
    [TestClass]
    public class DjiVideoFileNameTests
    {
        [TestMethod]
        public void TryParse_ShouldParseStandardVideoName()
        {
            bool parsed = DjiVideoFileName.TryParse("DJI_20240315123456_0007_D.MP4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result.Should().NotBeNull();
            result!.CaptureTimestamp.Should().Be(new DateTime(2024, 3, 15, 12, 34, 56));
            result.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void TryParse_ShouldAcceptLowerCaseNames()
        {
            bool parsed = DjiVideoFileName.TryParse("dji_20240315123456_0007_d.mp4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result!.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void TryParse_ShouldAcceptMultiLetterSuffix()
        {
            bool parsed = DjiVideoFileName.TryParse("DJI_20240315123456_9999_DX.MP4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result!.SequenceNumber.Should().Be(9999);
        }

        [TestMethod]
        public void TryParse_ShouldAcceptLeapDay()
        {
            bool parsed = DjiVideoFileName.TryParse("DJI_20240229123456_0007_D.MP4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result!.CaptureTimestamp.Should().Be(new DateTime(2024, 2, 29, 12, 34, 56));
        }

        [TestMethod]
        public void TryParse_ShouldReturnFalseForNull()
        {
            bool parsed = DjiVideoFileName.TryParse(null, out DjiVideoFileName? result);

            parsed.Should().BeFalse();
            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow("GH010001.mp4")]
        [DataRow("DJX_20240315123456_0007_D.MP4")]
        [DataRow("DJI_2024031512345_0007_D.MP4")]
        [DataRow("DJI_202403151234567_0007_D.MP4")]
        [DataRow("DJI_20240315123456_007_D.MP4")]
        [DataRow("DJI_20240315123456_00007_D.MP4")]
        [DataRow("DJI_20240315123456_0007_.MP4")]
        [DataRow("DJI_20240315123456_0007_D.LRF")]
        [DataRow("DJI_20240315123456_0007_D.WAV")]
        [DataRow("DJI_20240315123456_0007_D.JPG")]
        [DataRow("DJI_20240315123456_0007_D.DNG")]
        [DataRow("xDJI_20240315123456_0007_D.MP4")]
        [DataRow("DJI_20240315123456_0007_D.MP4.bak")]
        [DataRow("DJI_20241315123456_0007_D.MP4")]
        [DataRow("DJI_20240230123456_0007_D.MP4")]
        [DataRow("DJI_20240315243456_0007_D.MP4")]
        [DataRow("notes.txt")]
        [DataRow("")]
        [DataRow("DJI_20240315123456_0007_D.MP4\n")]
        public void TryParse_ShouldRejectNonVideoNames(string fileName)
        {
            bool parsed = DjiVideoFileName.TryParse(fileName, out DjiVideoFileName? result);

            parsed.Should().BeFalse();
            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_ShouldReturnParsedName()
        {
            DjiVideoFileName result = DjiVideoFileName.Parse("DJI_20240315123456_0007_D.MP4");

            result.CaptureTimestamp.Should().Be(new DateTime(2024, 3, 15, 12, 34, 56));
            result.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void Parse_ShouldThrowForNonVideoName()
        {
            Action act = () => DjiVideoFileName.Parse("notes.txt");

            act.Should().ThrowExactly<ArgumentException>().WithMessage("*notes.txt*");
        }
    }
}
