using StockFlow.Domain;

namespace StockFlow.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="PurchaseNumber"/> format rule.
/// </summary>
public class PurchaseNumberTests
{
    /// <summary>A small sequence value is padded to six digits.</summary>
    [Fact]
    public void Format_WithSmallValue_PadsToSixDigits()
    {
        // Act & Assert
        Assert.Equal("OC-000001", PurchaseNumber.Format(1));
    }

    /// <summary>Zero is padded to six digits.</summary>
    [Fact]
    public void Format_WithZero_PadsToSixDigits()
    {
        // Act & Assert
        Assert.Equal("OC-000000", PurchaseNumber.Format(0));
    }

    /// <summary>A value with more than six digits keeps every digit.</summary>
    [Fact]
    public void Format_WithMoreThanSixDigits_KeepsAllDigits()
    {
        // Act & Assert
        Assert.Equal("OC-1234567", PurchaseNumber.Format(1234567));
    }

    /// <summary>A negative sequence value is rejected as a programming error.</summary>
    [Fact]
    public void Format_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => PurchaseNumber.Format(-1));
    }
}
