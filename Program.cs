using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LogiTrackContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<LogiTrackContext>()
    .AddDefaultTokenProviders();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Returns the home page
app.MapGet("/", () => "LogiTrack API");

// Create the "Admin" role
var roles = new[] {"Admin"};

app.MapPost("/api/auth/create-role", async (RoleManager<IdentityRole> roleManager, LogiTrackContext context) =>
{
    foreach (var role in roles)
    {
        if(!await context.Roles.AnyAsync(r => r.Name == role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    return Results.Ok("Role created successfully.");
});

// Registers a new user
app.MapPost("/api/auth/register", async (UserManager<ApplicationUser> userManager, RegisterModel model) =>
{
    var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
    var result = await userManager.CreateAsync(user, model.Password);
    
    if (!result.Succeeded)
    {
        return Results.BadRequest(result.Errors);
    }

    var roleResult = await userManager.AddToRoleAsync(user, "Admin");
    if (!roleResult.Succeeded)
    {
        return Results.BadRequest(roleResult.Errors);
    }

    return Results.Ok("User registered successfully.");
});

// Logins a new user
app.MapPost("/api/auth/login", async (SignInManager<ApplicationUser> signInManager, LogiTrackContext context, LoginModel model) =>
{
    var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);
    
    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    return Results.Ok("Logged in successfully.");
});

// Returns the whole inventory as a list
app.MapGet("/api/inventory", (LogiTrackContext context, IMemoryCache cache) => 
{
    string cacheKey = "inventoryItems";

    if (!cache.TryGetValue(cacheKey, out List<InventoryItem>? inventory))
    {
        // AsNoTracking() for read-only
        inventory = context.inventoryItems.AsNoTracking().ToList();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));

        cache.Set(cacheKey, inventory, cacheOptions);

        Console.WriteLine("Fetched inventory from database");
    }

    Console.WriteLine("Fetched inventory from cache");
    return Results.Ok(inventory);
});

// Finds a specific item by its ID and displays its info
app.MapGet("/api/inventory/{id}", async (LogiTrackContext context, int id, IMemoryCache cache) =>
{
    string cacheKey = $"inventoryItem_{id}";

    if(!cache.TryGetValue(cacheKey, out InventoryItem? item))
    {
        // AsNoTracking() for read-only
        // (i => i.itemId == id) for each InventoryItem object "i", i.itemId equals to id
        item = await context.inventoryItems.AsNoTracking().SingleOrDefaultAsync(i => i.itemId == id);
    
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            
        cache.Set(cacheKey, item, cacheOptions);
        Console.WriteLine("Fetched the item from database");
    }

    if (item != null)
    {
        Console.WriteLine("Fetched item from cache");
        return Results.Ok(item.DisplayInfo());
    }
    else
    {
        return Results.NotFound();
    }
}).RequireAuthorization("Admin");

// Adds an item to the inventory and displays its info
app.MapPost("/api/inventory", async (LogiTrackContext context, InventoryItem item) =>
{
    context.inventoryItems.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/api/inventory/{item.itemId}", item.DisplayInfo());
}).RequireAuthorization("Admin");

// Delete a specific item from the inventory by its ID
app.MapDelete("api/inventory/{id}", async (LogiTrackContext context, int id) =>
{
    var item = context.inventoryItems.Find(id);
    if (item == null)
    {
        return Results.NotFound();
    }
    context.inventoryItems.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

// Returns all orders
app.MapGet("api/orders", (LogiTrackContext context, IMemoryCache cache) => 
{
    string cacheKey = "orders";

    if(!cache.TryGetValue(cacheKey, out List<Order>? orderList))
    {
        // Used .Include(o => o.items) for eager loading
        // AsNoTracking() for read-only
        orderList = context.orders.AsNoTracking().Include(o => o.items).ToList();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
        
        cache.Set(cacheKey, orderList, cacheOptions);
        Console.WriteLine("Fetched orders from database");
    }

    Console.WriteLine("Fetched orders from cache");
    return Results.Ok(orderList);
}).RequireAuthorization("Admin");

// Finds a specific order by its ID and returns its info
app.MapGet("api/orders/{id}", async (LogiTrackContext context, int id, IMemoryCache cache) =>
{
    string cacheKey = $"order_{id}";

    if (!cache.TryGetValue(cacheKey, out Order? order))
    {
        // Used .Include(o => o.items) for eager loading
        // AsNoTracking() for read-only
        // (o => o.orderId == id) for each Order object "o", o.orderId equals to id
        order = await context.orders.AsNoTracking().Include(o => o.items)
            .SingleOrDefaultAsync(o => o.orderId == id);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            
        cache.Set(cacheKey, order, cacheOptions);
        Console.WriteLine("Fetched order from database");
    }

    if (order != null)
    {
        Console.WriteLine("Fetched order from cache");
        return Results.Ok(order.GetOrderSummary());
    }
    else
    {
        return Results.NotFound();
    }
}).RequireAuthorization("Admin");

// Adds an order and returns its info
app.MapPost("api/orders", async (LogiTrackContext context, Order order) =>
{
    context.orders.Add(order);
    await context.SaveChangesAsync();
    return Results.Created($"api/orders/{order.orderId}", order.GetOrderSummary());
});

// Delete a specific order by its ID
app.MapDelete("api/orders/{id}", async (LogiTrackContext context, int id) => 
{
    var order = await context.orders.FindAsync(id);
    if(order == null)
    {
        return Results.NotFound();
    }
    context.orders.Remove(order);
    await context.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

app.Run();

