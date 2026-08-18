namespace FaizanCosmetics.Application.Common;

/// <summary>Base type for business-rule violations the UI should show as a friendly message rather than a crash.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public class ValidationAppException : AppException
{
    public ValidationAppException(string message) : base(message) { }
}

public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException() : base("Invalid username or password.") { }
}

public class InsufficientStockException : AppException
{
    public InsufficientStockException(string productName, decimal available, decimal requested)
        : base($"Insufficient stock for '{productName}'. Available: {available}, Requested: {requested}.") { }
}

public class CreditLimitExceededException : AppException
{
    public CreditLimitExceededException(string clientName, decimal creditLimit, decimal wouldBeOutstanding)
        : base($"This sale would take '{clientName}' to Rs. {wouldBeOutstanding:N2} outstanding, exceeding their credit limit of Rs. {creditLimit:N2}.") { }
}

public class DuplicateBarcodeException : AppException
{
    public DuplicateBarcodeException(string barcode) : base($"A product with barcode '{barcode}' already exists.") { }
}

public class DuplicateSkuException : AppException
{
    public DuplicateSkuException(string sku) : base($"A product with SKU '{sku}' already exists.") { }
}

public class InvoiceNotEditableException : AppException
{
    public InvoiceNotEditableException(string invoiceNumber) : base($"Invoice '{invoiceNumber}' is posted and cannot be edited directly. Use Sales Return or Cancellation instead.") { }
}

public class PaymentExceedsDueException : AppException
{
    public PaymentExceedsDueException() : base("The payment amount cannot exceed the total amount due.") { }
}
