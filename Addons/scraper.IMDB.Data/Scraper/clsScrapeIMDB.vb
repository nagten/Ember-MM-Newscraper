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

Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Web
Imports EmberAPI
Imports HtmlAgilityPack
Imports Microsoft.Win32
Imports Newtonsoft.Json
Imports NLog
Imports OpenQA.Selenium
Imports OpenQA.Selenium.Chrome
Imports OpenQA.Selenium.Edge
Imports OpenQA.Selenium.Firefox
Imports OpenQA.Selenium.Support.UI

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
    Private strCookieHeaders As String = String.Empty
    Private strUserAgent As String = String.Empty
    Private onAssemblyResolveEventHandler As ResolveEventHandler
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

            If strCookieHeaders Is String.Empty Then
                RetrieveIMDBcookies(strCookieHeaders)
            End If

            Dim htmldReference = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", id, "/reference/"))

            If htmldReference Is Nothing Then
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
                    Dim strRuntime = ParseRuntime(json_IMBD_next_data)
                    If strRuntime IsNot Nothing Then
                        nMovie.Runtime = strRuntime
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
                    If json_IMBD_next_data.props.PageProps.MainColumnData.RatingsSummary.topRanking.rank <= 250 Then
                        nMovie.Top250 = json_IMBD_next_data.props.PageProps.MainColumnData.RatingsSummary.topRanking.rank
                    End If
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

            If strCookieHeaders Is String.Empty Then
                RetrieveIMDBcookies(strCookieHeaders)
            End If

            Dim htmldReference = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", id, "/reference/"))

            If htmldReference Is Nothing Then
                logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] failed to retrieve imdb reference page", id))
                Return Nothing
            End If

            'Get our React JSON next_data
            json_IMBD_next_data = DeserializeJsonObject(Of IMDBJson)(htmldReference.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMBD_next_data IsNot Nothing Then

                'Get season and episode number
                If json_IMBD_next_data.props.PageProps.MainColumnData.Series IsNot Nothing AndAlso json_IMBD_next_data.props.PageProps.MainColumnData.Series.episodeNumber IsNot Nothing Then
                    nTVEpisode.Episode = json_IMBD_next_data.props.PageProps.MainColumnData.Series.episodeNumber.episodeNumber
                    nTVEpisode.Season = json_IMBD_next_data.props.PageProps.MainColumnData.Series.episodeNumber.seasonNumber
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] can't parse Episode number", id))
                End If

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

        If strCookieHeaders Is String.Empty Then
            RetrieveIMDBcookies(strCookieHeaders)
        End If

        Dim htmldEpisodes = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", showid, "/episodes/?season=", season))

        If htmldEpisodes Is Nothing Then
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

        If strCookieHeaders Is String.Empty Then
            RetrieveIMDBcookies(strCookieHeaders)
        End If

        Dim htmldEpisodes = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", showid, "/episodes/?season=", season))

        If htmldEpisodes Is Nothing Then
            logger.Trace(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] failed to retrieve imdb episode/seasons page", showid))
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

            If strCookieHeaders Is String.Empty Then
                RetrieveIMDBcookies(strCookieHeaders)
            End If

            Dim htmldReference = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", id, "/reference/"))

            If htmldReference Is Nothing Then
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

                'Tagline
                If filteredoptions.bMainTagline AndAlso bIsScraperLanguage Then
                    Dim strTagline = ParseTagline(json_IMBD_next_data)
                    If strTagline IsNot Nothing Then
                        nTVShow.Tagline = strTagline
                    Else
                        logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Tagline", id))
                    End If
                End If

                If bwIMDB.CancellationPending Then Return Nothing

                If scrapemodifier.withEpisodes OrElse scrapemodifier.withSeasons Then
                    'Seasons and Episodes

                    If strCookieHeaders Is String.Empty Then
                        RetrieveIMDBcookies(strCookieHeaders)
                    End If

                    Dim htmldSeasonsAndEpisodes = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", id, "/episodes/"))

                    If htmldSeasonsAndEpisodes Is Nothing Then
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
        If strCookieHeaders Is String.Empty Then
            RetrieveIMDBcookies(strCookieHeaders)
        End If

        Dim htmldReference = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/title/", id, "/reference/"))

        If htmldReference Is Nothing Then
            logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] failed to retrieve imdb reference page", id))
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
                If String.Equals(CreditCategory.Name, "cast", StringComparison.OrdinalIgnoreCase) Or String.Equals(CreditCategory.Id, "amzn1.imdb.concept.name_credit_group.7caf7d16-5db9-4f4f-8864-d4c6e711c686", StringComparison.OrdinalIgnoreCase) Then
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
                    Dim ConvertedCountry As String = certificate.Node.country.text.Trim().Replace("United Kingdom", "UK") _
                                       .Replace("United States", "USA") _
                                       .Replace("West", "")

                    lstCertifications.Add(ConvertedCountry.Trim() & ":" & certificate.Node.rating.Trim())
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
        Dim PrincipalCredit As List(Of PrincipalCreditsForGrouping)
        Dim nCreators As New List(Of String)

        'Creators for series is only stored in principalCreditsV2 grouped as "Creator", it's not present in MainColumnData.Categories
        If json_data.props.PageProps.MainColumnData.principalCreditsV2 IsNot Nothing Then
            PrincipalCredit = json_data.props.PageProps.MainColumnData.principalCreditsV2

            For Each nCreator In PrincipalCredit
                If nCreator.grouping IsNot Nothing AndAlso nCreator.credits IsNot Nothing Then
                    If String.Equals(nCreator.grouping.Text, "creator", StringComparison.OrdinalIgnoreCase) Or String.Equals(nCreator.grouping.Text, "creators", StringComparison.OrdinalIgnoreCase) Or String.Equals(nCreator.grouping.groupingId, "amzn1.imdb.concept.name_credit_group.85198717-6c2d-481e-93a5-47858774bcce", StringComparison.OrdinalIgnoreCase) Then
                        For Each nCredit In nCreator.credits
                            nCreators.Add(nCredit.name.NameText.Text)
                        Next
                    End If
                End If
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
                        If String.Equals(CreditCategory.Name, "writer", StringComparison.OrdinalIgnoreCase) Or String.Equals(CreditCategory.Name, "writers", StringComparison.OrdinalIgnoreCase) Or String.Equals(CreditCategory.Id, "amzn1.imdb.concept.name_credit_category.c84ecaff-add5-4f2e-81db-102a41881fe3", StringComparison.OrdinalIgnoreCase) Then
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
        If json_data.props.PageProps.MainColumnData.Categories IsNot Nothing Then
            Dim nDirectors As New List(Of String)
            Dim CreditCategoriesList As List(Of Category) = json_data.props.PageProps.MainColumnData.Categories

            If CreditCategoriesList IsNot Nothing Then
                For Each CreditCategory In CreditCategoriesList
                    If CreditCategory.Id IsNot Nothing AndAlso CreditCategory.Section IsNot Nothing Then
                        If String.Equals(CreditCategory.Name, "director", StringComparison.OrdinalIgnoreCase) Or String.Equals(CreditCategory.Name, "directors", StringComparison.OrdinalIgnoreCase) Or String.Equals(CreditCategory.Id, "amzn1.imdb.concept.name_credit_category.ace5cb4c-8708-4238-9542-04641e7c8171", StringComparison.OrdinalIgnoreCase) Then
                            'Loop all creditconnection edges wich contains director members

                            For Each json_section_item As CategoryItem In CreditCategory.Section.Items
                                nDirectors.Add(json_section_item.RowTitle)
                            Next

                            Return nDirectors
                        End If
                    End If
                Next
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseGenres(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.MainColumnData.Genres IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Genres.Genres IsNot Nothing Then
            Dim GenresList As List(Of Genre)
            Dim nGenres As New List(Of String)

            If json_data.props.PageProps.MainColumnData.Genres IsNot Nothing Then
                GenresList = json_data.props.PageProps.MainColumnData.Genres.Genres

                If GenresList IsNot Nothing Then
                    For Each nGenre In GenresList
                        nGenres.Add(nGenre.Text)
                    Next

                    Return nGenres
                End If
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseMPAA(ByRef json_data As IMDBJson, id As String) As String
        'Try to get the full MPAA from MainColumnData.Certificates
        If json_data.props.PageProps.MainColumnData.Certificates IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Certificates.Edges IsNot Nothing Then
            Dim Certifications As List(Of CertificatesEdge) = json_data.props.PageProps.MainColumnData.Certificates.Edges

            If Certifications Is Nothing Then Return Nothing

            For Each certificate In Certifications
                If certificate.Node IsNot Nothing AndAlso certificate.Node.ratingsBody IsNot Nothing Then
                    If certificate.Node.ratingsBody.id.ToUpper = "MPAA" Then
                        If _SpecialSettings.MPAADescription Then
                            Return certificate.Node.ratingReason.Trim()
                        Else
                            Return String.Format("Rated {0}", certificate.Node.rating.Trim())
                        End If
                    End If
                End If
            Next
        End If

        If _SpecialSettings.MPAADescription Then logger.Trace(String.Format("[IMDB] [ParseMPAA] [ID:""{0}""] can't parse full MPAA or MPAA description, try to parse the short rating", id))

        If json_data.props.PageProps.MainColumnData.Certificate IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Certificate.rating IsNot Nothing Then
            Dim allowedRatings As String() = {"G", "PG", "PG-13", "R", "NC-17"}

            If allowedRatings.Contains(json_data.props.PageProps.MainColumnData.Certificate.rating.ToUpper) Then
                Return String.Format("Rated {0}", json_data.props.PageProps.MainColumnData.Certificate.rating)
            Else
                Return "NR"
            End If
        End If

        Return Nothing
    End Function

    Private Function ParseOutline(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Outlines IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Outlines.Edges IsNot Nothing Then
            Dim ndPlotEdges = json_data.props.PageProps.MainColumnData.Outlines.Edges

            For Each ndPlot In ndPlotEdges
                'Only return the first outline
                If ndPlot IsNot Nothing AndAlso ndPlot.Node IsNot Nothing AndAlso ndPlot.Node.PlotText IsNot Nothing Then
                    Return ndPlot.Node.PlotText.plaidHtml
                End If
            Next
        End If

        Return Nothing
    End Function

    Private Function ParsePlot(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Plot IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Plot.PlotText IsNot Nothing Then
            Dim strPlot = json_data.props.PageProps.MainColumnData.Plot.PlotText.PlainText

            If Not String.IsNullOrEmpty(strPlot) AndAlso Not strPlot.ToLower = "know what this is about?" Then
                Return strPlot
            End If
        End If

        Return Nothing
    End Function

    Private Sub ParsePosterURL(ByRef json_data As IMDBJson)
        If json_data.props.PageProps.MainColumnData.PrimaryImage IsNot Nothing Then
            strPosterURL = json_data.props.PageProps.MainColumnData.PrimaryImage.Url
        End If
    End Sub

    Private Function ParseRating(ByRef json_data As IMDBJson) As MediaContainers.RatingDetails
        If json_data.props.PageProps.MainColumnData.RatingsSummary IsNot Nothing Then
            Return New MediaContainers.RatingDetails With {
                .Max = 10,
                .Type = "imdb",
                .Value = json_data.props.PageProps.MainColumnData.RatingsSummary.GetAggregateRating,
                .Votes = json_data.props.PageProps.MainColumnData.RatingsSummary.voteCount
            }
        End If

        Return Nothing
    End Function

    Private Function ParseRuntime(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Runtime IsNot Nothing Then
            'Nfo file expects minutes
            Return Math.Round((json_data.props.PageProps.MainColumnData.Runtime.Seconds / 60), 0).ToString
        End If

        Return Nothing
    End Function

    Private Function ParseStudios(ByRef json_data As IMDBJson) As List(Of String)
        If json_data.props.PageProps.MainColumnData.CompanyCreditCategories IsNot Nothing Then
            Dim CompanyCreditsCategories As List(Of CompanyCreditCategoryWithCompanyCredits) = json_data.props.PageProps.MainColumnData.CompanyCreditCategories
            Dim nCompanies As New List(Of String)

            For Each CompanyCreditCategory In CompanyCreditsCategories
                If CompanyCreditCategory.Category IsNot Nothing Then
                    If String.Equals(CompanyCreditCategory.Category.Id, "production", StringComparison.OrdinalIgnoreCase) Then
                        If CompanyCreditCategory.CompanyCredits IsNot Nothing AndAlso CompanyCreditCategory.CompanyCredits.Edges IsNot Nothing Then
                            Dim CompanyCreditsProduction As List(Of CompanyCreditEdge) = CompanyCreditCategory.CompanyCredits.Edges

                            For Each CompanyCredit In CompanyCreditsProduction
                                nCompanies.Add(CompanyCredit.Node.DisplayableProperty.Value.PlainText)
                            Next
                        End If
                    End If

                    If _SpecialSettings.StudiowithDistributors Then
                        If String.Equals(CompanyCreditCategory.Category.Id, "distribution", StringComparison.OrdinalIgnoreCase) Then
                            If CompanyCreditCategory.CompanyCredits IsNot Nothing AndAlso CompanyCreditCategory.CompanyCredits.Edges IsNot Nothing Then
                                Dim CompanyCreditsDistribution As List(Of CompanyCreditEdge) = CompanyCreditCategory.CompanyCredits.Edges

                                For Each CompanyCredit In CompanyCreditsDistribution
                                    nCompanies.Add(CompanyCredit.Node.DisplayableProperty.Value.PlainText)
                                Next

                            End If
                        End If
                    End If
                End If
            Next

            Return nCompanies.Distinct.ToList
        End If

        Return Nothing
    End Function

    Private Function ParseTagline(ByRef json_data As IMDBJson) As String
        If json_data.props.PageProps.MainColumnData.Taglines IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Taglines.Edges IsNot Nothing AndAlso json_data.props.PageProps.MainColumnData.Taglines.Edges.Count > 0 Then
            Return json_data.props.PageProps.MainColumnData.Taglines.Edges(0).Node.Text
        End If

        Return Nothing
    End Function

    Private Function GetInstalledBrowsers() As List(Of String)
        Dim browsers As New List(Of String)()

        If IsBrowserInstalledRegistry("msedge.exe") Then
            browsers.Add("Edge")
        End If

        If IsBrowserInstalledRegistry("chrome.exe") Then
            browsers.Add("Chrome")
        End If

        If IsBrowserInstalledRegistry("firefox.exe") Then
            browsers.Add("Firefox")
        End If

        Return browsers
    End Function

    Private Function IsBrowserInstalledRegistry(executableName As String) As Boolean
        Dim paths As String() = {
        "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" & executableName,
        "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" & executableName
        }

        For Each path In paths
            Using key = Registry.LocalMachine.OpenSubKey(path)
                If key IsNot Nothing Then Return True
            End Using
        Next

        Return False
    End Function

    Private Sub RetrieveIMDBcookies(ByRef strCookieHeaders As String)
        Dim url As String = "https://www.imdb.com"
        Dim browserSelected As New List(Of String)

        Dim installedBrowser As List(Of String) = GetInstalledBrowsers()

        If installedBrowser.Count = 0 Then
            strCookieHeaders = ""
            logger.Trace(String.Format("[IMDB] [RetrieveIMDBcookies] No supported browser installed (Chrome, Edge, Firefox)"))
            Exit Sub
        Else
            logger.Trace(String.Format("[IMDB] [RetrieveIMDBcookies] following browsers are detected ({0})", String.Join(", ", installedBrowser)))
        End If

        If String.IsNullOrEmpty(_SpecialSettings.BrowserPriority) Then
            'No browser priority preference configured
            browserSelected = installedBrowser
        ElseIf installedBrowser.Contains(_SpecialSettings.BrowserPriority, StringComparer.OrdinalIgnoreCase) Then
            'The priority browser is installed use that one first
            browserSelected.Add(_SpecialSettings.BrowserPriority)
        Else
            'Fallback, try all browsers one by one
            browserSelected = installedBrowser
        End If

        Dim browserDriver As IWebDriver = Nothing

        Try
            browserDriver = CreateWebDriver(browserSelected)

            If browserDriver Is Nothing Then
                strCookieHeaders = ""
                Exit Sub
            End If

            'strCookieHeaders = ExtractCookiesAntiBotTest(browserDriver, url)
            strCookieHeaders = ExtractCookies(browserDriver, url)

        Catch ex As Exception
            strCookieHeaders = ""
            logger.Trace(String.Format("[IMDB] [RetrieveIMDBcookies] Exception: {0}", ex.Message))
        Finally
            If onAssemblyResolveEventHandler IsNot Nothing Then
                RemoveHandler AppDomain.CurrentDomain.AssemblyResolve, onAssemblyResolveEventHandler
            End If

            If browserDriver IsNot Nothing Then browserDriver.Quit()
        End Try
    End Sub

    Private Function ExtractCookiesAntiBotTest(browserDriver As IWebDriver, url As String) As String
        'Antibot test websites used for testing, this function will take a screenshot antibot_result.png and dump pagesource of the antibottest page to antibot_result.html
        url = "https://pixelscan.net/bot-check"
        url = "https://scrapfly.io/web-scraping-tools/automation-detector"
        url = "https://bot.sannysoft.com/"

        browserDriver.Navigate().GoToUrl(url)

        'Wait for page to load for maximum 10 seconds
        Dim wait As New WebDriverWait(browserDriver, TimeSpan.FromSeconds(10))

        wait.Until(Function(driverwait)
                       Dim pageReady As Boolean = CType(driverwait, IJavaScriptExecutor).ExecuteScript("return document.readyState").Equals("complete")
                       Return pageReady
                   End Function)

        Dim screenshot As Screenshot = (CType(browserDriver, ITakesScreenshot)).GetScreenshot()

        screenshot.SaveAsFile(".\antibot_result.png")
        File.WriteAllText(".\antibot_result.html", browserDriver.PageSource)

        Return String.Empty
    End Function

    Private Function ExtractCookies(browserDriver As IWebDriver, url As String) As String
        browserDriver.Navigate().GoToUrl(url)

        'Wait for page to load and cookies for maximum 10 seconds
        Dim wait As New WebDriverWait(browserDriver, TimeSpan.FromSeconds(10))

        wait.Until(Function(driverwait)
                       Dim pageReady As Boolean = CType(driverwait, IJavaScriptExecutor).ExecuteScript("return document.readyState").Equals("complete")
                       Dim cookieExists As Boolean = (driverwait.Manage().Cookies.AllCookies.Count > 0)
                       Return pageReady AndAlso cookieExists
                   End Function)

        Dim cookieHeader As New Text.StringBuilder()

        If browserDriver.Manage().Cookies.AllCookies.Count > 0 Then
            For Each cookie In browserDriver.Manage().Cookies.AllCookies
                cookieHeader.Append($"{cookie.Name}={cookie.Value}; ")
            Next

            'Trim trailing semicolon + space
            Return cookieHeader.ToString().TrimEnd(" "c, ";"c)
        Else
            logger.Trace(String.Format("[IMDB] [ExtractCookies] Failed to retrieve cookies"))
            Return String.Empty
        End If
    End Function

    Private Function CreateAssemblyResolver(unsafeAsm As Assembly) As ResolveEventHandler
        Return Function(sender As Object, e As ResolveEventArgs) As Assembly
                   If e.Name.StartsWith("System.Runtime.CompilerServices", StringComparison.OrdinalIgnoreCase) Then
                       Return unsafeAsm
                   End If

                   For Each asm As Assembly In AppDomain.CurrentDomain.GetAssemblies()
                       If asm.FullName = e.Name Then
                           Return asm
                       End If
                   Next

                   Return Nothing
               End Function
    End Function

    Private Function CreateWebDriver(browserList As List(Of String)) As IWebDriver
        'Disable data collection see https://www.selenium.dev/documentation/selenium_manager/
        Environment.SetEnvironmentVariable("SE_AVOID_STATS", "1")

        'Load the old System.Runtime.CompilerServices.Unsafe assembly version 4.0.4.1
        'Selenium switched in version v4.24.0 from `Newtonsoft.Json` to `System.Text.Json` package (#14292) which causes following assembley load error:
        '"Could not load file or assembly 'System.Runtime.CompilerServices.Unsafe, Version=4.0.4.1, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' or one of its dependencies. The system cannot find the file specified."
        'See https://github.com/SeleniumHQ/selenium/issues/16539 and https://github.com/SeleniumHQ/selenium/pull/16564
        'Below workaround avoids that error by loading an older pinned version
        Dim oldSystemRuntimeCompilerServicesUnsafeAssembly = Assembly.LoadFrom(String.Concat(IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "\System.Runtime.CompilerServices.Unsafe.4.0.4.1.dll"))
        onAssemblyResolveEventHandler = CreateAssemblyResolver(oldSystemRuntimeCompilerServicesUnsafeAssembly)
        AddHandler AppDomain.CurrentDomain.AssemblyResolve, onAssemblyResolveEventHandler

        'No BiDi support in .Net yet to change webdriver options https://github.com/SeleniumHQ/selenium/issues/13993 so we use CDP commands
        For Each browser As String In browserList
            Try
                Select Case browser
                    Case "Edge"
                        Dim browserService = EdgeDriverService.CreateDefaultService()
                        browserService.HideCommandPromptWindow = True

                        Dim browserOptions As New EdgeOptions()
                        browserOptions.AddArgument("--headless=new")
                        browserOptions.AddArgument("--no-sandbox")
                        browserOptions.AddArgument("--disable-blink-features=AutomationControlled")
                        browserOptions.AddArgument("--disable-dev-shm-usage")
                        browserOptions.AddArgument("--timezone=" & TimeZoneInfo.Local.Id)
                        browserOptions.AddExcludedArgument("enable-automation")
                        browserOptions.AddAdditionalOption("useAutomationExtension", False)

                        Dim driver = New EdgeDriver(browserService, browserOptions)

                        'Remove HeadlessChrome from useragent for increased stealth
                        Dim originalUserAgent As String = driver.ExecuteScript("return navigator.userAgent;").ToString()

                        logger.Trace(String.Format("[IMDB] [CreateWebDriver Edge] Original user agent ""{0}""", originalUserAgent))

                        strUserAgent = originalUserAgent.Replace("HeadlessChrome", "Chrome")

                        driver.ExecuteCdpCommand("Network.setUserAgentOverride",
                                         New Dictionary(Of String, Object) From {
                                             {"userAgent", strUserAgent}
                                         })

                        logger.Trace(String.Format("[IMDB] [CreateWebDriver Edge] Modified user agent ""{0}""", strUserAgent))

                        Return driver
                    Case "Chrome"
                        Dim browserService = ChromeDriverService.CreateDefaultService()
                        browserService.HideCommandPromptWindow = True

                        Dim browserOptions As New ChromeOptions()
                        browserOptions.AddArgument("--headless=new")
                        browserOptions.AddArgument("--no-sandbox")
                        browserOptions.AddArgument("--disable-blink-features=AutomationControlled")
                        browserOptions.AddArgument("--disable-dev-shm-usage")
                        browserOptions.AddArgument("--timezone=" & TimeZoneInfo.Local.Id)
                        browserOptions.AddExcludedArgument("enable-automation")
                        browserOptions.AddAdditionalOption("useAutomationExtension", False)

                        Dim driver = New ChromeDriver(browserService, browserOptions)

                        'Remove HeadlessChrome from useragent for increased stealth
                        Dim originalUserAgent As String = driver.ExecuteScript("return navigator.userAgent;").ToString()
                        logger.Trace(String.Format("[IMDB] [CreateWebDriver Chrome] Original user agent ""{0}""", originalUserAgent))

                        strUserAgent = originalUserAgent.Replace("HeadlessChrome", "Chrome")

                        driver.ExecuteCdpCommand("Network.setUserAgentOverride",
                                         New Dictionary(Of String, Object) From {
                                             {"userAgent", strUserAgent}
                                         })

                        logger.Trace(String.Format("[IMDB] [CreateWebDriver Chrome] Modified user agent ""{0}""", strUserAgent))

                        Return driver
                    Case "Firefox"
                        Dim browserService = FirefoxDriverService.CreateDefaultService()
                        browserService.HideCommandPromptWindow = True

                        Dim browserOptions As New FirefoxOptions()
                        browserOptions.AddArgument("--headless")
                        browserOptions.SetPreference("dom.webdriver.enabled", False)
                        browserOptions.SetPreference("timezone", TimeZoneInfo.Local.Id)
                        browserOptions.SetPreference("useAutomationExtension", False)
                        browserOptions.SetPreference("intl.accept_languages", "en-US, en")
                        browserOptions.SetPreference("marionette.enabled", False) 'Disables most automation options, etc but does also hide the navigator.webdriver flag in some cases

                        Dim driver = New FirefoxDriver(browserService, browserOptions)

                        strUserAgent = driver.ExecuteScript("return navigator.userAgent;").ToString
                        logger.Trace(String.Format("[IMDB] [CreateWebDriver Firefox] Modified user agent ""{0}""", strUserAgent))

                        'Again try to hide navigator webdriver flag only works in very limited cases, webdriver is a readonly property in Firefox there's no sure way to disable it
                        Dim script As String = "
                        Object.defineProperty(navigator, 'webdriver', {
                            get: () => undefined
                        });
                        "

                        CType(driver, IJavaScriptExecutor).ExecuteScript(script)

                        Return driver
                    Case Else
                        Return Nothing
                End Select
            Catch ex As Exception
                'Driver failed try next browser in the the browser list
                logger.Trace(String.Format("[IMDB] [CreateWebDriver] Failed to start ""{0}"" with status {1}", browser, ex.Message))
            End Try
        Next

        Return Nothing
    End Function

    Private Function GetHtmlDocumentWithCookieHeader(ByVal cookieHeader As String, ByVal strUrl As String) As HtmlDocument
        Return GetAsyncHtmlDocumentWithCookieHeader(cookieHeader, strUrl).GetAwaiter.GetResult()
    End Function

    Async Function GetAsyncHtmlDocumentWithCookieHeader(ByVal cookieHeader As String, ByVal strUrl As String) As Threading.Tasks.Task(Of HtmlDocument)
        Dim handler As New HttpClientHandler() With {
            .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate,
            .UseCookies = False,
            .AllowAutoRedirect = True
        }
        Using handler
            Using webClient As New HttpClient(handler)
                webClient.Timeout = TimeSpan.FromSeconds(30)

                'Add the WAF cookieheaders
                webClient.DefaultRequestHeaders.Add("Cookie", cookieHeader)

                'Pretend to be a real browser by adding the useragent that we used when we retrieved the cookies
                If Not String.IsNullOrWhiteSpace(strUserAgent) Then
                    webClient.DefaultRequestHeaders.UserAgent.ParseAdd(strUserAgent)
                End If

                Using response As HttpResponseMessage = Await webClient.GetAsync(strUrl).ConfigureAwait(False)

                    logger.Trace(String.Format("[IMDB] [GetAsyncHtmlDocumentWithCookieHeader] Status: {0} {1} {2}", CInt(response.StatusCode), response.StatusCode.ToString(), response.RequestMessage.RequestUri.AbsoluteUri))

                    If response.StatusCode.Equals(HttpStatusCode.OK) Then
                        Dim html As String = Await response.Content.ReadAsStringAsync().ConfigureAwait(False)

                        If String.IsNullOrWhiteSpace(html) Then
                            logger.Trace(String.Format("[IMDB] [GetAsyncHtmlDocumentWithCookieHeader] Empty HTML response for URL ""{0}""", strUrl))
                            Return Nothing
                        Else
                            'Parse HTML normally via htmlagilitypack
                            Dim htmlresult As New HtmlDocument()
                            htmlresult.LoadHtml(html)

                            Return htmlresult
                        End If
                    Else
                        logger.Trace(String.Format("[IMDB] [GetAsyncHtmlDocumentWithCookieHeader] Failed for URL ""{0}"" with status {1} {2}", strUrl, CInt(response.StatusCode), response.StatusCode.ToString()))
                        Return Nothing
                    End If
                End Using
            End Using
        End Using
    End Function

    Private Function SearchMovie(ByVal title As String, ByVal year As String) As SearchResults_Movie
        Dim R As New SearchResults_Movie

        Dim strTitle As String = String.Concat(title, " ", If(Not String.IsNullOrEmpty(year), String.Concat("(", year, ")"), String.Empty)).Trim

        Dim htmldResultsPartialTitles As HtmlDocument = Nothing
        Dim htmldResultsPopularTitles As HtmlDocument = Nothing
        Dim htmldResultsShortTitles As HtmlDocument = Nothing
        Dim htmldResultsTvTitles As HtmlDocument = Nothing
        Dim htmldResultsVideoTitles As HtmlDocument = Nothing
        Dim htmldResultsExact As HtmlDocument = Nothing

        If strCookieHeaders Is String.Empty Then
            RetrieveIMDBcookies(strCookieHeaders)

            If strCookieHeaders Is String.Empty Then
                Return R
            End If
        End If

        htmldResultsExact = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&s=ttexact=true"))

        If _SpecialSettings.SearchTvTitles Then
            htmldResultsTvTitles = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&title_type=tv_movie"))
        End If

        If _SpecialSettings.SearchVideoTitles Then
            htmldResultsVideoTitles = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&title_type=video"))
        End If

        If _SpecialSettings.SearchShortTitles Then
            htmldResultsShortTitles = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&title_type=short"))
        End If

        If _SpecialSettings.SearchPartialTitles Then
            htmldResultsPartialTitles = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&s=tt&ttype=ft&ref_=fn_ft"))
        End If

        If _SpecialSettings.SearchPopularTitles Then
            htmldResultsPopularTitles = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(strTitle), "&s=tt"))
        End If

        'Exact titles
        If htmldResultsExact IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsExact.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.PopularTitles.Add(New MediaContainers.Movie With {
                            .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.listItem.originalTitleText),
                            .Title = nResult.listItem.originalTitleText,
                            .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.listItem.titleId},
                            .Year = nResult.listItem.releaseYear
                            })
                    End If
                Next
            End If
        End If

        'Popular titles
        If htmldResultsPopularTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsPopularTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.PopularTitles.Add(New MediaContainers.Movie With {
                            .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.listItem.originalTitleText),
                            .Title = nResult.listItem.originalTitleText,
                            .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.listItem.titleId},
                            .Year = nResult.listItem.releaseYear
                            })
                    End If
                Next
            End If
        End If

        'Partial titles
        If htmldResultsPartialTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsPartialTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.PartialMatches.Add(New MediaContainers.Movie With {
                             .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.listItem.originalTitleText),
                             .Title = nResult.listItem.originalTitleText,
                             .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.listItem.titleId},
                             .Year = nResult.listItem.releaseYear
                             })
                    End If
                Next
            End If

        End If

        'tv titles
        If htmldResultsTvTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsTvTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.TvTitles.Add(New MediaContainers.Movie With {
                             .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.listItem.originalTitleText),
                             .Title = nResult.listItem.originalTitleText,
                             .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.listItem.titleId},
                             .Year = nResult.listItem.releaseYear
                             })
                    End If
                Next
            End If
        End If

        'video titles
        If htmldResultsVideoTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsVideoTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.VideoTitles.Add(New MediaContainers.Movie With {
                             .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.listItem.originalTitleText),
                             .Title = nResult.listItem.originalTitleText,
                             .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.listItem.titleId},
                             .Year = nResult.listItem.releaseYear
                             })
                    End If
                Next
            End If
        End If

        'short titles
        If htmldResultsShortTitles IsNot Nothing Then
            json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldResultsShortTitles.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.ShortTitles.Add(New MediaContainers.Movie With {
                                .Lev = StringUtils.ComputeLevenshtein(StringUtils.FilterYear(strTitle).ToLower, nResult.listItem.originalTitleText),
                                .Title = nResult.listItem.originalTitleText,
                                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = nResult.listItem.titleId},
                                .Year = nResult.listItem.releaseYear
                                })
                    End If
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
        Dim htmldSearchResults As HtmlDocument

        If strCookieHeaders Is String.Empty Then
            RetrieveIMDBcookies(strCookieHeaders)
        End If

        htmldSearchResults = GetHtmlDocumentWithCookieHeader(strCookieHeaders, String.Concat("https://www.imdb.com/find/?q=", HttpUtility.UrlEncode(title), "&s=tt&ttype=tv"))

        If htmldSearchResults Is Nothing Then
            logger.Trace(String.Format("[IMDB] [SearchTVShow] failed to retrieve imdb find page"))
            'Do nothing
        Else
            If htmldSearchResults IsNot Nothing Then
                json_IMDB_Search_Results_next_data = DeserializeJsonObject(Of IMDBSearchResultsJson)(htmldSearchResults.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']").InnerHtml)
            End If

            If json_IMDB_Search_Results_next_data.props.pageProps.titleResults IsNot Nothing AndAlso json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results IsNot Nothing Then
                Dim searchResults As List(Of Result)
                searchResults = json_IMDB_Search_Results_next_data.props.pageProps.titleResults.results

                For Each nResult In searchResults
                    If nResult.listItem IsNot Nothing Then
                        R.Matches.Add(New MediaContainers.TVShow With {
                          .Title = nResult.listItem.originalTitleText,
                          .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVShow) With {.IMDbId = nResult.listItem.titleId}
                          })
                    End If
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