using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SonorousDAB
{
    public class Track
    {
        public string Title { get; set; }
        public string AlbumTitle { get; set; }
        public string Artist { get; set; }
        public string ArtistId { get; set; }
        public string AlbumId { get; set; }
        public string ReleaseDate { get; set; }
        public string Genre { get; set; }
        public int Duration { get; set; } // Duration in seconds
        public string AlbumCover { get; set; }
        public AudioQuality AudioQuality { get; set; }
        public string Id { get; set; }
        public string StreamUrl => $"https://dabmusic.xyz/api/stream?trackId={Id}";
        // Cached stream URL after API call
        public string ResolvedStreamUrl { get; set; }


        public Visibility HiResVisibility => IsHiRes ? Visibility.Visible : Visibility.Collapsed;

        public bool IsHiRes => AudioQuality?.IsHiRes == true;

        public string BitDepthDisplay => AudioQuality?.MaximumBitDepth > 0
    ? $"{AudioQuality.MaximumBitDepth}-bit"
    : "Unknown";

        public string SampRateDisplay => AudioQuality?.MaximumSamplingRate > 0
    ? $"{AudioQuality.MaximumSamplingRate}khz"
    : "Unknown";

        public string AudioQualityDisplay
        {
            get
            {
                if (AudioQuality == null)
                    return "Unknown";

                var bitDepth = AudioQuality.MaximumBitDepth > 0
                    ? $"{AudioQuality.MaximumBitDepth}-bit"
                    : "N/A";

                var sampleRate = AudioQuality.MaximumSamplingRate > 0
                    ? $"{AudioQuality.MaximumSamplingRate}kHz"
                    : "N/A";

                //var isHighRes = AudioQuality.IsHiRes ? "(Hi-Res)" : null;

                return $"{bitDepth} / {sampleRate}";
            }
        }

    }

    public class SearchResponse
    {
        public List<Track> Tracks { get; set; }
    }

    public class AudioQuality
    {

        public int MaximumBitDepth { get; set; }
        public double MaximumSamplingRate { get; set; }
        public bool IsHiRes { get; set; }
    }
}
