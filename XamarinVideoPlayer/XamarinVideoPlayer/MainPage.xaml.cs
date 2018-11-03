using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace XamarinVideoPlayer
{
    public partial class MainPage : ContentPage
    {
        private const int VideoNameTimeOut = 5000;
        private IEnumerable<TrackDescription> _trackDescriptions;
        private readonly int _fd;
        private bool _firstTimePlaying = true;
        private readonly object _lockObject = new object();
        private readonly List<string> _aspectAndCropRatios = new List<string>();
        private double _panValue = 0;

        public MainPage()
        {
            _fd = -1;
            InitializeComponent();
            CommonInitialize();
        }

        public MainPage(int fd)
        {
            InitializeComponent();
            CommonInitialize();
            this._fd = fd;
        }

        private void CommonInitialize()
        {
            if (!ScreenLock.IsActive)
                ScreenLock.RequestActive();

            ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;

            _aspectAndCropRatios.Add("4:3");
            _aspectAndCropRatios.Add("16:9");
            _aspectAndCropRatios.Add("16:10");
            _aspectAndCropRatios.Add("Auto");

            CenterGrid.GestureRecognizers.Add(GetDoubleTapGesture());
            CenterGrid.GestureRecognizers.Add(GetTapGesture());
            CenterGrid.GestureRecognizers.Add(GetPanRecognizer());
            //CenterGrid.GestureRecognizers.Add(GetPinchRecognizer());
        }

        void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            lock (_lockObject)
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Running:
                        // Translate and ensure we don't pan beyond the wrapped user interface element bounds.
                        var slideValue = GetSliderUpdatedValue(videoView.MediaPlayer.Length, e.TotalX);

                        _panValue = Math.Min(slideValue + ProgressSlider.Value, ProgressSlider.Maximum);

                        break;
                    case GestureStatus.Completed:
                        ProgressSlider.Value = _panValue;
                        _panValue = 0;
                        break;
                }
            }
        }

        private double GetSliderUpdatedValue(long maxValue, double gestureValue)
        {
            var percentage = (gestureValue * 100) / maxValue;

            return maxValue * percentage / 1000;
        }

        private IGestureRecognizer GetDoubleTapGesture()
        {
            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 2
            };
            tapGestureRecognizer.Tapped += DoubleTapGestureRecognizer_Tapped;
            return tapGestureRecognizer;
        }

        private IGestureRecognizer GetPanRecognizer()
        {
            PanGestureRecognizer panGestureRecognizer = new PanGestureRecognizer();
            panGestureRecognizer.PanUpdated += OnPanUpdated;
            return panGestureRecognizer;
        }

        private IGestureRecognizer GetPinchRecognizer()
        {
            PinchGestureRecognizer pinchGestureRecognizer = new PinchGestureRecognizer();
            pinchGestureRecognizer.PinchUpdated += OnPinchUpdated;
            return pinchGestureRecognizer;
        }

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Running)
            {
                // Calculate the scale factor to be applied.
                videoView.MediaPlayer.Scale += (float)e.Scale - 1;
            }
        }

        private IGestureRecognizer GetTapGesture()
        {
            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 1
            };
            tapGestureRecognizer.Tapped += SingleTapGestureRecognizer_Tapped;
            return tapGestureRecognizer;
        }

        private void SingleTapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            HeaderGrid.IsVisible = !HeaderGrid.IsVisible;
            FooterGrid.IsVisible = !FooterGrid.IsVisible;
        }

        private void DoubleTapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            var index = _aspectAndCropRatios.FindIndex(x => x.Equals(videoView.MediaPlayer.AspectRatio));
            if (index + 1 > _aspectAndCropRatios.Count() - 1)
            {
                videoView.MediaPlayer.AspectRatio = _aspectAndCropRatios[0];
            }
            else
            {
                videoView.MediaPlayer.AspectRatio = _aspectAndCropRatios[index + 1];
            }
        }

        protected override void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                DisableAds();
                ShowLoading();
                Media media = null;

                if (!ScreenLock.IsActive)
                    ScreenLock.RequestActive();

                videoView.MediaPlayer.TimeChanged += EventManager_TimeChanged;
                videoView.MediaPlayer.Playing += EventManager_Playing;
                if (_fd > 0)
                {
                    media = new Media(videoView.LibVLC, _fd);
                }
                else
                {
                    media = new Media(videoView.LibVLC,
                        "http://www.quirksmode.org/html5/videos/big_buck_bunny.mp4",
                        Media.FromType.FromLocation);
                }
                videoView.MediaPlayer.Media = media;
                videoView.MediaPlayer.Play();
                LoadingFinished();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error occurred " + ex.Message);
            }
        }

        private void EventManager_Playing(object sender, EventArgs e)
        {
            if (!_firstTimePlaying)
                return;

            videoView.MediaPlayer.SetVideoTitleDisplay(Position.TopLeft, VideoNameTimeOut);
            ProgressSlider.Value = 0;
            var totalTime = TimeSpan.FromMilliseconds(videoView.MediaPlayer.Length);
            ProgressSlider.Maximum = totalTime.TotalSeconds;
            _firstTimePlaying = false;
            videoView.MediaPlayer.ToggleFullscreen();
            
            //Xamarin.Essentials.Platform.BeginInvokeOnMainThread(() =>
            //{
                // Ensure invoked on MainThread. Xamarin.Essentials will optimize this and check if you are already on the main thread
                VideoTotalTimeLabel.Text = totalTime.ToString(@"hh\:mm\:ss");
            //});

            _trackDescriptions = GetTrackDescriptions();
        }

        private void ShowLoading()
        {
            LoadingIndicator.IsEnabled = true;
            ProgressSlider.IsEnabled = false;
            PlayPauseButton.IsEnabled = false;
            //BtnSelectAudioTrack.IsEnabled = false;
        }

        private void LoadingFinished()
        {
            LoadingIndicator.IsEnabled = !LoadingIndicator.IsEnabled;
            ProgressSlider.IsEnabled = !ProgressSlider.IsEnabled;
            PlayPauseButton.IsEnabled = !PlayPauseButton.IsEnabled;
            //BtnSelectAudioTrack.IsEnabled = !BtnSelectAudioTrack.IsEnabled;
        }

        private void ProgressSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {

            var difference = e.NewValue - e.OldValue;
            difference = difference < 0 ? difference * -1 : difference;
            if (difference < 1)
                return;

            lock (_lockObject)
            {
                //var timeSpan = TimeSpan.FromSeconds(e.NewValue);
                var newTime = (long)TimeSpan.FromSeconds(e.NewValue).TotalMilliseconds;
                if (newTime > videoView.MediaPlayer.Length)
                    return;

                //ShowLoading();
                videoView.MediaPlayer.Time = newTime;

                //LoadingFinished();
            }
        }

        private void EventManager_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            lock (_lockObject)
            {
                ProgressSlider.Value = TimeSpan.FromMilliseconds(videoView.MediaPlayer.Time).TotalSeconds; //GetPercentage(videoView.MediaPlayer.Length, e.Time);	
            }
        }

        private TrackDescription[] GetTrackDescriptions()
        {
            _trackDescriptions = videoView.MediaPlayer.AudioTrackDescription.Any()
                ? videoView.MediaPlayer.AudioTrackDescription
                : new List<TrackDescription> {new TrackDescription {Id = 0, Name = "None"}}.ToArray();
            return _trackDescriptions.ToArray();
        }

        private void PlayButton_Clicked(object sender, System.EventArgs e)
        {
            if (videoView.MediaPlayer.IsPlaying)
            {
                PlayPauseButton.Image = "play.png";
                videoView.MediaPlayer.Pause();
                ShowAds();
            }
            else
            {
                PlayPauseButton.Image = "pause.png";
                videoView.MediaPlayer.Play();
                DisableAds();
            }
        }

        private void ShowAds()
        {
            AdMobView.IsVisible = true;
            AdMobView.IsEnabled = true;
        }

        private void DisableAds()
        {
            AdMobView.IsVisible = false;
            AdMobView.IsEnabled = false;
        }

        protected override bool OnBackButtonPressed()
        {
            videoView?.MediaPlayer?.Dispose();
            return base.OnBackButtonPressed();
        }

        private async Task DisplayAudioTracks()
        {
            var userSelection = await DisplayActionSheet("Audio Tracks", "Cancel", null,
                _trackDescriptions.
                Select(x => x.Name).ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                _trackDescriptions.Any(x => x.Name.Equals(userSelection)))
            {
                var result = videoView.MediaPlayer.SetAudioTrack(
                    _trackDescriptions.
                    First(x => x.Name.Equals(userSelection)).Id);

                if (!result)
                {
                    // Show error dialog to user
                    await DisplayAlert("Unable to set audio track",
                        "Selected audio track cannot be set, please try again or select any other audio track", "Ok");
                }
            }
        }

        private async Task DisplayAspectRatios()
        {
            var userSelection = await DisplayActionSheet("Aspect Ratio", "Cancel", null,
                _aspectAndCropRatios.ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                _aspectAndCropRatios.Any(x => x.Equals(userSelection)))
            {
                videoView.MediaPlayer.AspectRatio = userSelection;
            }
        }

        private async Task DisplayCropRatios()
        {
            var userSelection = await DisplayActionSheet("Aspect Ratio", "Cancel", null,
                _aspectAndCropRatios.ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                _aspectAndCropRatios.Any(x => x.Equals(userSelection)))
            {
                videoView.MediaPlayer.CropGeometry = userSelection;
            }
        }

        private async Task DisplaySettings()
        {
            List<string> settings = new List<string>();
            settings.Add("Aspect Ratio");
            settings.Add("Crop");
            settings.Add("Audio Track");
            settings.Add("Video Track");
            settings.Add("Playback Rate");
            //settings.Add("Video decoding");

            var userSelection = await DisplayActionSheet("Settings", "Cancel", null,
                settings.ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                settings.Any(x => x.Equals(userSelection)))
            {
                switch (userSelection)
                {
                    case "Aspect Ratio":
                        await DisplayAspectRatios();
                        break;
                    case "Crop":
                        await DisplayCropRatios();
                        break;
                    case "Audio Track":
                        await DisplayAudioTracks();
                        break;
                    case "Playback Rate":
                        await DisplayPlaybackRates();
                        break;
                    case "Video Track":
                        await DisplayVideoTracks();
                        break;
                    case "Video decoding":
                        await DisplayVideoDecoding();
                        break;
                }
            }
        }

        private async Task DisplayVideoDecoding()
        {
            var videoDecodings = new List<string>();
            videoDecodings.Add("Software decoding");
            videoDecodings.Add("Hardware decoding");

            var userSelection = await DisplayActionSheet("Video decodings", "Cancel", null,
                videoDecodings.ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                videoDecodings.Any(x => x.Equals(userSelection)))
            {
                _firstTimePlaying = true;
                videoView.MediaPlayer.Stop();
                var media = new Media(videoView.LibVLC, _fd);
                if (userSelection.Equals(videoDecodings[0]))
                {
                    videoView.MediaPlayer.Play(media);
                }
                else
                {
                    var configuration = new MediaConfiguration();
                    configuration.EnableHardwareDecoding();
                    media.AddOption(configuration);

                    videoView.MediaPlayer.Play(media);
                }
            }
        }

        //private async Task DisplayDegreeViews()
        //{
        //	List<string> playbackRates = new List<string>();
        //	playbackRates.Add("Normal video view");
        //	playbackRates.Add("360 degree video view");

        //	var userSelection = await DisplayActionSheet("Video view", "Cancel", null,
        //		playbackRates.ToArray());
        //	if (!string.IsNullOrWhiteSpace(userSelection) &&
        //		playbackRates.Any(x => x.Equals(userSelection)))
        //	{
        //		if (userSelection.Any(x => x.Equals("Normal video view")))
        //		{
        //			var videoTracks = videoView.MediaPlayer.Media.Tracks.ToList();

        //			//videoView.MediaPlayer.Media.Tracks.ToList()[0].Data.Video.Projection = VideoProjection.Equirectangular;
        //			//videoView.MediaPlayer.SetRate(videoView.MediaPlayer.Media.Tracks + (float)0.1);
        //		}
        //		else if (userSelection.Any(x => x.Equals("360 degree video view")))
        //		{
        //			videoView.MediaPlayer.SetRate(videoView.MediaPlayer.Rate + (float)0.1);
        //		}
        //	}
        //}

        private async Task DisplayPlaybackRates()
        {
            List<string> playbackRates = new List<string>();
            playbackRates.Add("Reset");
            playbackRates.Add("Fast");
            playbackRates.Add("Faster");
            playbackRates.Add("Slow");
            playbackRates.Add("Slower");

            var userSelection = await DisplayActionSheet("Playback Rates", "Cancel", null,
                playbackRates.ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                playbackRates.Any(x => x.Equals(userSelection)))
            {
                if (userSelection.Equals("Fast"))
                {
                    if (videoView.MediaPlayer.Rate < 5)
                        videoView.MediaPlayer.SetRate(videoView.MediaPlayer.Rate + (float)0.1);
                }
                else if (userSelection.Equals("Faster"))
                {
                    if (videoView.MediaPlayer.Rate < 4.6)
                        videoView.MediaPlayer.SetRate(videoView.MediaPlayer.Rate + (float)0.5);
                }
                else if (userSelection.Equals("Reset"))
                {
                    videoView.MediaPlayer.SetRate(1);
                }
                else if (userSelection.Equals("Slow"))
                {
                    if (videoView.MediaPlayer.Rate > 0.1)
                        videoView.MediaPlayer.SetRate(videoView.MediaPlayer.Rate - (float)0.1);
                }
                else if (userSelection.Equals("Slower"))
                {
                    if (videoView.MediaPlayer.Rate > 0.5)
                        videoView.MediaPlayer.SetRate(videoView.MediaPlayer.Rate - (float)0.5);
                }
            }
        }

        private async Task DisplayVideoTracks()
        {
            var videoTrackDescriptions = videoView.MediaPlayer.VideoTrackDescription.ToList();

            var userSelection = await DisplayActionSheet("Video Tracks", "Cancel", null,
                videoTrackDescriptions.
                Select(x => x.Name).ToArray());

            if (!string.IsNullOrWhiteSpace(userSelection) &&
                videoTrackDescriptions.Any(x => x.Name.Equals(userSelection)))
            {
                var selectedVideoTrackId = videoTrackDescriptions.
                    First(x => x.Name.Equals(userSelection)).Id;
                var result = videoView.MediaPlayer.SetVideoTrack(selectedVideoTrackId);

                if (!result)
                {
                    // Show error dialog to user
                    await DisplayAlert("Unable to set video track", "Selected video track cannot be set, please try again or select any other video track", "Ok");
                }
            }
        }

        private async void BtnSelectSetting_Clicked(object sender, EventArgs e)
        {
            await DisplaySettings();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            videoView.MediaPlayer.Dispose();
            videoView.LibVLC.Dispose();
        }
    }
}