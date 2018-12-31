using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media;
using Windows.UI.Xaml.Controls;
using Windows.Storage.FileProperties;

namespace ultPlayer
{
    public class mPlayer
    {
        MediaPlayer pI = null; //Instance pro všechny funkce
        public MediaPlaybackList currPlaylist = new MediaPlaybackList();
        MediaPlaybackItem testSong = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("C:\\Users\\ValidUser\\Music\\settle.m4a"))); //V případě nouze je tu testovcí písnička!
        int[] currPlaylistHistory = new int[128]; //Paměť na již přehrané (předchozí) skladby
        string dinfo = ""; //Informace pro debug, nutné pro operace try/parse
        bool canSkip = true;
        bool canBeDescribed = true;
        bool IsFilesCached = true;
        public bool requestedGetPlaybackInfo = false;
        Random rand;

        StorageFile[] filesCached;
        SystemMediaTransportControlsDisplayUpdater currItemUpdater;
        string[] currItemUpdaterFileInfo = new string[2];
        SystemMediaTransportControlsDisplayUpdater playlistItemUpdater;
        string[] playlistItemUpdaterFileInfo = new string[2];

        StorageItemThumbnail thumbI;
        public MediaSource thumb;
        public int currItemIndex = 0;
        TimeSpan t_zero = new TimeSpan(0, 0, 0); 
        private repeatState rState;
        private shuffleState sState;

        enum repeatState {
            noRepeat = 0,
            singleRepeat = 1,
            playlistRepeat = 2
        }
        enum shuffleState
        {
            noShuffle = 0,
            classicShuffle = 1
        }
        public enum Option
        {
            Repeat,
            Shuffle
        }

        
        public void Initialize(MediaPlayer playerInstance = null)
        {
            pI = (playerInstance != null) ? playerInstance : new MediaPlayer();
            pI.AutoPlay = true;
            repeatState rState = repeatState.noRepeat;
            shuffleState sState = shuffleState.noShuffle;
            //pI.Source = testSong;
            currItemUpdater = pI.SystemMediaTransportControls.DisplayUpdater;
            playlistItemUpdater = pI.SystemMediaTransportControls.DisplayUpdater;
        }
        
        public void Play()
        {
            if (pI.Source == null)
            {
                PromptFile();
            }
            else if (pI.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                pI.Pause();
            }
            else
            {
                pI.Play();
            }
        }

        public void Next()
        {
            canSkip = true;
            try { Console.WriteLine(currPlaylist.Items[(currItemIndex + 1)]); }
            catch (ArgumentOutOfRangeException e) { canSkip = false; }

            if (canSkip)
            {
                currItemIndex++;
            }
            Pause(true);
            pI.Source = currPlaylist.Items[currItemIndex];
            UpdateUpdater(filesCached[currItemIndex], currItemUpdater, currItemUpdaterFileInfo);

            /*
            switch (rState)
            {
                case repeatState.singleRepeat: break; //leave next song the same (_nextItemIndex should equal currItemIndex)
                case repeatState.playlistRepeat: if (_nextItemIndex == currPlaylist.Items.Count) _nextItemIndex = 0; break;
                default: _nextItemIndex++; break;
            }
            
            switch (sState)
            {
                case shuffleState.classicShuffle: _nextItemIndex = rand.Next(0, currPlaylist.Items.Count); if (_nextItemIndex == currItemIndex) _nextItemIndex++; break;
                default: break;
            }
            */
        }

        public void Prev()
        {
            canSkip = true;
            try { Console.WriteLine(currPlaylist.Items[(currItemIndex - 1)]); }
            catch (ArgumentOutOfRangeException e) { canSkip = false; }

            if (canSkip)
            {
                currItemIndex--;
            }
            Pause(true);
            pI.Source = currPlaylist.Items[currItemIndex];
            UpdateUpdater(filesCached[currItemIndex], currItemUpdater, currItemUpdaterFileInfo);
        }

        public void ToggleOption(Option o)
        {
            switch (o)
            {
                case Option.Repeat: rState++; if ((int)rState > (Enum.GetNames(typeof(repeatState)).Length)) { rState = 0; }; break;
                case Option.Shuffle: sState++; if ((int)sState > (Enum.GetNames(typeof(shuffleState)).Length)) { sState = 0; }; break;
                default: throw new Exception("Invalid option is toggled");
            }
        }

        public async void PromptFile()
        {
            string[] filetypes = { ".m4a", ".mp3", ".webm", ".wma" };

            FileOpenPicker fop = new FileOpenPicker();
            fop.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            fop.ViewMode = PickerViewMode.List;
            foreach (string _filetype in filetypes)
            {
                fop.FileTypeFilter.Add(_filetype);
            }


            IReadOnlyList<StorageFile> file = await fop.PickMultipleFilesAsync();

            if (file != null)
            {
                currPlaylist.Items.Clear();
                currItemIndex = 0;
                foreach (StorageFile f in file)
                {
                    currPlaylist.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromStorageFile(f)));
                }
                if (currPlaylist.Items.Count > 0)
                {
                    pI.Source = currPlaylist.Items[currItemIndex]; //[?]implementovat jinde
                    UpdatePlaybackInfo();
                    
                    filesCached = new StorageFile[currPlaylist.Items.Count];
                    for (int i = 0; i < currPlaylist.Items.Count; i++)
                    {
                        //list
                        filesCached[i] = file[i];
                        UpdateUpdater(filesCached[i], playlistItemUpdater, playlistItemUpdaterFileInfo);
                    }
                }
            }
        }

        async void UpdateUpdater(StorageFile file, SystemMediaTransportControlsDisplayUpdater updater, string[] updaterfileinfo)
        {
            try {
                await updater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                updater.Update();
                updaterfileinfo[0] = file.DisplayName;
                updaterfileinfo[1] = file.Path;
            }
            catch (Exception e) { Debug.WriteLine(e.Message); requestedGetPlaybackInfo = true; }
        }

        public void UpdateCurrItemUpdater() //Veřejná metoda pro Obecný UpdateUpdater() pouze pro přehrávanou stopu
        {
            IsFilesCached = true;
            try { Console.WriteLine(filesCached[0].ToString()); }
            catch (Exception e) { IsFilesCached = false; }
            if (IsFilesCached) {
                UpdateUpdater(filesCached[currItemIndex], currItemUpdater, currItemUpdaterFileInfo);
            }
        }

        public void UpdatePlaybackInfo()
        {
            canBeDescribed = true;
            try { Console.WriteLine(filesCached[currItemIndex]); }
            catch (NullReferenceException e) { canBeDescribed = false; }
            if (canBeDescribed) {
                UpdateUpdater(filesCached[currItemIndex], currItemUpdater, currItemUpdaterFileInfo);
                getPlaybackThumb(filesCached[currItemIndex]);
            }
        }
    
        public void Pause(bool willStop=false)
        {
            pI.Pause();
            if (willStop)
            {
                pI.PlaybackSession.Position = t_zero;
            }
        }

        public void SetPlaybackRate(double speed)
        {
            pI.PlaybackSession.PlaybackRate = speed;
        }

        public void Seek(int _sec, bool forwards=true)
        {
            if (forwards) { pI.PlaybackSession.Position = pI.PlaybackSession.Position.Add(new TimeSpan(0, 0, _sec)); }
            else { pI.PlaybackSession.Position = pI.PlaybackSession.Position.Subtract(new TimeSpan(0, 0, _sec)); }
        }

        public void SetPos(int _min, int _sec)
        {
            pI.PlaybackSession.Position = new TimeSpan(0, _min, _sec);
        }

        public void SetPos(int _sec)
        {
            pI.PlaybackSession.Position = new TimeSpan(0, _sec / 60, _sec);
        }

        public string[] GetPlaybackInfo()
        {
            string[] _playbackInfo = new string[5]; //Obsahuje důležité informace (potřebné k vykreslení) o aktuálně přehrávané stopě aktuální pouze v době dotazu 
            try { _playbackInfo[0] = currItemUpdater.MusicProperties.Title; }
            catch (Exception e) { Debug.WriteLine(e.Message); requestedGetPlaybackInfo = true; }
            if ((currPlaylist.Items.Count > 0) && (requestedGetPlaybackInfo == false))
            {
                _playbackInfo[0] = currItemUpdater.MusicProperties.Title; //Title
                _playbackInfo[1] = currItemUpdater.MusicProperties.Artist; //Author
                _playbackInfo[2] = currItemUpdater.MusicProperties.AlbumTitle; //Album
                _playbackInfo[3] = currItemUpdaterFileInfo[1]; //Source (Path)

                if (String.IsNullOrEmpty(_playbackInfo[0])) _playbackInfo[0] = currItemUpdaterFileInfo[0];

                for (byte i = 0; i < _playbackInfo.Length; i++)
                {
                    if (String.IsNullOrEmpty(_playbackInfo[i])) _playbackInfo[i] = "Unknown";
                }
            }
            else
            {
                _playbackInfo[0] = "None"; //Title
                _playbackInfo[1] = "Nobody"; //Author
                _playbackInfo[2] = "None"; //Album
                _playbackInfo[3] = "(Unselected)"; //Source
            }

            return _playbackInfo;
        }

        /*
        public string[] GetPlaylistPlaybackInfo()
        {
            return new string[] { };
        }
        */

        public async void getPlaybackThumb(StorageFile f)
        {
            /*
            thumbI = await f.GetThumbnailAsync(ThumbnailMode.SingleItem);
            MediaSource.CreateFromStreamReference()
            */
            if (currItemUpdater.Thumbnail != null)
            {
                IRandomAccessStream thumb_stream = await currItemUpdater.Thumbnail.OpenReadAsync();
                thumb = MediaSource.CreateFromStream(thumb_stream, f.ContentType);
                //[?]callback místo dt_seconder
            }
        } 

        public string GetPlaybackStatus()
        {
            string _playbackState;
            switch (pI.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Playing: _playbackState = "Playing"; break;
                case MediaPlaybackState.Opening:
                case MediaPlaybackState.Buffering: _playbackState = "Loading"; break;
                default: _playbackState = "Resting"; break;
            }
            return _playbackState;
        }

        public string GetDebugPlaybackStatus()
        {
            return pI.PlaybackSession.PlaybackState.ToString();
        }

    }
}


/* MIGHT BE HANDY
 * currPlaylist.Items[currItemIndex].Source.MediaStreamSource.Duration
 * currPlaylist.Items[currItemIndex].Source.MediaStreamSource.Thumbnail
 */
