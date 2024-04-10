var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/GenerateSocialMediaPost/{podcastUrl}", (string podcastUrl) =>
{
    return PodcastAppAPI.PodcastCopilot.GenerateSocialMediaPost(podcastUrl);
})
    .WithName("GetSocialMediaPost")
    .WithSummary("Generate Social Media Post")
    .WithDescription("Generates a blurb / social media post with an image for a podcast episode based on the podcast url provided.")
    .WithOpenApi();

app.Run();