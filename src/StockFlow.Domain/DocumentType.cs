namespace StockFlow.Domain;

/// <summary>
/// Type of fiscal or identity document held by a customer or supplier. Stored as text so the values
/// stay readable in the database.
/// </summary>
public enum DocumentType
{
    /// <summary>Tax identification number. In El Salvador it is the DUI for natural persons and a
    /// separate 14-digit number for legal entities.</summary>
    Nit,

    /// <summary>Documento Único de Identidad (El Salvador), 9 digits.</summary>
    Dui,

    /// <summary>Passport; free alphanumeric value.</summary>
    Passport,

    /// <summary>Any other document; free alphanumeric value.</summary>
    Other
}
