Public Class IMDBJson
    Public Property props As Props
End Class

Public Class Props
    Public Property PageProps As PageProps 'Contains detailed information about the movie/serie
End Class

Public Class PageProps
    Public Property Tconst As String 'The unique identifier for the imdb title e.g. tt0111161
    Public Property AboveTheFoldData As AboveTheFoldData 'Key details about the movie
    Public Property MainColumnData As MainColumnData 'Additional detailed information about the movie
    Public Property ContentData As ContentData 'Serie episodedata
End Class

Public Class AboveTheFoldData
    Public Property Id As String 'Movie ID e.g. "tt0111161"
End Class

Public Class MainColumnData
    Public Property Id As String 'Movie ID e.g. "tt0111161"
    Public Property TitleText As TitleText 'Title (Movie, Serie) e.g. "The Shawshank Redemption"
    Public Property OriginalTitleText As TitleText 'Title in original language e.g. "Ying xiong" for movie "Hero"
    Public Property Plot As Plot 'Plot summary
    Public Property Outlines As PlotConnection 'Plot Outline
    Public Property Synopses As PlotConnection 'Complete plot text in Edge node
    Public Property PrimaryImage As PrimaryImage 'URL and details of the primary image
    Public Property ReleaseDate As ReleaseDate 'Release date 
    Public Property Certificates As CertificatesConnection 'All certifications for all countries e.g. "United States:R", "United States:TV-MA"
    Public Property Series As Series 'Series information
    Public Property Taglines As TaglineConnection
    Public Property CountriesDetails As CountriesOfOrigin 'Countries of origin
    Public Property CreatorsPageTitle As List(Of CreatorPrincipalCreditsForCategory) 'Creator of the TV-serie, empty for movies
    Public Property RatingsSummary As RatingsSummary 'Aggregate rating "9.3" and vote count "3068475"
    Public Property Runtime As MainColumnDataRuntime 'Duration of the movie in seconds e.g. "8520"
    Public Property Categories As List(Of Category) 'Contains all cast data
    Public Property DirectorsPageTitle As List(Of PrincipalCreditsForCategory) 'Directors details
    Public Property Genres As Genres 'List of genres e.g. "Action", "Comedy", "Crime"
    Public Property Certificate As MainColumnDataCertificate 'Rating information (R), (TV-MA)
    Public Property CompanyCreditCategories As List(Of CompanyCreditCategoryWithCompanyCredits) 'All companies involved ordered by company catergory e.g. "Castle Rock Entertainment" when Category is "production"
End Class

Public Class ContentData
    Public Property Section As Section
End Class

Public Class Series
    Public Property episodeNumber As Episodenumber
    Public Property nextEpisode As Nextepisode
    Public Property previousEpisode As Previousepisode
    Public Property __typename As String
End Class

Public Class Episodenumber
    Public Property episodeNumber As Integer
    Public Property seasonNumber As Integer
    Public Property __typename As String
End Class

Public Class Nextepisode
    Public Property id As String
    Public Property __typename As String
End Class

Public Class Previousepisode
    Public Property id As String
    Public Property __typename As String
End Class

Public Class TitleText
    Public Property Text As String
    Public Property __typename As String
End Class

Public Class Plot
    Public Property PlotText As Markdown
    Public Property __typename As String
End Class

Public Class Markdown
    Public Property PlainText As String
    Public Property __typename As String
End Class

Public Class PlotConnection
    Public Property Edges As List(Of PlotEdge)
    Public Property __typename As String
End Class

Public Class PlotEdge
    Public Property Node As PlotText
    Public Property __typename As String
End Class

Public Class PlotText
    Public Property PlotText As PlotMarkdown
    Public Property __typename As String
End Class

Public Class PlotMarkdown
    Public Property plaidHtml As String
    Public Property __typename As String
End Class

Public Class PrimaryImage
    Public Property Url As String
    Public Property Height As Integer
    Public Property Width As Integer
    Public Property Caption As Caption
    Public Property __typename As String
End Class

Public Class Caption
    Public Property PlainText As String
    Public Property __typename As String
End Class

Public Class ReleaseDate
    Public Property Day As Nullable(Of Integer)
    Public Property Month As Nullable(Of Integer)
    Public Property Year As Nullable(Of Integer)
    Public Property Country As Country
    Public Property __typename As String

    Public Function GetFullReleaseDate(Optional format As String = "yyyy-MM-dd") As String
        If Year.HasValue AndAlso Month.HasValue AndAlso Day.HasValue Then
            Dim dateValue As New DateTime(Year.Value, Month.Value, Day.Value)
            Return dateValue.ToString(format)
        Else
            Return Nothing
        End If
    End Function
End Class

Public Class CertificatesConnection
    Public Property Total As Integer
    Public Property Edges As List(Of CertificatesEdge)
    Public Property __typename As String
End Class

Public Class CertificatesEdge
    Public Property Node As Certificate
    Public Property __typename As String
End Class

Public Class Certificate
    Public Property id As String
    Public Property rating As String
    Public Property ratingReason As String
    Public Property ratingsBody As Ratingsbody
    Public Property country As Country
    Public Property __typename As String
End Class

Public Class Ratingsbody
    Public Property id As String
    Public Property __typename As String
End Class

Public Class Country
    Public Property id As String
    Public Property text As String
    Public Property __typename As String
End Class

Public Class TaglineConnection
    Public Property Edges As List(Of TaglineEdge)
    Public Property Total As Integer
    Public Property __typename As String
End Class

Public Class TaglineEdge
    Public Property Node As Tagline
    Public Property __typename As String
End Class

Public Class Tagline
    Public Property Text As String
    Public Property __typename As String
End Class

Public Class CountriesOfOrigin
    Public Property Countries As List(Of CountryOfOrigin)
    Public Property __typename As String
End Class

Public Class CountryOfOrigin
    Public Property id As String
    Public Property text As String
    Public Property __typename As String
End Class

Public Class CreatorPrincipalCreditsForCategory
    Public Property Credits As List(Of CreatorCrew)
    Public Property __typename As String
End Class

Public Class CreatorCrew
    Public Property Name As CreatorName
    Public Property __typename As String
End Class

Public Class CreatorName
    Public Property Id As String
    Public Property NameText As NameText
    Public Property __typename As String
End Class

Public Class RatingsSummary
    Public Property topRanking As topRanking
    Public Property aggregateRating As Nullable(Of Double)
    Public Property voteCount As Integer
    Public Property __typename As String

    Public Function GetAggregateRating() As Double
        If aggregateRating.HasValue Then
            Return aggregateRating.Value
        Else
            Return 0
        End If
    End Function
End Class

Public Class topRanking
    Public Property Id As String
    Public Property text As NameText
    Public Property rank As Integer
    Public Property __typename As String
End Class

Public Class MainColumnDataRuntime
    Public Property Seconds As Integer
    Public Property DisplayableProperty As DisplayableTitleRuntimeProperty
    Public Property __typename As String
End Class

Public Class DisplayableTitleRuntimeProperty
    Public Property Value As Markdown
    Public Property __typename As String
End Class

Public Class Category
    Public Property Id As String
    Public Property Name As String
    Public Property Section As CategorySection
End Class

Public Class CategorySection
    Public Property Items As List(Of CategoryItem)
End Class

Public Class CategoryItem
    Public Property Id As String 'nm id of person
    Public Property RowTitle As String 'Person name
    Public Property Characters As List(Of String) 'Played characters in movie/serie
    Public Property attributes As String 'Attribute e.g. (voice)
    Public Property ImageProps As ImageProps
End Class

Public Class ImageProps
    Public Property ImageModel As ImageModel
End Class

Public Class ImageModel
    'ImageModel is sadly not equal to Class PrimaryImage
    Public Property Url As String
    Public Property MaxHeight As Integer
    Public Property MaxWidth As Integer
    Public Property Caption As String
End Class

Public Class PrincipalCreditsForCategory
    Public Property Credits As List(Of Crew)
    Public Property __typename As String
End Class

Public Class Crew
    Public Property Name As Name
    Public Property __typename As String
End Class

Public Class Name
    Public Property Id As String
    Public Property NameText As NameText
    Public Property PrimaryImage As PrimaryImage
    Public Property __typename As String
End Class

Public Class NameText
    Public Property Text As String
    Public Property __typename As String
End Class

Public Class Genres
    Public Property Genres As List(Of Genre)
    Public Property __typename As String
End Class

Public Class Genre
    Public Property Text As String
    Public Property id As String
    Public Property __typename As String
End Class

Public Class MainColumnDataCertificate
    Public Property rating As String
    Public Property __typename As String
End Class

Public Class Section
    Public Property Seasons As List(Of Seasons)
    Public Property Episodes As Episodes
End Class

Public Class Seasons
    Public Property id As String
    Public Property value As String

    'Season value can be "unknown" in imdb see for example https://www.imdb.com/title/tt1305826/episodes/ (https://www.imdb.com/title/tt1305826/episodes/?season=Unknown)
    Public ReadOnly Property ValueAsInteger As Integer
        Get
            If String.IsNullOrEmpty(value) OrElse value.ToLower() = "unknown" Then
                Return -1
            Else
                Dim intValue As Integer = -1
                If Integer.TryParse(value, intValue) Then
                    Return intValue
                Else
                    Throw New InvalidCastException($"Cannot convert season value '{value}' to an integer.")
                End If
            End If
        End Get
    End Property
End Class

Public Class Episodes
    Public Property items As List(Of EpisodeItem)
    Public Property total As Integer
    Public Property hasNextPage As Boolean
    Public Property endCursor As String
    Public Property hasRatedEpisode As Boolean
End Class

Public Class EpisodeItem
    Public Property id As String
    Public Property type As String
    Public Property season As String
    Public Property episode As String
    Public Property titleText As String
    Public Property releaseDate As ReleaseDate
    Public Property image As EpisodeItemImage
    Public Property plot As String
    Public Property aggregateRating As Single
    Public Property voteCount As Integer
End Class

Public Class EpisodeItemImage
    Public Property url As String
    Public Property maxHeight As Integer
    Public Property maxWidth As Integer
    Public Property caption As String
End Class

Public Class CompanyCreditCategoryWithCompanyCredits
    Public Property Category As CompanyCreditCategory
    Public Property CompanyCredits As CompanyCreditConnection
End Class

Public Class CompanyCreditCategory
    Public Property Id As String 'e.g production, sales, distribution
    Public Property Text As String
    Public Property __typename As String
End Class

Public Class CompanyCreditConnection
    Public Property Edges As List(Of CompanyCreditEdge)
    'Total
    Public Property __typename As String
End Class

Public Class CompanyCreditEdge
    Public Property Node As CompanyCredit
End Class

Public Class CompanyCredit
    Public Property company As Company
    Public Property DisplayableProperty As DisplayableTitleCompanyCreditProperty
    Public Property __typename As String
End Class

Public Class Company
    Public Property Id As String
    Public Property __typename As String
End Class

Public Class DisplayableTitleCompanyCreditProperty
    Public Property Value As Markdown
End Class