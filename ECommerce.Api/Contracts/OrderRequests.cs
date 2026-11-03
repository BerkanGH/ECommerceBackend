using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Contracts;

public record ShippingAddressRequest(
    [Required, StringLength(100)] string RecipientName,
    [Required, StringLength(30)] string Phone,
    [Required, StringLength(2, MinimumLength = 2)] string CountryCode,
    [Required, StringLength(100)] string City,
    [Required, StringLength(20)] string PostalCode,
    [Required, StringLength(500)] string AddressLine);

public record CheckoutRequest(
    [Required, StringLength(3)] string ExpectedCurrency,
    decimal ExpectedTotal,
    [Required] ShippingAddressRequest ShippingAddress);
