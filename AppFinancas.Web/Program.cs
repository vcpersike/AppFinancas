using AppFinancas.Shared.Services;
using AppFinancas.Web.Components;
using AppFinancas.Web.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SignalR com otimizações para contêiner
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 10;
});

// Adicione compressão de resposta para melhorar o desempenho
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] {
            "application/octet-stream",
            "application/javascript",
            "text/css",
            "image/svg+xml"
        });
});

// Configuração de anti-forgery
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

// Serviços específicos do dispositivo
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// Ajusta configurações para melhor desempenho em contêiner
builder.WebHost.ConfigureKestrel(options => {
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Não use HTTPS em contêiner
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Aplica compressão de resposta
app.UseResponseCompression();

// Rota de diagnóstico simples que não usa serialização JSON
app.MapGet("/health", () => "Application is running!");

// Use arquivos estáticos
app.UseStaticFiles();
app.UseAntiforgery();

// Condiciona o logger detalhado apenas para ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Request Path: {Path}, Method: {Method}", 
            context.Request.Path, context.Request.Method);
        await next.Invoke();
    });
}

// Configura os componentes Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(AppFinancas.Shared._Imports).Assembly);

app.Run();