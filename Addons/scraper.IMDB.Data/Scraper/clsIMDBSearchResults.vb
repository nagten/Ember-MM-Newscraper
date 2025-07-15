Public Class IMDBSearchResultsJson
    Public Property props As IMDBSearchResultsJson_Props
End Class

Public Class IMDBSearchResultsJson_Props
    Public Property pageProps As IMDBSearchResultsJson_PageProps
End Class

Public Class IMDBSearchResultsJson_Pageprops
    Public Property findPageMeta As Findpagemeta
    Public Property nameResults As Nameresults
    Public Property titleResults As Titleresults
End Class

Public Class Findpagemeta
    Public Property searchTerm As String
    Public Property includeAdult As Boolean
    Public Property isExactMatch As Boolean
    Public Property searchType As String
End Class

Public Class Nameresults
    Public Property results() As Object
End Class

Public Class Titleresults
    Public Property results() As List(Of Result)
    Public Property hasExactMatches As Boolean
End Class

Public Class Result
    Public Property id As String 'imdbid
    Public Property titleNameText As String
    Public Property titleReleaseText As String
    Public Property titleTypeText As String
    Public Property titlePosterImageModel As Titleposterimagemodel
    Public Property imageType As String
End Class

Public Class Titleposterimagemodel
    Public Property url As String
    Public Property maxHeight As Integer
    Public Property maxWidth As Integer
    Public Property caption As String
End Class