Module Sample
    Const CNPJ_LENGTH As Integer = 14
    Dim companyCnpj = 12345678000190
    Dim unrelated = 123
    Dim CNPJ As Object
    Dim cnpj2 = "Company XYZ"
    Dim z As Integer

    Sub Register(taxId As Integer)
        Dim normalized = taxId.ToString()
    End Sub
End Module

Class Business
    Public Property TaxId As Integer
    Public Property TaxId As Integer = 12345678
    Private _internalCnpj As Integer = 12345678
    Private id As Integer
    Public count As Integer = 100
    Protected taxId = 12345678 ' Inferred type (VB.NET 2010+ allows this)

    Public Property ID As Integer
    Private _age As Integer

    Public Property Age As Integer
        Get
            Return _age
        End Get
        Set(value As Integer)
            _age = value
        End Set
    End Property
End Class

Interface ICompany
    Property CNPJ As String
    Property TaxId As Integer
End Interface

Enum TaxEnum
    CnpjValue = 12345678000190
    Other = 42
End Enum
