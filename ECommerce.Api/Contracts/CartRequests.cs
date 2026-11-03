using System.ComponentModel.DataAnnotations;
using ECommerce.Domain.Entities;

namespace ECommerce.Api.Contracts;

public record AddCartItemRequest(
    Guid ProductVariantId,
    [Range(1, CartItem.MaxQuantity)] int Quantity,
    [Required, StringLength(3)] string Currency);

public record CartQuantityRequest([Range(1, CartItem.MaxQuantity)] int Quantity);
public record CartCurrencyRequest([Required, StringLength(3)] string Currency);
