var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseUrls("http://localhost:5055");
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Configurar puerto y dirección
    serverOptions.ListenAnyIP(5055); // Esto reemplaza localhost
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
