using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File
{
    /// <summary>
    /// Orders videos by the camera's capture counter (the NNNN in DJI_yyyyMMddHHmmss_NNNN_D.MP4).
    /// The timestamp is deliberately not used for ordering: the camera clock can be wrong or
    /// change time zone mid-trip, while the counter always reflects recording order.
    /// </summary>
    public class FileSort : IFileSort
    {
        private readonly ILogger<FileSort> _logger;
        private readonly INumberedVideoFileFactory _numberedVideoFileFactory;

        public FileSort(ILogger<FileSort> logger, INumberedVideoFileFactory numberedVideoFileFactory)
        {
            _logger = logger;
            _numberedVideoFileFactory = numberedVideoFileFactory;
        }

        public List<INumberedVideoFile> GetOrderedFiles(IEnumerable<IVideoFile> files, int? startingNumber)
        {
            int firstNumber = startingNumber ?? 1;
            VerifyStartingNumberIsNotNegative(firstNumber);
            List<IVideoFile> ordered = files.OrderBy(file => file.SequenceNumber).ToList();
            VerifyNoDuplicateSequenceNumbers(ordered);
            WarnIfTimestampsDisagreeWithSequenceOrder(ordered);
            return ordered
                .Select((file, i) => _numberedVideoFileFactory.Create(i + firstNumber, file))
                .ToList();
        }

        private void VerifyStartingNumberIsNotNegative(int startingNumber)
        {
            if (startingNumber < 0)
            {
                _logger.LogCritical("Cannot use starting number {startingNumber} because it is negative.", startingNumber);
                throw new ArgumentOutOfRangeException(nameof(startingNumber), "Starting number must be zero or greater.");
            }
        }

        private void VerifyNoDuplicateSequenceNumbers(List<IVideoFile> ordered)
        {
            List<int> duplicates = ordered
                .GroupBy(file => file.SequenceNumber)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
            {
                string duplicateList = string.Join(", ", duplicates);
                _logger.LogCritical(
                    "Sequence number(s) {duplicates} appear more than once; the camera counter restarted or files from more than one card are mixed together.",
                    duplicateList);
                throw new ArgumentException(
                    $"Sequence number(s) {duplicateList} appear more than once. The camera counter restarted or files from more than one card are mixed together; split them into separate directories and rename each directory separately.");
            }
        }

        private void WarnIfTimestampsDisagreeWithSequenceOrder(List<IVideoFile> ordered)
        {
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].CaptureTimestamp < ordered[i - 1].CaptureTimestamp)
                {
                    _logger.LogWarning(
                        "Timestamps are not in sequence order ({later} was recorded after {earlier} but has an earlier timestamp); the camera clock may have changed, the time zone may have been adjusted, or files from more than one card may be mixed together. Files are ordered by sequence number.",
                        ordered[i].Name,
                        ordered[i - 1].Name);
                    return;
                }
            }
        }
    }
}
