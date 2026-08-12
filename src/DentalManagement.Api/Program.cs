// Host wiring (DI, environment configuration, health probe) lands in T15.
// BL-001 exposes no feature endpoint — controllers belong to later backlog items.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();
