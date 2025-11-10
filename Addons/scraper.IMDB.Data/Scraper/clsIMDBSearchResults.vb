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
    Public Property index As String 'imdbid
    Public Property listItem As resultListItem
End Class

Public Class resultListItem
    Public Property titleId As String 'imdbid
    Public Property originalTitleText As String
    Public Property releaseYear As String
End Class