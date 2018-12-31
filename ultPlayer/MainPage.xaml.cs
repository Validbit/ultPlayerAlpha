using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas.Effects;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ultPlayer
{
    /// <summary>
    /// Místo, kde se většina dění odehrává
    /// </summary>
    public sealed partial class MainPage : Page
    {
        mPlayer mp = new mPlayer();
        DispatcherTimer dt_occasional = new DispatcherTimer(); //Časovač pro věeobecné využití, kontroluje správnost zobrazení, vypisuje Debug hlášky, aj.
        DispatcherTimer dt_quick = new DispatcherTimer(); //Časovač podobný dt_occasional, jen o něco rychlejší
        DispatcherTimer dt_delay = new DispatcherTimer(); //Podobně jako LateInit() zamezuje chybám, které nastávají převážně při asynchronním volání
        Stopwatch sw_prev = new Stopwatch(); //Zjišťuje dobu, která uběhla ezi kliknutími na tlačítko "předchozí stopa", u kterého se vyhodnocuje, zda se má vrátit na začátek stopy či na stopu předchozí.
        Thickness browserTabMargin, featuresTabMargin, noTabMargin, bothTabMargin; //Obsahují (po inicializaci) definice odsazení grid_playback při všech kombinacích zobrazení

        enum visuallyKnownState //Poslední playbackState sjednaný s ostatnímy elementy na stránce
        {
            Playing = 0,
            Resting = 1,
            Loading = 2
        };
        visuallyKnownState vkState = visuallyKnownState.Resting;

        //Konfigurovatelné proměnné
        int seekTimeAmount = 3; //přeskok (>> nebo <<) v sekundách
        TimeSpan maxPrevElapsedTime = new TimeSpan(0, 0, 1); //Jak dlouhá doba uplyne dokud nezačne být stopa opakována při stisknutí tlačítka pro předchozí stopu
        //motiv
        //odsazeni grid_playback
        //jazyk

        public MainPage()
        {
            this.InitializeComponent();
            DefineAllAnimatableIcons(); //Slouží k definování umistění všech ikon, které mají být animovány, nebo mají být často měněny (např. při přejíždějícím kurzoru se změní obrázek)
            mp.Initialize(); //Vytvoří a načte přehrávač
            //[?]load&checkPrevSoundFolder

            
            
            dt_occasional.Interval = new TimeSpan(0, 0, 0, 0, 2400);
            dt_occasional.Tick += Dt_occasional_Tick;
            dt_occasional.Start();
            dt_quick.Interval = new TimeSpan(0, 0, 0, 0, 400);
            dt_quick.Tick += Dt_quick_Tick;
            dt_quick.Start();
            dt_delay.Interval = new TimeSpan(0, 0, 0, 0, 50);
            dt_delay.Tick += Dt_delay_Tick;


            //Schová elementy potřebné při vývoji aplikace
            RenderBrowserTab(false);
            RenderFeaturesTab(false);
            grid_playback.Margin = noTabMargin;
            grid_tab_features.Margin = noTabMargin;

            //
            browserTabMargin = new Thickness(0, 0, 320, 0);
            featuresTabMargin = new Thickness(0, 0, 0, 84);
            bothTabMargin = new Thickness(0, 0, 320, 84);
            noTabMargin = new Thickness(0, 0, 0, 0);

            LateInit();
            UpdateVisuals();
        }

        private void Dt_quick_Tick(object sender, object e)
        {
            try { _trackThumb.Source = new BitmapImage(mp.thumb.Uri); }
            catch (Exception ex) { Debug.WriteLine(ex.Message); mp.requestedGetPlaybackInfo = true; }

            if (mp.requestedGetPlaybackInfo)
            {
                if (!(dt_delay.IsEnabled))
                {
                    dt_delay.Start();
                }
            }
            else
            {
                UpdateVisuals(true);
                /*
                if (mp.thumb == null)
                {
                    imageBrushes["trackThumb"].ImageSource = imageBrushes["emptyThumb"].ImageSource;
                    imageBrushes["trackThumbBG"].ImageSource = imageBrushes["emptyThumbBG"].ImageSource;
                }
                else
                {
                    _trackThumb.Source = new BitmapImage(mp.thumb.Uri);
                    imageBrushes["trackThumb"].ImageSource = _trackThumb.Source;
                    imageBrushes["trackThumbBG"].ImageSource = _trackThumb.Source;
                }
                img_thumb.Source = imageBrushes["trackThumb"].ImageSource;
                img_thumb_bg.Source = imageBrushes["trackThumbBG"].ImageSource;
                */
            }
            
        }

        private void Dt_delay_Tick(object sender, object e)
        {
            if (mp.requestedGetPlaybackInfo)
            {
                mp.requestedGetPlaybackInfo = false; //Počká ještě jednou
            }
            else
            {
                dt_delay.Stop();
            }
            mp.UpdatePlaybackInfo();
            UpdateVisuals();
        }

        private void Dt_occasional_Tick(object sender, object e)
        {
            //Debug.WriteLine(mp.GetDebugPlaybackStatus());
            //UpdateVisuals();
            //mp.requestedGetPlaybackInfo = false;
        }

        public void UpdateVisuals(bool minimalUpdate=false) //Aktualizuje proměnné viditelné prvky aplikace (nadpisy, podnadpisy, tlačítka, dodatečné informace, aj.)
        {
            if (!minimalUpdate)
            {
                mp.UpdateCurrItemUpdater();
                string[] curr_playbackInfo = mp.GetPlaybackInfo();

                //Aktualizace informací o přehrávaném médiu
                lbl_song.Text = curr_playbackInfo[0];
                lbl_author.Text = curr_playbackInfo[1];
                //lbl_album.Text = curr_playbackInfo;[2];
                lbl_src.Text = curr_playbackInfo[3];
                //img_thumb.Source = (mp.getPlaybackThumb());
                //lbl_playbackState.Text = "NOW " + curr_playbackInfo[4].ToUpper(); -- NEPOUZIVANO
            }

            //Nastaví ikonu tlačítka přo ovládání přehrávání dle jeho aktuálního stavu
            string _visuallyKnownState = mp.GetPlaybackStatus(); //
            if (_visuallyKnownState == "Playing") { vkState = visuallyKnownState.Playing; if (!btn_play.IsPointerOver) { btn_play.Background = imageBrushes["pauseIcon"]; } }
            else if (_visuallyKnownState == "Loading") { vkState = visuallyKnownState.Loading; if (!btn_play.IsPointerOver) { btn_play.Background = imageBrushes["loadingIcon"]; } }
            else {  vkState = visuallyKnownState.Resting; if (!btn_play.IsPointerOver) { btn_play.Background = imageBrushes["playIcon"]; } }
        }

        void LateInit() //Pozdější inicializace prvků (za účelem zabránění chyb)
        {
            slider_playbackSpeed.StepFrequency = 0.05;
            slider_playbackSpeed.Minimum = 0.2;
            slider_playbackSpeed.Maximum = 3.2;
            slider_playbackSpeed.Value = 1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mp.Play();
            //[?]sp_browser.Children.Add(new TrackListing());
            
            dt_delay.Start();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            mp.Pause(true);
            dt_delay.Start();
        }

        private void btn_srcSelect_Click(object sender, RoutedEventArgs e)
        {
            mp.PromptFile();
            dt_delay.Start();
        }

        private void Slider_playbackSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            lbl_playbackSpeed.Text = String.Format("{0:f2}x", e.NewValue);
            mp.SetPlaybackRate(e.NewValue);
        }

        private void Lbl_playbackSpeed_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            slider_playbackSpeed.Value = 1.0d;
        }

        private void Btn_seek_back_Click(object sender, RoutedEventArgs e)
        {
            mp.Seek(seekTimeAmount,false);
        }

        private void Btn_seek_fwd_Click(object sender, RoutedEventArgs e)
        {
            mp.Seek(seekTimeAmount, true);
        }

        private void Btn_next_Click(object sender, RoutedEventArgs e)
        {
            mp.Next();
            dt_delay.Start();
        }

        private void Btn_prev_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (sw_prev.IsRunning)
            {
                sw_prev.Stop();
                if (sw_prev.ElapsedMilliseconds < maxPrevElapsedTime.TotalMilliseconds)
                {
                    mp.SetPos(0);
                }
                else
                {
                    mp.Prev();
                }
                sw_prev.Reset();
            }
            sw_prev.Start();
            */
            mp.Prev();
            dt_delay.Start();
        }

        private void Btn_close_tab_browser_Click(object sender, RoutedEventArgs e)
        {
            if (grid_playback.Margin == bothTabMargin) { grid_playback.Margin = featuresTabMargin; grid_tab_features.Margin = noTabMargin; RenderBrowserTab(false); }
            else if (grid_playback.Margin == featuresTabMargin) { grid_playback.Margin = bothTabMargin; grid_tab_features.Margin = browserTabMargin; RenderBrowserTab(true); }
            else if (grid_playback.Margin == noTabMargin) { grid_playback.Margin = browserTabMargin; grid_tab_features.Margin = browserTabMargin; RenderBrowserTab(true); }
            else { grid_playback.Margin = noTabMargin; grid_tab_features.Margin = noTabMargin; RenderBrowserTab(false); }
        }

        private void Btn_close_tab_features_Click(object sender, RoutedEventArgs e)
        {
            if (grid_playback.Margin == bothTabMargin) { grid_playback.Margin = browserTabMargin; grid_tab_features.Margin = browserTabMargin; RenderFeaturesTab(false); }
            else if (grid_playback.Margin == browserTabMargin) { grid_playback.Margin = bothTabMargin; grid_tab_features.Margin = browserTabMargin; RenderFeaturesTab(true); }
            else if (grid_playback.Margin == noTabMargin) { grid_playback.Margin = featuresTabMargin; grid_tab_features.Margin = noTabMargin; RenderFeaturesTab(true); }
            else { grid_playback.Margin = noTabMargin; grid_tab_features.Margin = noTabMargin; RenderFeaturesTab(false); }
        }

        private void Btn_promptFile_Click(object sender, RoutedEventArgs e)
        {
            mp.PromptFile();
        }

        private void RenderBrowserTab(bool visible)
        {
            if (visible)
            {
                grid_tab_browser.Visibility = Visibility.Visible;
                grid_tab_browser_label.Visibility = Visibility.Visible;
                grid_browser_category.Visibility = Visibility.Visible;
                btn_close_tab_browser.Visibility = Visibility.Collapsed;
            }
            else
            {
                grid_tab_browser.Visibility = Visibility.Collapsed;
                grid_tab_browser_label.Visibility = Visibility.Collapsed;
                grid_browser_category.Visibility = Visibility.Collapsed;
                btn_close_tab_browser.Visibility = Visibility.Visible;
            }
        }

        private void RenderFeaturesTab(bool visible)
        {
            if (visible)
            {
                grid_tab_features.Visibility = Visibility.Visible;
            }
            else
            {
                grid_tab_features.Visibility = Visibility.Collapsed;
            }
        }

        #region HoverEvents
        //Zde se nachází veškeré reakce na přejetí myši nad tlačíty (jsou téměř identické až na pár speciálních, jako např. Play/Pause)

        Dictionary<String, ImageBrush> imageBrushes = new Dictionary<string, ImageBrush>();

        ImageBrush playIcon = new ImageBrush();
        ImageBrush playHoverIcon = new ImageBrush();
        ImageBrush pauseIcon = new ImageBrush();
        ImageBrush pauseHoverIcon = new ImageBrush();
        ImageBrush loadingIcon = new ImageBrush();
        ImageBrush loadingHoverIcon = new ImageBrush();
        ImageBrush prevIcon = new ImageBrush();
        ImageBrush prevHoverIcon = new ImageBrush();
        ImageBrush seekBackIcon = new ImageBrush();
        ImageBrush seekBackHoverIcon = new ImageBrush();
        ImageBrush stopIcon = new ImageBrush();
        ImageBrush stopHoverIcon = new ImageBrush();
        ImageBrush seekFwdIcon = new ImageBrush();
        ImageBrush seekFwdHoverIcon = new ImageBrush();
        ImageBrush nextIcon = new ImageBrush();
        ImageBrush nextHoverIcon = new ImageBrush();
        ImageBrush musicIcon = new ImageBrush();
        ImageBrush musicHoverIcon = new ImageBrush();
        ImageBrush midiIcon = new ImageBrush();
        ImageBrush midiHoverIcon = new ImageBrush();
        ImageBrush bookIcon = new ImageBrush();
        ImageBrush bookHoverIcon = new ImageBrush();
        ImageBrush playlistIcon = new ImageBrush();
        ImageBrush playlistHoverIcon = new ImageBrush();
        ImageBrush settingsIcon = new ImageBrush();
        ImageBrush settingsHoverIcon = new ImageBrush();
        ImageBrush featuresIcon = new ImageBrush();
        ImageBrush featuresHoverIcon = new ImageBrush();
        ImageBrush browserIcon = new ImageBrush();
        ImageBrush browserHoverIcon = new ImageBrush();
        ImageBrush artistsIcon = new ImageBrush();
        ImageBrush artistsHoverIcon = new ImageBrush();
        ImageBrush tracksIcon = new ImageBrush();
        ImageBrush tracksHoverIcon = new ImageBrush();
        ImageBrush albumsIcon = new ImageBrush();
        ImageBrush albumsHoverIcon = new ImageBrush();
        ImageBrush emptyThumb = new ImageBrush();
        ImageBrush emptyThumbBG = new ImageBrush();
        ImageBrush trackThumb = new ImageBrush();
        ImageBrush trackThumbBG = new ImageBrush();

        Image _trackThumb = new Image();
        Image _trackThumbBG = new Image();

        void DefineAllAnimatableIcons()
        {
            Image _playIcon = new Image();
            Image _playHoverIcon = new Image();
            Image _pauseIcon = new Image();
            Image _pauseHoverIcon = new Image();
            Image _loadingIcon = new Image();
            Image _loadingHoverIcon = new Image();
            Image _prevIcon = new Image();
            Image _prevHoverIcon = new Image();
            Image _seekBackIcon = new Image();
            Image _seekBackHoverIcon = new Image();
            Image _stopIcon = new Image();
            Image _stopHoverIcon = new Image();
            Image _seekFwdIcon = new Image();
            Image _seekFwdHoverIcon = new Image();
            Image _nextIcon = new Image();
            Image _nextHoverIcon = new Image();
            Image _musicIcon = new Image();
            Image _musicHoverIcon = new Image();
            Image _midiIcon = new Image();
            Image _midiHoverIcon = new Image();
            Image _bookIcon = new Image();
            Image _bookHoverIcon = new Image();
            Image _playlistIcon = new Image();
            Image _playlistHoverIcon = new Image();
            Image _settingsIcon = new Image();
            Image _settingsHoverIcon = new Image();
            Image _featuresIcon = new Image();
            Image _featuresHoverIcon = new Image();
            Image _browserIcon = new Image();
            Image _browserHoverIcon = new Image();
            Image _artistsIcon = new Image();
            Image _artistsHoverIcon = new Image();
            Image _tracksIcon = new Image();
            Image _tracksHoverIcon = new Image();
            Image _albumsIcon = new Image();
            Image _albumsHoverIcon = new Image();
            Image _emptyThumb = new Image();
            Image _emptyThumbBG = new Image();

            imageBrushes.Add("playIcon", playIcon);
            imageBrushes.Add("playHoverIcon",playHoverIcon );
            imageBrushes.Add("pauseIcon", pauseIcon);
            imageBrushes.Add("pauseHoverIcon", pauseHoverIcon);
            imageBrushes.Add("loadingIcon", loadingIcon);
            imageBrushes.Add("loadingHoverIcon", loadingHoverIcon);
            imageBrushes.Add("prevIcon", prevIcon);
            imageBrushes.Add("prevHoverIcon", prevHoverIcon);
            imageBrushes.Add("seekBackIcon", seekBackIcon);
            imageBrushes.Add("seekBackHoverIcon", seekBackHoverIcon);
            imageBrushes.Add("stopIcon", stopIcon);
            imageBrushes.Add("stopHoverIcon",stopHoverIcon );
            imageBrushes.Add("seekFwdIcon", seekFwdIcon);
            imageBrushes.Add("seekFwdHoverIcon", seekFwdHoverIcon);
            imageBrushes.Add("nextIcon", nextIcon);
            imageBrushes.Add("nextHoverIcon", nextHoverIcon);
            imageBrushes.Add("musicIcon", musicIcon);
            imageBrushes.Add("musicHoverIcon", musicHoverIcon);
            imageBrushes.Add("midiIcon", midiIcon);
            imageBrushes.Add("midiHoverIcon", midiHoverIcon);
            imageBrushes.Add("bookIcon", bookIcon);
            imageBrushes.Add("bookHoverIcon", bookHoverIcon);
            imageBrushes.Add("playlistIcon", playlistIcon);
            imageBrushes.Add("playlistHoverIcon", playlistHoverIcon);
            imageBrushes.Add("settingsIcon", settingsIcon);
            imageBrushes.Add("settingsHoverIcon", settingsHoverIcon);
            imageBrushes.Add("featuresIcon", featuresIcon);
            imageBrushes.Add("featuresHoverIcon", featuresHoverIcon);
            imageBrushes.Add("browserIcon", browserIcon);
            imageBrushes.Add("browserHoverIcon", browserHoverIcon);
            imageBrushes.Add("artistsIcon", artistsIcon);
            imageBrushes.Add("artistsHoverIcon", artistsHoverIcon);
            imageBrushes.Add("tracksIcon", tracksIcon);
            imageBrushes.Add("tracksHoverIcon", tracksHoverIcon);
            imageBrushes.Add("albumsIcon", albumsIcon);
            imageBrushes.Add("albumsHoverIcon", albumsHoverIcon);
            imageBrushes.Add("emptyThumb", emptyThumb);
            imageBrushes.Add("emptyThumbBG", emptyThumbBG);
            imageBrushes.Add("trackThumb", trackThumb);
            imageBrushes.Add("trackThumbBG", trackThumbBG);

            _playIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/play.png"));
            imageBrushes["playIcon"].ImageSource = _playIcon.Source;
            _playHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/play_h.png"));
            imageBrushes["playHoverIcon"].ImageSource = _playHoverIcon.Source;

            _pauseIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/pause.png"));
            imageBrushes["pauseIcon"].ImageSource = _pauseIcon.Source;
            _pauseHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/pause_h.png"));
            imageBrushes["pauseHoverIcon"].ImageSource = _pauseHoverIcon.Source;

            _loadingIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/loading.png"));
            imageBrushes["loadingIcon"].ImageSource = _loadingIcon.Source;
            _loadingIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/loading_h.png"));
            imageBrushes["loadingHoverIcon"].ImageSource = _loadingHoverIcon.Source;

            _stopIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/stop.png"));
            imageBrushes["stopIcon"].ImageSource = _stopIcon.Source;
            _stopHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/stop_h.png"));
            imageBrushes["stopHoverIcon"].ImageSource = _stopHoverIcon.Source;

            _prevIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/prev.png"));
            imageBrushes["prevIcon"].ImageSource = _prevIcon.Source;
            _prevHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/prev_h.png"));
            imageBrushes["prevHoverIcon"].ImageSource = _prevHoverIcon.Source;

            _seekBackIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/seek_back.png"));
            imageBrushes["seekBackIcon"].ImageSource = _seekBackIcon.Source;
            _seekBackHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/seek_back_h.png"));
            imageBrushes["seekBackHoverIcon"].ImageSource = _seekBackHoverIcon.Source;

            _stopIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/stop.png"));
            imageBrushes["stopIcon"].ImageSource = _stopIcon.Source;
            _stopHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/stop_h.png"));
            imageBrushes["stopHoverIcon"].ImageSource = _stopHoverIcon.Source;

            _seekFwdIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/seek_fwd.png"));
            imageBrushes["seekFwdIcon"].ImageSource = _seekFwdIcon.Source;
            _seekFwdHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/seek_fwd_h.png"));
            imageBrushes["seekFwdHoverIcon"].ImageSource = _seekFwdHoverIcon.Source;

            _nextIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/next.png"));
            imageBrushes["nextIcon"].ImageSource = _nextIcon.Source;
            _nextHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Control/next_h.png"));
            imageBrushes["nextHoverIcon"].ImageSource = _nextHoverIcon.Source;

            _musicIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/music.png"));
            imageBrushes["musicIcon"].ImageSource = _musicIcon.Source;
            _musicHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/music_h.png"));
            imageBrushes["musicHoverIcon"].ImageSource = _musicHoverIcon.Source;

            _midiIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/midi.png"));
            imageBrushes["midiIcon"].ImageSource = _midiIcon.Source;
            _midiHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/midi_h.png"));
            imageBrushes["midiHoverIcon"].ImageSource = _midiHoverIcon.Source;

            _bookIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/book.png"));
            imageBrushes["bookIcon"].ImageSource = _bookIcon.Source;
            _bookHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/book_h.png"));
            imageBrushes["bookHoverIcon"].ImageSource = _bookHoverIcon.Source;

            _playlistIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/playlist.png"));
            imageBrushes["playlistIcon"].ImageSource = _playlistIcon.Source;
            _playlistHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/playlist_h.png"));
            imageBrushes["playlistHoverIcon"].ImageSource = _playlistHoverIcon.Source;

            _settingsIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/settings.png"));
            imageBrushes["settingsIcon"].ImageSource = _settingsIcon.Source;
            _settingsHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/settings_h.png"));
            imageBrushes["settingsHoverIcon"].ImageSource = _settingsHoverIcon.Source;

            _featuresIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/features.png"));
            imageBrushes["featuresIcon"].ImageSource = _featuresIcon.Source;
            _featuresHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/features_h.png"));
            imageBrushes["featuresHoverIcon"].ImageSource = _featuresHoverIcon.Source;

            _browserIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/browser.png"));
            imageBrushes["browserIcon"].ImageSource = _browserIcon.Source;
            _browserHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/browser_h.png"));
            imageBrushes["browserHoverIcon"].ImageSource = _browserHoverIcon.Source;

            _albumsIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/albums.png"));
            imageBrushes["albumsIcon"].ImageSource = _albumsIcon.Source;
            _albumsHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/albums_h.png"));
            imageBrushes["albumsHoverIcon"].ImageSource = _albumsHoverIcon.Source;

            _artistsIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/artists.png"));
            imageBrushes["artistsIcon"].ImageSource = _artistsIcon.Source;
            _artistsHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/artists_h.png"));
            imageBrushes["artistsHoverIcon"].ImageSource = _artistsHoverIcon.Source;

            _tracksIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/tracks.png"));
            imageBrushes["tracksIcon"].ImageSource = _tracksIcon.Source;
            _tracksHoverIcon.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/tracks_h.png"));
            imageBrushes["tracksHoverIcon"].ImageSource = _tracksHoverIcon.Source;

            _emptyThumb.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/emptyThumb.png"));
            imageBrushes["emptyThumb"].ImageSource = _emptyThumb.Source;
            _emptyThumbBG.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/emptyThumbBG.png"));
            imageBrushes["emptyThumbBG"].ImageSource = _emptyThumbBG.Source;

            _trackThumb.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/emptyThumb.png"));
            imageBrushes["trackThumb"].ImageSource = _trackThumb.Source;
            _trackThumbBG.Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/UI/Bars/emptyThumbBG.png"));
            imageBrushes["trackThumbBG"].ImageSource = _trackThumbBG.Source;
        }

        private void Btn_play_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (vkState == visuallyKnownState.Playing) { btn_play.Background = imageBrushes["pauseIcon"]; }
            else if (vkState == visuallyKnownState.Loading) { btn_play.Background = imageBrushes["loadingIcon"]; }
            else { btn_play.Background = imageBrushes["playIcon"]; }
        }

        private void Btn_play_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (vkState == visuallyKnownState.Playing) { btn_play.Background = imageBrushes["pauseHoverIcon"]; }
            else if (vkState == visuallyKnownState.Loading) { btn_play.Background = imageBrushes["loadingHoverIcon"]; }
            else { btn_play.Background = imageBrushes["playHoverIcon"]; }
        }

        private void Btn_music_artists_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            btn_music_artists.Background = imageBrushes["artistsIcon"];
        }

        private void Btn_music_artists_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            btn_music_artists.Background = imageBrushes["artistsHoverIcon"];
        }

        private void Btn_music_tracks_PointerExited(object sender, PointerRoutedEventArgs e)
        {
           
            btn_music_tracks.Background = imageBrushes["tracksIcon"];
        }

        private void Btn_music_tracks_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_music_tracks.Background = imageBrushes["tracksHoverIcon"];
        }

        private void Btn_music_albums_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_music_albums.Background = imageBrushes["albumsIcon"];
        }

        private void Btn_music_albums_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_music_albums.Background = imageBrushes["albumsHoverIcon"];
        }

        private void Btn_prev_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_prev.Background = imageBrushes["prevIcon"];
        }

        private void Btn_prev_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_prev.Background = imageBrushes["prevHoverIcon"];
        }

        private void Btn_seek_back_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_seek_back.Background = imageBrushes["seekBackIcon"];
        }

        private void Btn_seek_back_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            btn_seek_back.Background = imageBrushes["seekBackHoverIcon"];
        }

        private void Btn_stop_PointerExited(object sender, PointerRoutedEventArgs e)
        {   
            btn_stop.Background = imageBrushes["stopIcon"];
        }

        private void Btn_stop_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_stop.Background = imageBrushes["stopHoverIcon"];
        }

        private void Btn_seek_fwd_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_seek_fwd.Background = imageBrushes["seekFwdIcon"];
        }

        private void Btn_seek_fwd_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_seek_fwd.Background = imageBrushes["seekFwdHoverIcon"];
        }

        private void Btn_next_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_next.Background = imageBrushes["nextIcon"];
        }

        private void Btn_next_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_next.Background = imageBrushes["nextHoverIcon"];
        }

        private void Btn_close_tab_features_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_close_tab_features.Background = imageBrushes["featuresIcon"];
        }

        private void Btn_close_tab_features_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_close_tab_features.Background = imageBrushes["featuresHoverIcon"];
        }

        private void Btn_browser_settings_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_settings.Background = imageBrushes["settingsIcon"];
        }

        private void Btn_browser_settings_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_settings.Background = imageBrushes["settingsHoverIcon"];
        }

        private void Btn_browser_playlist_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_playlist.Background = imageBrushes["playlistIcon"];
        }

        private void Btn_browser_playlist_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_playlist.Background = imageBrushes["playlistHoverIcon"];
        }

        private void Btn_browser_book_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_book.Background = imageBrushes["bookIcon"];
        }

        private void Btn_browser_book_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_book.Background = imageBrushes["bookHoverIcon"];
        }

        private void Btn_browser_midi_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_midi.Background = imageBrushes["midiIcon"];
        }

        private void Btn_close_tab_browser_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            btn_close_tab_browser.Background = imageBrushes["browserHoverIcon"];
        }

        private void Btn_close_tab_browser_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            btn_close_tab_browser.Background = imageBrushes["browserIcon"];
        }

        private void Btn_browser_midi_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_browser_midi.Background = imageBrushes["midiHoverIcon"];
        }

        private void Btn_close_tab_browser_2_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
            btn_close_tab_browser_2.Background = imageBrushes["browserHoverIcon"];
        }

        private void Btn_close_tab_browser_2_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            
            btn_close_tab_browser_2.Background = imageBrushes["browserIcon"];
        }

        #endregion

        /* MIGHT BE USEFUL
 * 
 * private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var logged = Math.Log(volumeSlider.Value + 1) / Math.Log(101);
            ViewModel.Player.Volume = (logged >= 0 && logged <= 100) ? logged : Math.Max(logged - 100, logged + double.Epsilon);
        }
*/

    }
}
