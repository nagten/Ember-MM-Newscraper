' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports EmberAPI
Imports HtmlAgilityPack
Imports NLog
Imports System.IO
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json

Public Class SearchResults_Movie

#Region "Properties"

    Public Property ExactMatches() As New List(Of MediaContainers.Movie)

    Public Property PartialMatches() As New List(Of MediaContainers.Movie)

    Public Property PopularTitles() As New List(Of MediaContainers.Movie)

    Public Property TvTitles() As New List(Of MediaContainers.Movie)

    Public Property VideoTitles() As New List(Of MediaContainers.Movie)

    Public Property ShortTitles() As New List(Of MediaContainers.Movie)

#End Region 'Properties

End Class

Public Class SearchResults_TVShow

#Region "Properties"

    Public Property Matches() As New List(Of MediaContainers.TVShow)

#End Region 'Properties

End Class

Public Class Scraper

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Friend WithEvents bwIMDB As New ComponentModel.BackgroundWorker

    Private Const REGEX_Certifications As String = "<a href=""/search/title\?certificates=[^""]*"">([^<]*):([^<]*)</a>[^<]*(<i>([^<]*)</i>)?"
    Private Const REGEX_IMDBID As String = "tt\d\d\d\d\d\d\d"

    Private json_IMBD_next_data As IMDBJson = Nothing
    Private json_IMDB_Search_Results_next_data As IMDBSearchResultsJson = Nothing
    Private strPosterURL As String = String.Empty

    Private _SpecialSettings As IMDB_Data.SpecialSettings

#End Region 'Fields

#Region "Enumerations"

    Private Enum SearchType
        Details = 0
        Movies = 1
        SearchDetails_Movie = 2
        SearchDetails_TVShow = 3
        TVShows = 4
    End Enum

#End Region 'Enumerations

#Region "Events"

    Public Event Exception(ByVal ex As Exception)

    Public Event SearchInfoDownloaded_Movie(ByVal sPoster As String, ByVal sInfo As MediaContainers.Movie)
    Public Event SearchInfoDownloaded_TV(ByVal sPoster As String, ByVal sInfo As MediaContainers.TVShow)

    Public Event SearchResultsDownloaded_Movie(ByVal mResults As SearchResults_Movie)
    Public Event SearchResultsDownloaded_TV(ByVal mResults As SearchResults_TVShow)

#End Region 'Events

#Region "Methods"
    Public Sub New(ByVal SpecialSettings As IMDB_Data.SpecialSettings)
        _SpecialSettings = SpecialSettings
    End Sub

    Public Sub CancelAsync()

        If bwIMDB.IsBusy Then
            bwIMDB.CancelAsync()
        End If

        While bwIMDB.IsBusy
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        End While
    End Sub

    Private Sub bwIMDB_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles bwIMDB.DoWork
        Dim Args As Arguments = DirectCast(e.Argument, Arguments)

        Select Case Args.Search
            Case SearchType.Movies
                Dim r As SearchResults_Movie = SearchMovie(Args.Parameter, Args.Year)
                e.Result = New Results With {.ResultType = SearchType.Movies, .Result = r}

            Case SearchType.TVShows
                Dim r As SearchResults_TVShow = SearchTVShow(Args.Parameter)
                e.Result = New Results With {.ResultType = SearchType.TVShows, .Result = r}

            Case SearchType.SearchDetails_Movie
                Dim r As MediaContainers.Movie = GetMovieInfo(Args.Parameter, True, Args.Options_Movie)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_Movie, .Result = r}

            Case SearchType.SearchDetails_TVShow
                Dim r As MediaContainers.TVShow = GetTVShowInfo(Args.Parameter, Args.ScrapeModifiers, Args.Options_TV, True)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_TVShow, .Result = r}
        End Select
    End Sub

    Private Sub bwIMDB_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bwIMDB.RunWorkerCompleted
        Dim Res As Results = DirectCast(e.Result, Results)

        Select Case Res.ResultType
            Case SearchType.Movies
                RaiseEvent SearchResultsDownloaded_Movie(DirectCast(Res.Result, SearchResults_Movie))

            Case SearchType.TVShows
                RaiseEvent SearchResultsDownloaded_TV(DirectCast(Res.Result, SearchResults_TVShow))

            Case SearchType.SearchDetails_Movie
                Dim movieInfo As MediaContainers.Movie = DirectCast(Res.Result, MediaContainers.Movie)
                RaiseEvent SearchInfoDownloaded_Movie(strPosterURL, movieInfo)

            Case SearchType.SearchDetails_TVShow
                Dim showInfo As MediaContainers.TVShow = DirectCast(Res.Result, MediaContainers.TVShow)
                RaiseEvent SearchInfoDownloaded_TV(strPosterURL, showInfo)
        End Select
    End Sub

    Private Function FindYear(ByVal tmpname As String, ByVal movies As List(Of MediaContainers.Movie)) As Integer
        Dim tmpyear As String = String.Empty
        Dim i As Integer
        Dim ret As Integer = -1
        tmpname = Path.GetFileNameWithoutExtension(tmpname)
        tmpname = tmpname.Replace(".", " ").Trim.Replace("(", " ").Replace(")", "").Trim
        i = tmpname.LastIndexOf(" ")
        If i >= 0 Then
            tmpyear = tmpname.Substring(i + 1, tmpname.Length - i - 1)
            If Integer.TryParse(tmpyear, 0) AndAlso Convert.ToInt32(tmpyear) > 1950 Then 'let's assume there are no movies older then 1950
                For c = 0 To movies.Count - 1
                    If movies(c).Year = tmpyear Then
                        ret = c
                        Exit For
                    End If
                Next
            End If
        End If
        Return ret
    End Function

    Public Function DeserializeJsonObject(Of deserialize_Data)(jsonString As String) As deserialize_Data
        Try
            Return JsonConvert.DeserializeObject(Of deserialize_Data)(jsonString)
        Catch ex As JsonException

            logger.Error(ex, "Failed to deserialize JSON string.")
            Return Nothing
        End Try
    End Function

    Public Function GetMovieInfo(ByVal id As String, ByVal getposter As Boolean, ByVal filteredoptions As Structures.ScrapeOptions) As MediaContainers.Movie
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            If bwIMDB.CancellationPending Then Return Nothing

            Dim bIsScraperLanguage As Boolean = _SpecialSettings.PrefLanguage.ToLower.StartsWith("en")
            strPosterURL = String.Empty

            Dim nMovie As New MediaContainers.Movie With {
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = id},
                .Scrapersource = "IMDB"
            }

            Dim webParsing As New HtmlWeb
            Dim htmldReference As HtmlDocument = webParsing.Load(String.Concat("https://www.imdb.com/title/", id, "/reference/"))

            If webParsing.StatusCode <> 200 Then
                logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] failed to retrieve imdb reference page", id))
                Return Nothing
            End If

            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldReference.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then

                'Original Title
                If filteredoptions.bMainOriginalTitle Then
                    nMovie.OriginalTitle = json_IMBD_next_data.props.PageProps.MainColumnData.OriginalTitleText.Text
                End If

                'Title
                If filteredoptions.bMainTitle Then
                    If Not String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then
                        'Translated English title
                        nMovie.Title = json_IMBD_next_data.props.PageProps.MainColumnData.TitleText.Text
                    Else
                        nMovie.Title = json_IMBD_next_data.props.PageProps.MainColumnData.OriginalTitleText.Text
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Actors
                If filteredoptions.bMainActors Then
                    Dim nActors = ParseActors(json_IMBD_next_data)
                    If nActors IsNot Nothing Then
                        nMovie.Actors = nActors
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Actors", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Certifications
                If filteredoptions.bMainCertifications Then
                    Dim lstCertifications = ParseCertifications(json_IMBD_next_data)
                    If lstCertifications IsNot Nothing Then
                        nMovie.Certifications = lstCertifications
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Certifications", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Countries
                If filteredoptions.bMainCountries Then
                    Dim lstCountries = ParseCountries(json_IMBD_next_data)
                    If lstCountries IsNot Nothing Then
                        nMovie.Countries = lstCountries
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Countries", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Director
                If filteredoptions.bMainDirectors Then
                    Dim lstDirectors = ParseDirectors(json_IMBD_next_data)
                    If lstDirectors IsNot Nothing Then
                        nMovie.Directors = lstDirectors
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Directors", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Duration
                If filteredoptions.bMainRuntime Then
                    Dim strDuration = ParseDuration(json_IMBD_next_data)
                    If strDuration IsNot Nothing Then
                        nMovie.Runtime = strDuration
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Runtime", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Genres
                If filteredoptions.bMainGenres Then
                    Dim lstGenres = ParseGenres(json_IMBD_next_data)
                    If lstGenres IsNot Nothing Then
                        nMovie.Genres = lstGenres
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Genres", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'MPAA
                If filteredoptions.bMainMPAA Then
                    Dim strMPAA = ParseMPAA(json_IMBD_next_data, id)
                    If id IsNot Nothing Then
                        nMovie.MPAA = strMPAA
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse MPAA", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Outline
                If filteredoptions.bMainOutline AndAlso bIsScraperLanguage Then
                    Dim strOutline = ParseOutline(json_IMBD_next_data)
                    If strOutline IsNot Nothing Then
                        nMovie.Outline = strOutline
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Outline", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Plot
                If filteredoptions.bMainPlot AndAlso bIsScraperLanguage Then
                    Dim strPlot = ParsePlot(json_IMBD_next_data)

                    If Not String.IsNullOrEmpty(strPlot) Then
                        nMovie.Plot = strPlot
                    Else
                        'if "plot" isn't available then the "outline" will be used as plot
                        If nMovie.OutlineSpecified Then
                            nMovie.Plot = nMovie.Outline
                        End If
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Poster for search result
                If getposter Then
                    ParsePosterURL(json_IMBD_next_data)
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Premiered
                If filteredoptions.bMainPremiered Then
                    If json_IMBD_next_data.props.PageProps.MainColumnData.ReleaseDate IsNot Nothing Then
                        nMovie.Premiered = json_IMBD_next_data.props.PageProps.MainColumnData.ReleaseDate.GetFullReleaseDate()
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Rating
                If filteredoptions.bMainRating Then
                    Dim nRating = ParseRating(json_IMBD_next_data)
                    If nRating IsNot Nothing Then
                        nMovie.Ratings.Add(nRating)
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Rating", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Studios
                If filteredoptions.bMainStudios Then
                    Dim lstStudios = ParseStudios(json_IMBD_next_data)
                    If lstStudios IsNot Nothing Then
                        nMovie.Studios = lstStudios
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Studios", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Tagline
                If filteredoptions.bMainTagline AndAlso bIsScraperLanguage Then
                    Dim strTagline = ParseTagline(json_IMBD_next_data)
                    If strTagline IsNot Nothing Then
                        nMovie.Tagline = strTagline
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Tagline", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Top250
                If filteredoptions.bMainTop250 AndAlso json_IMBD_next_data.props.PageProps.MainColumnData.RatingsSummary IsNot Nothing AndAlso json_IMBD_next_data.props.PageProps.MainColumnData.RatingsSummary.topRanking IsNot Nothing Then
                    nMovie.Top250 = json_IMBD_next_data.props.PageProps.MainColumnData.RatingsSummary.topRanking.rank
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Top250", id))
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Credits (writers)
                If filteredoptions.bMainWriters Then
                    Dim lstCredits = ParseCredits(json_IMBD_next_data)
                    If lstCredits IsNot Nothing Then
                        nMovie.Credits = lstCredits
                    Else
                        logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Writers (Credits)", id))
                    End If
                End If

                Return nMovie
                Else
                    Return Nothing
            End If

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    Public Function GetTVEpisodeInfo(ByVal id As String, ByRef filteredoptions As Structures.ScrapeOptions) As MediaContainers.EpisodeDetails
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            If bwIMDB.CancellationPending Then Return Nothing

            Dim bIsScraperLanguage As Boolean = _SpecialSettings.PrefLanguage.ToLower.StartsWith("en")
            strPosterURL = String.Empty

            Dim nTVEpisode As New MediaContainers.EpisodeDetails With {
                .Scrapersource = "IMDB",
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVEpisode) With {.IMDbId = id}
            }

            Dim webParsingSeasons As New HtmlWeb
            Dim htmldReference As HtmlDocument = webParsingSeasons.Load(String.Concat("https://www.imdb.com/title/", id, "/reference/"))

            If webParsingSeasons.StatusCode <> 200 Then
                logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] failed to retrieve imdb page", id))
                Return Nothing
            End If

            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldReference.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then

                'Get season and episode number
                nTVEpisode.Episode = json_IMBD_next_data.props.PageProps.AboveTheFoldData.Series.episodeNumber.episodeNumber
                nTVEpisode.Season = json_IMBD_next_data.props.PageProps.AboveTheFoldData.Series.episodeNumber.seasonNumber

                'Original Title
                If filteredoptions.bEpisodeOriginalTitle Then
                    nTVEpisode.OriginalTitle = json_IMBD_next_data.props.PageProps.MainColumnData.OriginalTitleText.Text
                End If

                'Title
                If filteredoptions.bEpisodeTitle Then
                    If Not String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then
                        'Translated English title
                        nTVEpisode.Title = json_IMBD_next_data.props.PageProps.MainColumnData.TitleText.Text
                    Else
                        nTVEpisode.Title = json_IMBD_next_data.props.PageProps.MainColumnData.OriginalTitleText.Text
                    End If
                End If

                'Actors
                If filteredoptions.bEpisodeActors Then
                    Dim lstActors = ParseActors(json_IMBD_next_data)
                    If lstActors IsNot Nothing Then
                        nTVEpisode.Actors = lstActors
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] can't parse Actors", id))
                    End If
                End If

                'AiredDate
                If filteredoptions.bEpisodeAired Then
                    If json_IMBD_next_data.props.PageProps.MainColumnData.ReleaseDate IsNot Nothing Then
                        nTVEpisode.Aired = json_IMBD_next_data.props.PageProps.MainColumnData.ReleaseDate.GetFullReleaseDate()
                    End If
                End If

                'Credits (writers)
                If filteredoptions.bEpisodeCredits Then
                    Dim lstCredits = ParseCredits(json_IMBD_next_data)
                    If lstCredits IsNot Nothing Then
                        nTVEpisode.Credits = lstCredits
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] can't parse Credits (Writers)", id))
                    End If
                End If

                'Directors
                If filteredoptions.bEpisodeDirectors Then
                    Dim lstDirectors = ParseDirectors(json_IMBD_next_data)
                    If lstDirectors IsNot Nothing Then
                        nTVEpisode.Directors = lstDirectors
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] can't parse Directors", id))
                    End If
                End If

                'Plot
                If filteredoptions.bEpisodePlot AndAlso bIsScraperLanguage Then
                    Dim strPlot = ParsePlot(json_IMBD_next_data)

                    If Not String.IsNullOrEmpty(strPlot) Then
                        nTVEpisode.Plot = strPlot
                    Else
                        strPlot = ParseOutline(json_IMBD_next_data)
                        If Not String.IsNullOrEmpty(strPlot) Then
                            nTVEpisode.Plot = strPlot
                        Else
                            logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] no result from ""plotsummary"" page for Plot", id))
                        End If
                    End If
                End If

                'Rating
                If filteredoptions.bEpisodeRating Then
                    Dim nRating = ParseRating(json_IMBD_next_data)
                    If nRating IsNot Nothing Then
                        nTVEpisode.Ratings.Add(nRating)
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] can't parse Rating", id))
                    End If
                End If

                Return nTVEpisode
            Else
                Return Nothing
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    Public Function GetTVEpisodeInfo(ByVal showid As String, ByVal season As Integer, ByVal episode As Integer, ByRef filteredoptions As Structures.ScrapeOptions) As MediaContainers.EpisodeDetails
        If String.IsNullOrEmpty(showid) OrElse season = -1 OrElse episode = -1 Then Return Nothing

        Dim webParsingSeasons As New HtmlWeb
        Dim htmldEpisodes As HtmlDocument = webParsingSeasons.Load(String.Concat("https://www.imdb.com/title/", showid, "/episodes/?season=", season))

        If webParsingSeasons.StatusCode <> 200 Then
            logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] failed to retrieve imdb episode/seasons page", showid))
        Else
            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldEpisodes.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then
                Dim EpisodeItems As List(Of EpisodeItem)

                EpisodeItems = json_IMBD_next_data.props.PageProps.ContentData.Section.Episodes.items
                For Each EpisodeItem In EpisodeItems
                    If (Convert.ToInt32(EpisodeItem.episode) = episode) Then
                        Dim nEpisode As MediaContainers.EpisodeDetails = GetTVEpisodeInfo(EpisodeItem.id, filteredoptions)

                        If nEpisode IsNot Nothing Then
                            Return nEpisode
                        End If
                    End If
                Next
            End If
        End If

        Return Nothing
    End Function

    Public Sub GetTVSeasonInfo(ByRef nTVShow As MediaContainers.TVShow, ByVal showid As String, ByVal season As Integer, ByRef scrapemodifiers As Structures.ScrapeModifiers, ByRef filteredoptions As Structures.ScrapeOptions)
        Dim webParsingSeasons As New HtmlWeb
        Dim htmldEpisodes As HtmlDocument = webParsingSeasons.Load(String.Concat("https://www.imdb.com/title/", showid, "/episodes/?season=", season))

        If webParsingSeasons.StatusCode <> 200 Then
            logger.Trace(String.Format("[IMDB] [GetTVSeasonInfo] [ID:""{0}""] failed to retrieve imdb episode/seasons page", showid))
        Else
            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldEpisodes.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then
                Dim EpisodeItems As List(Of EpisodeItem)

                EpisodeItems = json_IMBD_next_data.props.PageProps.ContentData.Section.Episodes.items
                For Each EpisodeItem In EpisodeItems
                    Dim nEpisode As MediaContainers.EpisodeDetails = GetTVEpisodeInfo(EpisodeItem.id, filteredoptions)

                    If nEpisode IsNot Nothing Then
                        nTVShow.KnownEpisodes.Add(nEpisode)
                    End If
                Next
            End If

        End If
    End Sub

    Public Function GetTVShowInfo(ByVal id As String, ByVal scrapemodifier As Structures.ScrapeModifiers, ByVal filteredoptions As Structures.ScrapeOptions, ByVal getposter As Boolean) As MediaContainers.TVShow
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            If bwIMDB.CancellationPending Then Return Nothing

            Dim bIsScraperLanguage As Boolean = _SpecialSettings.PrefLanguage.ToLower.StartsWith("en")
            strPosterURL = String.Empty

            Dim nTVShow As New MediaContainers.TVShow With {
                .Scrapersource = "IMDB",
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVShow) With {.IMDbId = id}
            }

            Dim webParsing As New HtmlWeb
            Dim htmldReference As HtmlDocument = webParsing.Load(String.Concat("https://www.imdb.com/title/", id, "/reference/"))

            If webParsing.StatusCode <> 200 Then
                logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] failed to retrieve imdb reference page", id))
                Return Nothing
            End If

            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldReference.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then

                'Original Title
                If filteredoptions.bMainOriginalTitle Then
                    nTVShow.OriginalTitle = json_IMBD_next_data.props.PageProps.MainColumnData.OriginalTitleText.Text
                End If

                'Title
                If filteredoptions.bMainTitle Then
                    If Not String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then
                        'Translated English title
                        nTVShow.Title = json_IMBD_next_data.props.PageProps.MainColumnData.TitleText.Text
                    Else
                        nTVShow.Title = json_IMBD_next_data.props.PageProps.MainColumnData.OriginalTitleText.Text
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Actors
                If filteredoptions.bMainActors Then
                    Dim lstActors = ParseActors(json_IMBD_next_data)
                    If lstActors IsNot Nothing Then
                        nTVShow.Actors = lstActors
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Actors", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Certifications
                If filteredoptions.bMainCertifications Then
                    Dim lstCertifications = ParseCertifications(json_IMBD_next_data)
                    If lstCertifications IsNot Nothing Then
                        nTVShow.Certifications = lstCertifications
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Certifications", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Countries
                If filteredoptions.bMainCountries Then
                    Dim lstCountries = ParseCountries(json_IMBD_next_data)
                    If lstCountries IsNot Nothing Then
                        nTVShow.Countries = lstCountries
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Countries", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Creators
                If filteredoptions.bMainCreators Then
                    Dim lstCreators = ParseCreators(json_IMBD_next_data)
                    If lstCreators IsNot Nothing Then
                        nTVShow.Creators = lstCreators
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Creators", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Genres
                If filteredoptions.bMainGenres Then
                    Dim lstGenres = ParseGenres(json_IMBD_next_data)
                    If lstGenres IsNot Nothing Then
                        nTVShow.Genres = lstGenres
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Genres", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Plot
                If filteredoptions.bMainPlot AndAlso bIsScraperLanguage Then
                    Dim strPlot = ParsePlot(json_IMBD_next_data)
                    If Not String.IsNullOrEmpty(strPlot) Then
                        nTVShow.Plot = strPlot
                    Else
                        strPlot = ParseOutline(json_IMBD_next_data)

                        If Not String.IsNullOrEmpty(strPlot) Then
                            nTVShow.Plot = strPlot
                        End If
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Poster for search result
                If getposter Then
                    ParsePosterURL(json_IMBD_next_data)
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Premiered
                If filteredoptions.bMainPremiered Then
                    If json_IMBD_next_data.props.PageProps.MainColumnData.ReleaseDate IsNot Nothing Then
                        nTVShow.Premiered = json_IMBD_next_data.props.PageProps.MainColumnData.ReleaseDate.GetFullReleaseDate()
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Rating
                If filteredoptions.bMainRating Then
                    Dim nRating = ParseRating(json_IMBD_next_data)
                    If nRating IsNot Nothing Then
                        nTVShow.Ratings.Add(nRating)
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Rating", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Runtime
                If filteredoptions.bMainRuntime Then
                    Dim strRuntime = ParseRuntime(json_IMBD_next_data)
                    If strRuntime IsNot Nothing Then
                        nTVShow.Runtime = strRuntime
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Runtime", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                'Studios
                If filteredoptions.bMainStudios Then
                    Dim lstStudios = ParseStudios(json_IMBD_next_data)
                    If lstStudios IsNot Nothing Then
                        nTVShow.Studios = lstStudios
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Studios", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                If scrapemodifier.withEpisodes OrElse scrapemodifier.withSeasons Then
                    'Seasons and Episodes
                    Dim htmldSeasonsAndEpisodes As HtmlDocument = webParsing.Load(String.Concat("https://www.imdb.com/title/", id, "/episodes/"))

                    If webParsing.StatusCode <> 200 Then
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] failed to retrieve imdb episodes page", id))
                        'Do nothing
                    Else
                        'Get our React JSON next_data
                        json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldSeasonsAndEpisodes.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

                        If json_IMBD_next_data.props.PageProps.ContentData IsNot Nothing Then
                            Dim lstSeasons As New List(Of Integer)
                            Dim SeasonItems As List(Of Seasons) = json_IMBD_next_data.props.PageProps.ContentData.Section.Seasons

                            For Each Season In SeasonItems
                                lstSeasons.Add(Season.ValueAsInteger)
                            Next

                            For Each tSeason In lstSeasons
                                If bwIMDB.CancellationPending Then Return Nothing

                                GetTVSeasonInfo(nTVShow, nTVShow.UniqueIDs.IMDbId, tSeason, scrapemodifier, filteredoptions)

                                If scrapemodifier.withSeasons Then
                                    nTVShow.KnownSeasons.Add(New MediaContainers.SeasonDetails With {.Season = tSeason})
                                End If
                            Next
                        End If

                    End If
                End If

                Return nTVShow
            Else
                Return Nothing
            End If

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    Public Function GetMovieStudios(ByVal id As String) As List(Of String)
        Dim webParsingSeasons As New HtmlWeb
        Dim htmldReference As HtmlDocument = webParsingSeasons.Load(String.Concat("http://www.imdb.com/title/", id, "/reference"))

        If webParsingSeasons.StatusCode <> 200 Then
            logger.Trace(String.Format("[IMDB] [GetMovieStudios] [ID:""{0}""] failed to retrieve imdb reference page", id))
        Else
            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldReference.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then
                Dim lstStudios = ParseStudios(json_IMBD_next_data)

                If lstStudios IsNot Nothing Then
                    Return lstStudios
                End If
            End If
        End If

        Return New List(Of String)
    End Function

    Public Function GetSearchMovieInfo(ByVal title As String,
                                           ByVal year As String,
                                           ByRef oDBElement As Database.DBElement,
                                           ByVal scrapetype As Enums.ScrapeType,
                                           ByVal filteredoptions As Structures.ScrapeOptions) As MediaContainers.Movie
        Dim r As SearchResults_Movie = SearchMovie(title, year)

        Try
            Select Case scrapetype
                Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                    If r.ExactMatches.Count = 1 Then
                        Return GetMovieInfo(r.ExactMatches.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.PopularTitles.Count = 1 AndAlso r.PopularTitles(0).Lev <= 5 Then
                        Return GetMovieInfo(r.PopularTitles.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.ExactMatches.Count = 1 AndAlso r.ExactMatches(0).Lev <= 5 Then
                        Return GetMovieInfo(r.ExactMatches.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    Else
                        Using dlgSearch As New dlgIMDBSearchResults_Movie(_SpecialSettings, Me)
                            If dlgSearch.ShowDialog(r, title, oDBElement.Filename) = DialogResult.OK Then
                                If Not String.IsNullOrEmpty(dlgSearch.Result.UniqueIDs.IMDbId) Then
                                    Return GetMovieInfo(dlgSearch.Result.UniqueIDs.IMDbId, False, filteredoptions)
                                End If
                            End If
                        End Using
                    End If

                Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                    If r.ExactMatches.Count = 1 Then
                        Return GetMovieInfo(r.ExactMatches.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    End If

                Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                    'check if ALL results are over lev value
                    Dim useAnyway As Boolean = False
                    If ((r.PopularTitles.Count > 0 AndAlso r.PopularTitles(0).Lev > 5) OrElse r.PopularTitles.Count = 0) AndAlso
                        ((r.ExactMatches.Count > 0 AndAlso r.ExactMatches(0).Lev > 5) OrElse r.ExactMatches.Count = 0) AndAlso
                        ((r.PartialMatches.Count > 0 AndAlso r.PartialMatches(0).Lev > 5) OrElse r.PartialMatches.Count = 0) Then
                        useAnyway = True
                    End If
                    Dim exactHaveYear As Integer = FindYear(oDBElement.Filename, r.ExactMatches)
                    Dim popularHaveYear As Integer = FindYear(oDBElement.Filename, r.PopularTitles)
                    If r.ExactMatches.Count = 1 Then
                        Return GetMovieInfo(r.ExactMatches.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.ExactMatches.Count > 1 AndAlso exactHaveYear >= 0 Then
                        Return GetMovieInfo(r.ExactMatches.Item(exactHaveYear).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.PopularTitles.Count > 0 AndAlso popularHaveYear >= 0 Then
                        Return GetMovieInfo(r.PopularTitles.Item(popularHaveYear).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.ExactMatches.Count > 0 AndAlso (r.ExactMatches(0).Lev <= 5 OrElse useAnyway) Then
                        Return GetMovieInfo(r.ExactMatches.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.PopularTitles.Count > 0 AndAlso (r.PopularTitles(0).Lev <= 5 OrElse useAnyway) Then
                        Return GetMovieInfo(r.PopularTitles.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    ElseIf r.PartialMatches.Count > 0 AndAlso (r.PartialMatches(0).Lev <= 5 OrElse useAnyway) Then
                        Return GetMovieInfo(r.PartialMatches.Item(0).UniqueIDs.IMDbId, False, filteredoptions)
                    End If
            End Select

            Return Nothing
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    Public Sub GetSearchMovieInfoAsync(ByVal imdbID As String, ByVal FilteredOptions As Structures.ScrapeOptions)
        Try
            If Not bwIMDB.IsBusy Then
                bwIMDB.WorkerReportsProgress = False
                bwIMDB.WorkerSupportsCancellation = True
                bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_Movie,
                                           .Parameter = imdbID, .Options_Movie = FilteredOptions})
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Function GetSearchTVShowInfo(ByVal title As String, ByRef oDBElement As Database.DBElement, ByVal scrapetype As Enums.ScrapeType, ByVal scrapemodifier As Structures.ScrapeModifiers, ByVal FilteredOptions As Structures.ScrapeOptions) As MediaContainers.TVShow
        Dim r As SearchResults_TVShow = SearchTVShow(title)

        Select Case scrapetype
            Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                If r.Matches.Count = 1 Then
                    Return GetTVShowInfo(r.Matches.Item(0).UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                Else
                    Using dlgSearch As New dlgIMDBSearchResults_TV(_SpecialSettings, Me)
                        If dlgSearch.ShowDialog(r, title, oDBElement.ShowPath) = DialogResult.OK Then
                            If Not String.IsNullOrEmpty(dlgSearch.Result.UniqueIDs.IMDbId) Then
                                Return GetTVShowInfo(dlgSearch.Result.UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                            End If
                        End If
                    End Using
                End If

            Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                If r.Matches.Count = 1 Then
                    Return GetTVShowInfo(r.Matches.Item(0).UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                End If

            Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                If r.Matches.Count > 0 Then
                    Return GetTVShowInfo(r.Matches.Item(0).UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                End If
        End Select

        Return Nothing
    End Function

    Public Sub GetSearchTVShowInfoAsync(ByVal id As String, ByVal options As Structures.ScrapeOptions)
        Try
            If Not bwIMDB.IsBusy Then
                bwIMDB.WorkerReportsProgress = False
                bwIMDB.WorkerSupportsCancellation = True
                bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_TVShow,
                                           .Parameter = id, .Options_TV = options})
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Function ParseActors(ByRef json_data As IMDBJson) As List(Of MediaContainers.Person)
        Dim nActors As New List(Of MediaContainers.Person)
        Dim CreditCategoriesList As List(Of Category) = json_data.props.PageProps.MainColumnData.Categories

        If CreditCategoriesList Is Nothing Then Return Nothing

        For Each CreditCategory In CreditCategoriesList
            If CreditCategory.Id IsNot Nothing AndAlso CreditCategory.Section IsNot Nothing Then
                If String.Equals(CreditCategory.Id, "cast", StringComparison.OrdinalIgnoreCase) Then
                    'Loop through all creditconnection edges wich contains cast members
                    For Each json_section_item As CategoryItem In CreditCategory.Section.Items
                        Dim nActor As New MediaContainers.Person
                        Dim ndName = json_section_item.RowTitle
                        Dim ndThumb As String = Nothing
                        Dim ndCharacter As String = String.Empty

                        'Get the thumb some cast members don't have a thumb image
                        If json_section_item.ImageProps IsNot Nothing AndAlso json_section_item.ImageProps.ImageModel IsNot Nothing Then
                            ndThumb = json_section_item.ImageProps.ImageModel.Url
                        End If

                        If json_section_item.Characters IsNot Nothing Then
                            'Loop the character/role a cast member plays
                            If json_section_item.Characters.Count = 1 Then
                                ndCharacter = json_section_item.Characters(0)
                            Else
                                'Actor with multiple roles so we combine them
                                ndCharacter = String.Join(" / ", json_section_item.Characters)
                            End If
                        End If

                        'Append attributes (e.g. (voice), (credit-only), ... ) if available
                        If json_section_item.attributes IsNot Nothing Then
                            ndCharacter = ndCharacter + " " + json_section_item.attributes
                        End If

                        nActor.Name = ndName
                        nActor.Role = ndCharacter

                        If ndThumb IsNot Nothing Then
                            nActor.Thumb.URLOriginal = ndThumb
                        End If

                        nActors.Add(nActor)
                    Next
                End If
            End If
        Next

        Return If(nActors.Count > 0, nActors, Nothing)
    End Function

    Private Function ParseCertifications(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.MainColumnData.Certificates IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Certificates.Edges IsNot Nothing Then
            Dim lstCertifications As New List(Of String)
            Dim Certifications As List(Of CertificatesEdge) = json_data.props.PageProps.MainColumnData.Certificates.Edges

            If Certifications Is Nothing Then Return Nothing

            For Each certificate In Certifications
                If certificate.Node IsNot Nothing Then
                    lstCertifications.Add(certificate.Node.rating)
                End If
            Next

            lstCertifications = lstCertifications.Distinct.ToList
            lstCertifications.Sort()

            Return lstCertifications
        End If

        Return Nothing
    End Function

    Private Function ParseCountries(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.MainColumnData.CountriesDetails IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.CountriesDetails.Countries IsNot Nothing Then
            Dim Countries As List(Of CountryOfOrigin)
            Dim nCountries As New List(Of String)

            Countries = json_data.props.PageProps.MainColumnData.CountriesDetails.Countries

            If Countries IsNot Nothing Then
                For Each nCountry In Countries
                    nCountries.Add(nCountry.text)
                Next

                Return nCountries
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseCreators(ByRef json_data As IMDBJson) As List(Of String)
        Dim Creators As List(Of CreatorPrincipalCreditsForCategory)
        Dim nCreators As New List(Of String)

        If json_data.props.PageProps.MainColumnData.CreatorsPageTitle IsNot Nothing Then
            Creators = json_data.props.PageProps.MainColumnData.CreatorsPageTitle

            For Each nCreator In Creators
                For Each nCredit In nCreator.Credits
                    nCreators.Add(nCredit.Name.NameText.Text)
                Next
            Next

            If nCreators IsNot Nothing Then
                Return nCreators
            End If
        End If

        Return Nothing

    End Function

    Private Function ParseCredits(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.MainColumnData.Categories IsNot Nothing Then
            Dim nCredits As New List(Of String)
            Dim CreditCategoriesList As List(Of Category) = json_data.props.PageProps.MainColumnData.Categories

            If CreditCategoriesList IsNot Nothing Then
                For Each CreditCategory In CreditCategoriesList
                    If CreditCategory.Id IsNot Nothing AndAlso CreditCategory.Section IsNot Nothing Then
                        If String.Equals(CreditCategory.Id, "writer", StringComparison.OrdinalIgnoreCase) Then
                            'Loop all creditconnection edges wich contains writer members

                            For Each json_section_item As CategoryItem In CreditCategory.Section.Items
                                nCredits.Add(json_section_item.RowTitle)
                            Next

                            Return nCredits
                        End If
                    End If
                Next
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseDirectors(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.AboveTheFoldData.DirectorsPageTitle IsNot Nothing Then
            Dim DirectorCrewList As List(Of PrincipalCreditsForCategory)
            Dim nDirectors As New List(Of String)

            DirectorCrewList = json_data.props.PageProps.AboveTheFoldData.DirectorsPageTitle

            If DirectorCrewList IsNot Nothing Then
                For Each nDirector In DirectorCrewList
                    nDirectors.Add(nDirector.Credits(0).Name.NameText.Text)
                Next

                Return nDirectors
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseDuration(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Runtime IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Runtime.DisplayableProperty IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Runtime.DisplayableProperty.Value IsNot Nothing Then
            Return json_data.props.PageProps.MainColumnData.Runtime.DisplayableProperty.Value.PlainText
        End If

        Return Nothing
    End Function

    Private Function ParseGenres(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.AboveTheFoldData.TitleGenres IsNot Nothing AndAlso json_data.props.PageProps.AboveTheFoldData.TitleGenres.Genres IsNot Nothing Then
            Dim TitleGenresList As List(Of TitleGenre)
            Dim nGenres As New List(Of String)

            If json_data.props.PageProps.AboveTheFoldData.TitleGenres IsNot Nothing Then
                TitleGenresList = json_data.props.PageProps.AboveTheFoldData.TitleGenres.Genres

                If TitleGenresList IsNot Nothing Then
                    For Each nGenre In TitleGenresList
                        nGenres.Add(nGenre.Genre.Text)
                    Next

                    Return nGenres
                End If
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseMPAA(ByRef json_data As IMDBJson, id As String) As String
        'Try to get the full MPAA
        If _SpecialSettings.MPAADescription Then
            If json_data.props.PageProps.MainColumnData.Certificates IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Certificates.Edges IsNot Nothing Then
                Dim Certifications As List(Of CertificatesEdge) = json_data.props.PageProps.MainColumnData.Certificates.Edges

                If Certifications Is Nothing Then Return Nothing

                For Each certificate In Certifications
                    If certificate.Node IsNot Nothing AndAlso certificate.Node.ratingsBody IsNot Nothing Then
                        If certificate.Node.ratingsBody.id = "MPAA" Then
                            Return certificate.Node.ratingReason
                        End If
                    End If
                Next
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseOutline(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.AboveTheFoldData.Plot IsNot Nothing AndAlso json_data.props.PageProps.AboveTheFoldData.Plot.PlotText IsNot Nothing Then
            Dim strOutline = json_data.props.PageProps.AboveTheFoldData.Plot.PlotText.PlainText

            If Not String.IsNullOrEmpty(strOutline) AndAlso Not strOutline.ToLower = "know what this is about?" Then
                Return strOutline
            End If
        End If

        Return Nothing
    End Function

    Private Function ParsePlot(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Synopses IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Synopses.Edges IsNot Nothing Then
            Dim ndPlotEdges = json_data.props.PageProps.MainColumnData.Synopses.Edges

            For Each ndPlot In ndPlotEdges
                If ndPlot IsNot Nothing Then
                    Return ndPlot.Node.PlotText.PlainText
                End If
            Next
        End If

        Return Nothing
    End Function

    Private Sub ParsePosterURL(ByRef json_data As IMDBJson)
        If json_data.props.PageProps.MainColumnData.PrimaryImage IsNot Nothing Then
            strPosterURL = json_data.props.PageProps.MainColumnData.PrimaryImage.Url
        End If
    End Sub

    Private Function ParseRating(ByRef json_data As IMDBJson) As MediaContainers.RatingDetails
        If json_data.props.PageProps.AboveTheFoldData.RatingsSummary IsNot Nothing Then
            Return New MediaContainers.RatingDetails With {
                .Max = 10,
                .Type = "imdb",
                .Value = json_data.props.PageProps.AboveTheFoldData.RatingsSummary.GetAggregateRating,
                .Votes = json_data.props.PageProps.AboveTheFoldData.RatingsSummary.voteCount
            }
        End If

        Return Nothing
    End Function

    Private Function ParseRuntime(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Runtime IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Runtime.DisplayableProperty IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Runtime.DisplayableProperty.Value IsNot Nothing Then
            Return json_data.props.PageProps.MainColumnData.Runtime.DisplayableProperty.Value.PlainText
        End If

        Return Nothing
    End Function

    Private Function ParseStudios(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.MainColumnData.Production IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Production.Edges IsNot Nothing Then
            Dim nCompanies As New List(Of String)
            Dim CompanyCredits As List(Of CompanyCreditEdge) = json_data.props.PageProps.MainColumnData.Production.Edges

            If CompanyCredits IsNot Nothing Then
                For Each CompanyCredit In CompanyCredits
                    nCompanies.Add(CompanyCredit.Node.company.CompanyText.Text)
                Next

                Return nCompanies
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseTagline(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Taglines IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Taglines.Edges IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Taglines.Edges.Count > 0 Then
            Return json_data.props.PageProps.MainColumnData.Taglines.Edges(0).Node.Text
        End If

        Return Nothing
    End Function

    Private Function SearchMovie(ByVal title As String, ByVal year As String) As SearchResults_Movie
        Dim R As New SearchResults_Movie

        Dim strTitle As String = String.Concat(title, " ", If(Not String.IsNullOrEmpty(year), String.Concat("(", year, ")"), String.Empty)).Trim

        Dim htmldResultsPartialTitles As HtmlDocument = Nothing
        Dim htmldResultsPopularTitles As HtmlDocument = Nothing
        Dim htmldResultsShortTitles As HtmlDocument = Nothing
        Dim htmldResultsTvTitles As HtmlDocument = Nothing
        Dim htmldResultsVideoTitles As HtmlDocument = Nothing

        Dim webParsing As New HtmlWeb
        Dim htmldResultsExact As HtmlDocument = webParsing.Load(String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&s=ttexact=true"))
        Dim strResponseUri = webParsing.ResponseUri.ToString

        If _SpecialSettings.SearchTvTitles Then
            htmldResultsTvTitles = webParsing.Load(String.Concat("https://www.imdb.com/search/title/?title=", HttpUtility.UrlEncode(strTitle), "&title_type=tv_movie"))
        End If
        If _SpecialSettings.SearchVideoTitles Then
            htmldResultsVideoTitles = webParsing.Load(String.Concat("https://www.imdb.com/search/title/?title=", HttpUtility.UrlEncode(strTitle), "&title_type=video"))
        End If
        If _SpecialSettings.SearchShortTitles Then
            htmldResultsShortTitles = webParsing.Load(String.Concat("https://www.imdb.com/search/title/?title=", HttpUtility.UrlEncode(strTitle), "&title_type=short"))
        End If
        If _SpecialSettings.SearchPartialTitles Then
            htmldResultsPartialTitles = webParsing.Load(String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&s=tt&ttype=ft&ref_=fn_ft"))
        End If
        If _SpecialSettings.SearchPopularTitles Then
            htmldResultsPopularTitles = webParsing.Load(String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&s=tt"))
        End If

        If webParsing.StatusCode <> 200 Then
            logger.Trace(String.Format("[IMDB] [SearchMovie] failed to retrieve imdb search pages"))
        End If

        'Check if we've been redirected straight to the movie page
        If Regex.IsMatch(strResponseUri, REGEX_IMDBID) Then
            Return R
        End If

        'Exact titles
        If htmldResultsExact IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsExact.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.PopularTitles.Add(New MediaContainers.Movie With {
                                    .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.titleNameText),
                                    .Title = nResult.titleNameText,
                                    .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.id},
                                    .Year = nResult.titleReleaseText
                                    })
                Next
            End If
        End If

        'Popular titles
        If htmldResultsPopularTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsPopularTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.PopularTitles.Add(New MediaContainers.Movie With {
                                        .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.titleNameText),
                                        .Title = nResult.titleNameText,
                                        .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.id},
                                        .Year = nResult.titleReleaseText
                                        })
                Next
            End If
        End If

        'Partial titles
        If htmldResultsPartialTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsPartialTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.PartialMatches.Add(New MediaContainers.Movie With {
                                         .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.titleNameText),
                                         .Title = nResult.titleNameText,
                                         .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.id},
                                         .Year = nResult.titleReleaseText
                                         })
                Next
            End If

        End If

        'tv titles
        If htmldResultsTvTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsTvTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.TvTitles.Add(New MediaContainers.Movie With {
                                         .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.titleNameText),
                                         .Title = nResult.titleNameText,
                                         .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.id},
                                         .Year = nResult.titleReleaseText
                                   })
                Next
            End If
        End If

        'video titles
        If htmldResultsVideoTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsVideoTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.VideoTitles.Add(New MediaContainers.Movie With {
                                         .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.titleNameText),
                                         .Title = nResult.titleNameText,
                                         .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.id},
                                         .Year = nResult.titleReleaseText
                                   })
                Next
            End If
        End If

        'short titles
        If htmldResultsShortTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsShortTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.ShortTitles.Add(New MediaContainers.Movie With {
                                         .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.titleNameText),
                                         .Title = nResult.titleNameText,
                                         .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.id},
                                         .Year = nResult.titleReleaseText
                                   })
                Next
            End If
        End If

        Return R
    End Function

    Public Sub SearchMovieAsync(ByVal title As String, ByVal year As String, ByVal filteredoptions As Structures.ScrapeOptions)
        Try
            If Not bwIMDB.IsBusy Then
                bwIMDB.WorkerReportsProgress = False
                bwIMDB.WorkerSupportsCancellation = True
                bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.Movies, .Parameter = title, .Year = year, .Options_Movie = filteredoptions})
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Function SearchTVShow(ByVal title As String) As SearchResults_TVShow
        Dim R As New SearchResults_TVShow

        Dim webParsing As New HtmlWeb
        ' Dim htmldSearchResults As HtmlDocument = webParsing.Load(String.Concat("https://www.imdb.com/search/title/?title=", HttpUtility.UrlEncode(title), "&title_type=tv_series"))
        Dim htmldSearchResults As HtmlDocument = webParsing.Load(String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(title), "&s=tt&ttype=tv"))

        If webParsing.StatusCode <> 200 Then
            logger.Trace(String.Format("[IMDB] [SearchTVShow] failed to retrieve imdb find page"))
            'Do nothing
        Else
            If htmldSearchResults IsNot Nothing Then
                json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldSearchResults.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)
            End If

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    R.Matches.Add(New MediaContainers.TVShow With {
                                          .Title = nResult.titleNameText,
                                          .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVShow) With {.IMDbId = nResult.id}
                                          })
                Next
            End If
        End If

        Return R
    End Function

    Public Sub SearchTVShowAsync(ByVal title As String, ByVal scrapemodifiers As Structures.ScrapeModifiers, ByVal filteredoptions As Structures.ScrapeOptions)

        If Not bwIMDB.IsBusy Then
            bwIMDB.WorkerReportsProgress = False
            bwIMDB.WorkerSupportsCancellation = True
            bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.TVShows,
                  .Parameter = title, .Options_TV = filteredoptions, .ScrapeModifiers = scrapemodifiers})
        End If
    End Sub

#End Region 'Methods

#Region "Nested Types"

    Private Structure Arguments

#Region "Fields"

        Dim FullCast As Boolean
        Dim FullCrew As Boolean
        Dim Options_Movie As Structures.ScrapeOptions
        Dim Options_TV As Structures.ScrapeOptions
        Dim Parameter As String
        Dim ScrapeModifiers As Structures.ScrapeModifiers
        Dim Search As SearchType
        Dim Year As String

#End Region 'Fields

    End Structure

    Private Structure Results

#Region "Fields"

        Dim Result As Object
        Dim ResultType As SearchType

#End Region 'Fields

    End Structure

#End Region 'Nested Types

End Class