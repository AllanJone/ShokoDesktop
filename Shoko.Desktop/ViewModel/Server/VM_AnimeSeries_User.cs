﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using NLog;
using Shoko.Commons.Extensions;
using Shoko.Commons.Notification;
using Shoko.Desktop.Utilities;
using Shoko.Desktop.ViewModel.Helpers;
using Shoko.Models.Client;
using Shoko.Models.Enums;
using Shoko.Models.Server;
using AniDBVoteType = Shoko.Models.Enums.AniDBVoteType;

// ReSharper disable InconsistentNaming

namespace Shoko.Desktop.ViewModel.Server
{
    public class VM_AnimeSeries_User : CL_AnimeSeries_User, IListWrapper, INotifyPropertyChangedExt
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));
        }


        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public int ObjectType => 2;
        public bool IsEditable => true;


      

        #region Sorting properties

        // These properties are used when sorting group filters, and must match the names on the VM_AnimeGroup_User

        public decimal AniDBRating => AniDBAnime?.AniDBAnime?.AniDBRating ?? 0;

        public DateTime? Stat_AirDate_Min => AniDBAnime?.AniDBAnime?.AirDate;

        public DateTime? Stat_AirDate_Max => AniDBAnime?.AniDBAnime?.AirDate;


        public string SortName => SeriesName;

        public string DateTimeCreatedAsString => DateTimeCreated.ToString("dd MMM yyyy - HH:mm", Commons.Culture.Global);

        public new int MissingEpisodeCount
        {
            get { return base.MissingEpisodeCount; }
            set { base.MissingEpisodeCount = this.SetField(base.MissingEpisodeCount, value, () => MissingEpisodeCount, () => HasMissingEpisodesAny, () => HasMissingEpisodesAllDifferentToGroups); }
        }
        
        public DateTime? Stat_SeriesCreatedDate => DateTimeCreated;

        public decimal? Stat_UserVoteOverall => AniDBAnime.UserRating;


        public new int UnwatchedEpisodeCount
        {
            get { return base.UnwatchedEpisodeCount; }
            set { base.UnwatchedEpisodeCount = this.SetField(base.UnwatchedEpisodeCount, value); }
        }

        #endregion

        public void PopulateIsFave()
        {
            this.OnPropertyChanged(() => IsFave);
        }



        public new VM_AniDB_AnimeDetailed AniDBAnime
        {
            get { return (VM_AniDB_AnimeDetailed)base.AniDBAnime; }
            set
            {
                base.AniDBAnime = this.SetField(base.AniDBAnime, value);
                VM_MainListHelper.Instance.AllAnimeDictionary[AniDBAnime.AniDBAnime.AnimeID] = AniDBAnime.AniDBAnime;
            }
        }

        public new List<VM_CrossRef_AniDB_TvDBV2> CrossRefAniDBTvDBV2
        {
            get { return base.CrossRefAniDBTvDBV2.CastList<VM_CrossRef_AniDB_TvDBV2>(); }
            set { base.CrossRefAniDBTvDBV2 = value.CastList<CrossRef_AniDB_TvDBV2>(); }
        }



        public new List<VM_TvDB_Series> TvDB_Series
        {
            get { return base.TvDB_Series.CastList<VM_TvDB_Series>(); }
            set
            {
                base.TvDB_Series = this.SetField(base.TvDB_Series, value.CastList<TvDB_Series>(),()=>SeriesName);
            }
        }


     

        #region Editable members

        private Boolean isReadOnly = true;
        public Boolean IsReadOnly
        {
            get { return isReadOnly; }
            set { isReadOnly = this.SetField(isReadOnly, value); }
        }

        private Boolean isBeingEdited;
        public Boolean IsBeingEdited
        {
            get { return isBeingEdited; }
            set { isBeingEdited = this.SetField(isBeingEdited,value); }
        }

        public Boolean IsSeriesNameOverridden => !string.IsNullOrEmpty(SeriesNameOverride);



        public new string SeriesNameOverride
        {
            get { return base.SeriesNameOverride; }
            set { base.SeriesNameOverride = this.SetField(base.SeriesNameOverride, value, ()=>SeriesNameOverride, ()=>IsSeriesNameOverridden,()=>SeriesName,()=>SeriesNameTruncated); }
        }


        public new int AnimeGroupID
        {
            get { return base.AnimeGroupID; }
            set { base.AnimeGroupID = this.SetField(base.AnimeGroupID, value); }
        }

       
        public new int WatchedCount
        {
            get { return base.WatchedCount; }
            set { base.WatchedCount = this.SetField(base.WatchedCount, value); }
        }

        public bool IsFave
        {
            get
            {
                VM_AnimeGroup_User grp = TopLevelAnimeGroup;
                if (grp == null || grp.IsFave == 0)
                    return false;
                return true;
            }
        }

        public bool HasMissingEpisodesAny => (MissingEpisodeCount > 0 || MissingEpisodeCountGroups > 0);

        public bool HasMissingEpisodesAllDifferentToGroups => (MissingEpisodeCount > 0 && MissingEpisodeCount != MissingEpisodeCountGroups);

        public bool HasMissingEpisodesGroups => MissingEpisodeCountGroups > 0;


        public new int MissingEpisodeCountGroups
        {
            get { return base.MissingEpisodeCountGroups; }
            set { base.MissingEpisodeCountGroups = this.SetField(base.MissingEpisodeCountGroups, value,()=>MissingEpisodeCountGroups, ()=> HasMissingEpisodesAny, ()=> HasMissingEpisodesAllDifferentToGroups, ()=> HasMissingEpisodesGroups); }
        }

        private string posterPath;
        public string PosterPath
        {
            get { return posterPath ?? AniDBAnime.AniDBAnime.DefaultPosterPath; }
            set
            {
                posterPath = this.SetField(posterPath, value);
            }
        }

        public string SeriesName
        {
            get
            {
                if (!string.IsNullOrEmpty(SeriesNameOverride))
                    return SeriesNameOverride;
                if (VM_ShokoServer.Instance.SeriesNameSource == DataSourceType.AniDB)
                    return AniDBAnime.AniDBAnime.FormattedTitle;
                if (TvDB_Series != null && TvDB_Series.Count > 0 && !string.IsNullOrEmpty(TvDB_Series[0].SeriesName) &&
                    !TvDB_Series[0].SeriesName.ToUpper().Contains("**DUPLICATE"))
                    return TvDB_Series[0].SeriesName;
                return AniDBAnime.AniDBAnime.FormattedTitle;
            }
        }

        public string SeriesNameTruncated
        {
            get
            {
                string ret = SeriesName;
                if (ret.Length > 30)
                    ret = ret.Substring(0, 26) + "...";
                return ret;
            }
        }

        public string GroupName
        {
            get
            {
                string grp;
                try
                {
                    if (Heirarchy.Count == 0)
                    {
                        VM_AnimeGroup_User grop;
                        grp = VM_MainListHelper.Instance.AllGroupsDictionary.TryGetValue(AnimeGroupID, out grop) ? grop.GroupName : SeriesName;
                    }
                    else
                    {
                        grp= Heirarchy.FirstOrDefault(g => g != null)?.GroupName;
                    }
                    // If failed to set revert to using series name
                    if (string.IsNullOrEmpty(grp?.Trim()))
                    {
                        grp = SeriesName;
                    }
                }
                catch (Exception)
                {
                    grp = SeriesName;
                }
                return grp;
            }

        }


        public new string DefaultFolder
        {
            get { return base.DefaultFolder; }
            set { base.DefaultFolder = this.SetField(base.DefaultFolder, value); }
        }


        #endregion

        public enum SortMethod { SortName = 0, AirDate = 1 };
        public static SortMethod SortType { get; set; }

        public bool IsComplete
        {
            get
            {
                if (!AniDBAnime.AniDBAnime.EndDate.HasValue) return false; // ongoing

                // all series have finished airing and the user has all the episodes
                return AniDBAnime.AniDBAnime.EndDate.Value < DateTime.Now && !HasMissingEpisodesAny;
            }
        }

        public bool FinishedAiring
        {
            get
            {
                if (!AniDBAnime.AniDBAnime.EndDate.HasValue) return false; // ongoing

                // all series have finished airing
                return AniDBAnime.AniDBAnime.EndDate.Value < DateTime.Now;
            }
        }

        public bool UserHasVotedPerm => AniDBAnime?.UserVote?.VoteType == (int)AniDBVoteType.Anime;

        public bool UserHasVotedAny => AniDBAnime?.UserVote != null;

        public HashSet<string> AllTags => AniDBAnime.AniDBAnime.GetAllTags();
        
        public HashSet<string> CustomTags => new HashSet<string>(AniDBAnime.CustomTags.Select(a => a.TagName));

        public bool HasUnwatchedFiles => UnwatchedEpisodeCount > 0;

        public bool AllFilesWatched => UnwatchedEpisodeCount == 0;

        public bool AnyFilesWatched => WatchedEpisodeCount > 0;


        public string Description
        {
            get
            {
                if (VM_ShokoServer.Instance.SeriesDescriptionSource == DataSourceType.AniDB)
                    return AniDBAnime.AniDBAnime.Description;

                if (TvDB_Series != null && TvDB_Series.Count > 0 && !string.IsNullOrEmpty(TvDB_Series[0].Overview))
                    return TvDB_Series[0].Overview;
                return AniDBAnime.AniDBAnime.Description;
            }
        }

        public string DescriptionTruncated
        {
            get
            {
                string trunc = Description;
                if (!string.IsNullOrEmpty(trunc) && trunc.Length > 500)
                    trunc = trunc.Substring(0, 500) + "...";

                return trunc;
            }
        }

        public string LastWatchedDescription
        {
            get
            {
                if (WatchedDate.HasValue)
                {
                    DateTime today = DateTime.Now;
                    DateTime yesterday = today.AddDays(-1);

                    if (WatchedDate.Value.Day == today.Day && WatchedDate.Value.Month == today.Month && WatchedDate.Value.Year == today.Year)
                        return Shoko.Commons.Properties.Resources.Today;

                    if (WatchedDate.Value.Day == yesterday.Day && WatchedDate.Value.Month == yesterday.Month && WatchedDate.Value.Year == yesterday.Year)
                        return Shoko.Commons.Properties.Resources.Yesterday;

                    return WatchedDate.Value.ToString("dd MMM yyyy", Commons.Culture.Global);
                }
                return "";
            }
        }


        public string EpisodeCountFormatted
        {
            get
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(AppSettings.Culture);

                // Multiple Episodes
                if (AniDBAnime.AniDBAnime.EpisodeCountNormal > 1)
                {
                    // Multiple Episodes, Multiple Specials
                    if (AniDBAnime.AniDBAnime.EpisodeCountSpecial > 1)
                    {
                        return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episodes} ({AniDBAnime.AniDBAnime.EpisodeCountSpecial} {Shoko.Commons.Properties.Resources.Anime_Specials})";
                    }
                    // Multiple Episodes, No Specials
                    if (AniDBAnime.AniDBAnime.EpisodeCountSpecial <= 0)
                    {
                        return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episodes} ({AniDBAnime.AniDBAnime.EpisodeCountSpecial} {Shoko.Commons.Properties.Resources.Anime_Specials})";
                    }
                    return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episodes} ({AniDBAnime.AniDBAnime.EpisodeCountSpecial} {Shoko.Commons.Properties.Resources.Anime_Special})";
                }
                // Single Episode, Multiple Specials
                if (AniDBAnime.AniDBAnime.EpisodeCountSpecial > 1)
                {
                    return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episode} ({AniDBAnime.AniDBAnime.EpisodeCountSpecial} {Shoko.Commons.Properties.Resources.Anime_Specials})";
                }
                // Single Episodes, No Specials
                if (AniDBAnime.AniDBAnime.EpisodeCountSpecial <= 0)
                {
                    return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episode} ({AniDBAnime.AniDBAnime.EpisodeCountSpecial} {Shoko.Commons.Properties.Resources.Anime_Specials})";
                }
                return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episode} ({AniDBAnime.AniDBAnime.EpisodeCountSpecial} {Shoko.Commons.Properties.Resources.Anime_Special})";
            }
        }

        public string EpisodeCountFormattedShort
        {
            get
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(AppSettings.Culture);

                if (AniDBAnime.AniDBAnime.EpisodeCountNormal > 1)
                {
                    return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episodes}";
                }
                return $"{AniDBAnime.AniDBAnime.EpisodeCountNormal} {Shoko.Commons.Properties.Resources.Anime_Episode}";
            }
        }

        public string NamesSummary
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(SeriesName);

                return sb.ToString();
            }
        }



        private List<VM_AnimeEpisode_User> allEpisodes;
        public List<VM_AnimeEpisode_User> AllEpisodes
        {
            get
            {
                if (allEpisodes == null)
                {
                    RefreshEpisodes();
                }
                return allEpisodes;
            }
        }

        public List<VM_VideoLocal> AllVideoLocals
        {
            get
            {
                try
                {
                    DateTime start = DateTime.Now;
                    List<VM_VideoLocal> vids = VM_ShokoServer.Instance.ShokoServices.GetVideoLocalsForAnime(AniDB_ID,
                        VM_ShokoServer.Instance.CurrentUser.JMMUserID).Cast<VM_VideoLocal>().ToList();
                    TimeSpan ts = DateTime.Now - start;
                    logger.Trace("Got vids for series from service: {0} in {1} ms", AniDB_ID, ts.TotalMilliseconds);
                    return vids;
                }
                catch (Exception ex)
                {
                    Utils.ShowErrorMessage(ex);
                }
                return new List<VM_VideoLocal>();
            }
        }


        public void RefreshEpisodes()
        {
            allEpisodes = new List<VM_AnimeEpisode_User>();

            try
            {
                DateTime start = DateTime.Now;
                List<VM_AnimeEpisode_User> eps = VM_ShokoServer.Instance.ShokoServices.GetEpisodesForSeries(AnimeSeriesID,
                    VM_ShokoServer.Instance.CurrentUser.JMMUserID).CastList<VM_AnimeEpisode_User>();
                TimeSpan ts = DateTime.Now - start;
                logger.Info("Got episode data from service: {0} in {1} ms", AniDBAnime.AniDBAnime.FormattedTitle, ts.TotalMilliseconds);
                start = DateTime.Now;

                VM_TvDBSummary tvSummary = AniDBAnime.AniDBAnime.TvSummary;
                // Normal episodes
                foreach (VM_AnimeEpisode_User epvm in eps)
                {
                    epvm.SetTvDBInfo(tvSummary);
                    
                }

                ts = DateTime.Now - start;
                logger.Info("Got episode contracts: {0} in {1} ms", AniDBAnime.AniDBAnime.FormattedTitle, ts.TotalMilliseconds);

                start = DateTime.Now;
                allEpisodes = eps.OrderBy(a => a.EpisodeNumber).ToList();
                ts = DateTime.Now - start;
                logger.Info("Sorted episode contracts: {0} in {1} ms", AniDBAnime.AniDBAnime.FormattedTitle, ts.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        public int LatestRegularEpisodeNumber
        {
            get
            {
                int latestEpNo = 0;

                try
                {
                    List<CL_AnimeEpisode_User> eps = VM_ShokoServer.Instance.ShokoServices.GetEpisodesForSeries(AnimeSeriesID,
                        VM_ShokoServer.Instance.CurrentUser.JMMUserID);
                    allEpisodes = new List<VM_AnimeEpisode_User>();

                    foreach (CL_AnimeEpisode_User ep in eps)
                    {
                        if ((enEpisodeType)ep.EpisodeType == enEpisodeType.Episode)
                        {
                            if (ep.EpisodeNumber > latestEpNo) latestEpNo = ep.EpisodeNumber;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.ShowErrorMessage(ex);
                }
                return latestEpNo;
            }
        }


        public List<VM_AnimeEpisodeType> EpisodeTypes
        {
            get
            {
                List<VM_AnimeEpisodeType> epTypes = new List<VM_AnimeEpisodeType>();

                try
                {
                    foreach (VM_AnimeEpisode_User ep in AllEpisodes)
                    {
                        VM_AnimeEpisodeType epType = new VM_AnimeEpisodeType(this, ep);

                        bool alreadyAdded = false;
                        foreach (VM_AnimeEpisodeType thisEpType in epTypes)
                        {
                            if (thisEpType.EpisodeType == epType.EpisodeType)
                            {
                                alreadyAdded = true;
                                break;
                            }
                        }
                        if (!alreadyAdded)
                            epTypes.Add(epType);
                    }
                    epTypes = epTypes.OrderBy(a => a.EpisodeType).ToList();
                }
                catch (Exception ex)
                {
                    Utils.ShowErrorMessage(ex);
                }
                return epTypes;
            }
        }


        public void Refresh()
        {
            this.OnPropertyChanged(() => SeriesName);
            this.OnPropertyChanged(() => SeriesNameTruncated);
            this.OnPropertyChanged(() => GroupName);
        }
        public void Populate(VM_AnimeSeries_User contract)
        {
            //TODO we can get detailed in here
            AniDBAnime = contract.AniDBAnime;
            CrossRefAniDBTvDBV2 = contract.CrossRefAniDBTvDBV2;
            TvDB_Series = contract.TvDB_Series;
            CrossRefAniDBMovieDB = contract.CrossRefAniDBMovieDB;
            CrossRefAniDBMAL = contract.CrossRefAniDBMAL;
            AniDB_ID = contract.AniDB_ID;
            AnimeGroupID = contract.AnimeGroupID;
            AnimeSeriesID = contract.AnimeSeriesID;
            DateTimeUpdated = contract.DateTimeUpdated;
            DateTimeCreated = contract.DateTimeCreated;
            DefaultAudioLanguage = contract.DefaultAudioLanguage;
            DefaultSubtitleLanguage = contract.DefaultSubtitleLanguage;
            SeriesNameOverride = contract.SeriesNameOverride;
            DefaultFolder = contract.DefaultFolder;
            LatestLocalEpisodeNumber = contract.LatestLocalEpisodeNumber;
            PlayedCount = contract.PlayedCount;
            StoppedCount = contract.StoppedCount;
            UnwatchedEpisodeCount = contract.UnwatchedEpisodeCount;
            WatchedCount = contract.WatchedCount;
            WatchedDate = contract.WatchedDate;
            EpisodeAddedDate = contract.EpisodeAddedDate;
            WatchedEpisodeCount = contract.WatchedEpisodeCount;
            MissingEpisodeCount = contract.MissingEpisodeCount;
            MissingEpisodeCountGroups = contract.MissingEpisodeCountGroups;
            Refresh();
        }



        public void RefreshBase()
        {
            VM_AnimeSeries_User contract = (VM_AnimeSeries_User)VM_ShokoServer.Instance.ShokoServices.GetSeries(AnimeSeriesID,
                VM_ShokoServer.Instance.CurrentUser.JMMUserID);
            Populate(contract);
            allEpisodes = null;
        }



        public bool Save()
        {
            try
            {
                CL_Response<CL_AnimeSeries_User> response=VM_ShokoServer.Instance.ShokoServices.SaveSeries(ToSaveRequest(), VM_ShokoServer.Instance.CurrentUser.JMMUserID);
                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    MessageBox.Show(response.ErrorMessage);
                    return false;
                }
                Populate((VM_AnimeSeries_User)response.Result);
                return true;
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
                return false;
            }
        }

        public CL_AnimeSeries_Save_Request ToSaveRequest()
        {
            return new CL_AnimeSeries_Save_Request
            {
                AniDB_ID = AniDB_ID,
                AnimeGroupID = AnimeGroupID,
                AnimeSeriesID = AnimeSeriesID,
                DefaultAudioLanguage = DefaultAudioLanguage,
                DefaultSubtitleLanguage = DefaultSubtitleLanguage,
                SeriesNameOverride = SeriesNameOverride,
                DefaultFolder = DefaultFolder
            };
        }

        /*public override List<MainListWrapper> GetDirectChildren()
		{
			List<MainListWrapper> eps = new List<MainListWrapper>();
			List<VM_AnimeEpisode_User> allEps = AllEpisodes;

			// check settings to see if we need to hide episodes

			VM_AnimeEpisode_User.SortType = VM_AnimeEpisode_User.SortMethod.EpisodeNumber;
			allEps.Sort();
			eps.AddRange(allEps);
			return eps;
		}*/

        public VM_AnimeGroup_User TopLevelAnimeGroup
        {
            get
            {
                try
                {
                    VM_AnimeGroup_User grp = VM_MainListHelper.Instance.AllGroupsDictionary.SureGet(AnimeGroupID);
                    while (grp?.AnimeGroupParentID != null)
                        grp = VM_MainListHelper.Instance.AllGroupsDictionary.SureGet(grp.AnimeGroupParentID.Value);
                    return grp;
                }
                catch (Exception ex)
                {
                    Utils.ShowErrorMessage(ex);
                    return null;
                }
            }

        }



        public List<IListWrapper> GetDirectChildren()
        {
            List<IListWrapper> eps = new List<IListWrapper>();
            
            try
            {
                List<VM_AnimeEpisodeType> allEpTypes = EpisodeTypes;

                // check settings to see if we need to hide episodes

                eps.AddRange(allEpTypes.OrderBy(a => a.EpisodeType));
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
            return eps;
        }

        public List<VM_AnimeGroup_User> Heirarchy
        {
            get
            {
                if (VM_MainListHelper.Instance.AllGroupsDictionary.Count == 0)
                {
                    VM_MainListHelper.Instance.UpdateAnimeGroups();
                }

                List<VM_AnimeGroup_User> groups = new List<VM_AnimeGroup_User>();

                if (VM_MainListHelper.Instance.AllGroupsDictionary.ContainsKey(AnimeGroupID))
                {
                    VM_AnimeGroup_User thisGrp = VM_MainListHelper.Instance.AllGroupsDictionary[AnimeGroupID];
                    groups.Add(thisGrp);
                    while (thisGrp.AnimeGroupParentID.HasValue)
                    {
                        if (VM_MainListHelper.Instance.AllGroupsDictionary.ContainsKey(thisGrp.AnimeGroupParentID.Value))
                        {
                            thisGrp = VM_MainListHelper.Instance.AllGroupsDictionary[thisGrp.AnimeGroupParentID.Value];
                            groups.Add(thisGrp);
                        }
                        else
                            return groups;
                    }
                }

                return groups;
            }
        }
    }
}