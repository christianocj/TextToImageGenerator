using TextToImageGenerator.Components;
using TextToImageGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<CloudflareAiOptions>(builder.Configuration.GetSection("CloudflareAi"));
builder.Services.AddHttpClient<ICloudflareAiService, CloudflareAiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();