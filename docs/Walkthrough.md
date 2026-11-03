# Place and cancel an order

Start the API using the README instructions. Use a development database, not real
customer data. The commands below require PowerShell 7; keep tokens in your terminal.

You need an active product with a stocked variant and a TRY price. An admin can
create one using `ECommerce.Api/Catalog.http`. Public registration cannot create
admins; see `Identity.md` for the separate bootstrap command.

```powershell
$baseUrl = "http://localhost:5106"
$email = Read-Host "Customer email"
$password = Read-Host "Customer password" -MaskInput
$credentials = @{ email = $email; password = $password } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/api/auth/register" -Method Post -ContentType "application/json" -Body $credentials
$login = Invoke-RestMethod "$baseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $credentials
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
Remove-Variable password, credentials

Invoke-RestMethod "$baseUrl/api/products"
$productId = Read-Host "Product ID from the list"
$product = Invoke-RestMethod "$baseUrl/api/products/$productId"
$product.variants | Format-List
$variantId = Read-Host "Variant ID with stock and a TRY price"
$item = @{ productVariantId = $variantId; quantity = 1; currency = "TRY" } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/api/cart/items" -Method Post -Headers $headers -ContentType "application/json" -Body $item
$cart = Invoke-RestMethod "$baseUrl/api/cart" -Headers $headers
$cart
```

Read the cart before checkout. The expected total confirms the price you saw;
the server calculates the actual price and rejects stale totals.

```powershell
$headers["Idempotency-Key"] = [guid]::NewGuid().ToString()
$checkout = @{
    expectedCurrency = $cart.currency
    expectedTotal = $cart.totalAmount
    shippingAddress = @{
        recipientName = "Demo Customer"; phone = "+90 555 000 0000"
        countryCode = "TR"; city = "Istanbul"; postalCode = "34000"
        addressLine = "Example delivery address"
    }
} | ConvertTo-Json
$order = Invoke-RestMethod "$baseUrl/api/orders" -Method Post -Headers $headers -ContentType "application/json" -Body $checkout
$order
Invoke-RestMethod "$baseUrl/api/orders/$($order.id)" -Headers $headers
Invoke-RestMethod "$baseUrl/api/orders/$($order.id)/cancel" -Method Post -Headers $headers
Invoke-RestMethod "$baseUrl/api/products/$productId"
```

Checkout clears the cart and deducts stock. Cancellation restores it once.
Retry the identical checkout with the same key to receive the same order, not a
second purchase. Use a new key for a new purchase. This example does not charge
a card or arrange delivery. It leaves the demo account and cancelled order in
your development database; it does not delete records.

`Customer_can_browse_buy_and_cancel_a_product` exercises this flow automatically,
including real registration and login, in an isolated PostgreSQL test schema.
