using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OsmoVideoRenamer.File.Naming
{
    /// <summary>
    /// The parsed name of a video recorded by a DJI Osmo Pocket 3, for example
    /// DJI_20240315123456_0007_D.MP4: a 14-digit capture timestamp (yyyyMMddHHmmss),
    /// a 4-digit capture counter, and a letter suffix. This type is the single source
    /// of truth for the pattern.
    /// </summary>
    public sealed record DjiVideoFileName(DateTime CaptureTimestamp, int SequenceNumber)
    {
        public const string TimestampFormat = "yyyyMMddHHmmss";

        private const string MATCHING_FILE_PATTERN = @"^DJI_(?<timestamp>[0-9]{14})_(?<sequence>[0-9]{4})_[A-Z]+\.MP4$";

        private static readonly Regex _matchingFileRegex = new Regex(
            MATCHING_FILE_PATTERN,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool TryParse(string fileName, [NotNullWhen(true)] out DjiVideoFileName? result)
        {
            result = null;
            Match match = _matchingFileRegex.Match(fileName);
            if (!match.Success)
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                    match.Groups["timestamp"].Value,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime captureTimestamp))
            {
                return false;
            }

            int sequenceNumber = int.Parse(match.Groups["sequence"].Value, CultureInfo.InvariantCulture);
            result = new DjiVideoFileName(captureTimestamp, sequenceNumber);
            return true;
        }

        public static DjiVideoFileName Parse(string fileName)
        {
            if (TryParse(fileName, out DjiVideoFileName? result))
            {
                return result;
            }

            throw new ArgumentException($"'{fileName}' is not a DJI Osmo video file name.", nameof(fileName));
        }
    }
}
